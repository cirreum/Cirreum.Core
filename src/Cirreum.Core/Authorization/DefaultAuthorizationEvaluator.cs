namespace Cirreum.Authorization;

using Cirreum.Authorization.Diagnostics;
using Cirreum.Exceptions;
using Cirreum.Security;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

/// <summary>
/// The default implementation of the <see cref="IAuthorizationEvaluator"/>.
/// </summary>
/// <param name="registry">The authorization role registry for resolving effective roles.</param>
/// <param name="userAccessor">The accessor for retrieving current user state.</param>
/// <param name="services">The service provider for resolving validators.</param>
/// <param name="logger">The logger for authorization events.</param>
sealed class DefaultAuthorizationEvaluator(
	IAuthorizationRoleRegistry registry,
	IUserStateAccessor userAccessor,
	IServiceProvider services,
	ILogger<DefaultAuthorizationEvaluator> logger
) : IAuthorizationEvaluator {

	/// <inheritdoc/>
	/// <remarks>
	/// Ad-hoc evaluation entry point. Builds the OperationContext from scratch
	/// and delegates to the context-aware overload.
	/// </remarks>
	public async ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		// Build OperationContext for ad-hoc evaluation
		var userState = await userAccessor.GetUser().ConfigureAwait(false);
		var operation = OperationContext.Create(
			userState,
			operationId: Guid.NewGuid().ToString("N")[..16],
			correlationId: Guid.NewGuid().ToString("N"),
			startTimestamp: Stopwatch.GetTimestamp());

		// Delegate to the context-aware overload
		return await this.Evaluate(resource, operation, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	/// <remarks>
	/// Context-aware evaluation entry point. Uses the provided OperationContext
	/// to avoid rebuilding user state and environment information.
	/// </remarks>
	public async ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		OperationContext operation,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		var resourceRuntimeType = resource.GetType();
		var resourceName = resourceRuntimeType.Name;
		var resourceCompileTimeType = typeof(TResource);

		// Check authentication
		if (!operation.IsAuthenticated) {
			//******************************************
			//
			// NOT AUTHENTICATED
			//
			//******************************************
			var ex = new UnauthenticatedAccessException("User is not authenticated.");

			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				ex.Message);

			return Result.Fail(ex);
		}

		// Check cancellation early
		cancellationToken.ThrowIfCancellationRequested();

		// Check if the runtime type matches the compile-time type
		if (resourceRuntimeType != resourceCompileTimeType) {
			throw new ArgumentException(
				$"Resource must be a concrete type. Expected {resourceCompileTimeType.Name} but got {resourceRuntimeType.Name}. " +
				"Do not pass casted instances.",
				nameof(resource));
		}

		// Get all authorizors for this resource type
		var resourceAuthorizors = services
			.GetServices<IAuthorizationResourceValidator<TResource>>()
			.OfType<AuthorizationValidatorBase<TResource>>()
			.ToList();

		// Get all policy validators that support the current runtime
		var policyAuthorizors = services
			.GetServices<IAuthorizationPolicyValidator>()
			.Where(pv => pv.SupportedRuntimeTypes.Contains(operation.RuntimeType))
			.ToList();

		if (resourceAuthorizors.Count == 0 && policyAuthorizors.Count == 0) {
			//******************************************
			//
			// RESOURCE HAS NO AUTHORIZORS AND
			// RUNTIME HAS NO POLICY AUTHORIZORS
			//
			//******************************************
			var emptyAuthContainerEx = new InvalidOperationException(
				$"Resource '{resourceName}' has no authorizors or applicable policies.");
			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				emptyAuthContainerEx.Message);
			return Result.Fail(emptyAuthContainerEx);
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

		// Get the user's roles
		var roles = operation.UserState.Profile.Roles
			.Select(registry.GetRoleFromString)
			.OfType<Role>()
			.ToImmutableList();

		if (roles.Count == 0) {
			//******************************************
			//
			// USER HAS NO REGISTERED ROLES
			//
			//******************************************
			var noRolesEx = new ForbiddenAccessException(
				$"User '{operation.UserName}' has no assigned roles.");

			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				noRolesEx.Message);

			return Result.Fail(noRolesEx);
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

		const string scopeTemplate = "Authorizing user '{UserName}' for Resource '{ResourceName}'";
		using var logScope = logger.BeginScope(scopeTemplate, operation.UserName, resourceName);

		try {

			// Build effective roles ONCE
			var effectiveRoles = registry.GetEffectiveRoles(roles);

			// Build the canonical AuthorizationContext that validators will use
			var authorizationContext = new AuthorizationContext<TResource>(
				operation,
				effectiveRoles,
				resource);

			// Create FluentValidation context
			var validationContext = new ValidationContext<AuthorizationContext<TResource>>(authorizationContext);

			// Collect all failures
			var allFailures = new List<ValidationFailure>();

			// Run all Resource Authorizors
			var resourceTasks = resourceAuthorizors
				.Select(v => v.ValidateAsync(validationContext, cancellationToken));

			var resourceResults = await Task.WhenAll(resourceTasks).ConfigureAwait(false);

			allFailures.AddRange(resourceResults
				.SelectMany(result => result.Errors)
				.Where(f => f != null));

			// Run applicable Policy Authorizors
			var applicablePolicyValidatorsQuery = policyAuthorizors
				.Where(pv => pv.AppliesTo(resource, operation.RuntimeType, operation.Timestamp))
				.OrderBy(pv => pv.Order);

			foreach (var policyValidator in applicablePolicyValidatorsQuery) {
				cancellationToken.ThrowIfCancellationRequested();

				var policyResult = await policyValidator
					.ValidateAsync(authorizationContext, cancellationToken)
					.ConfigureAwait(false);

				if (!policyResult.IsValid) {
					allFailures.AddRange(policyResult.Errors);
				}
			}

			if (allFailures.Count != 0) {
				//******************************************
				//
				// NOT AUTHORIZED
				//
				//******************************************
				var userExplicitlyDeniedEx = string.Join(',', allFailures.Select(f => f.ErrorMessage));

				logger.LogAuthorizingResourceDenied(
					operation.UserName,
					resourceName,
					userExplicitlyDeniedEx);

				var ex = new ForbiddenAccessException(userExplicitlyDeniedEx);
				return Result.Fail(ex);
			}

			//******************************************
			//
			// AUTHORIZED
			//
			//******************************************
			logger.LogAuthorizingResourceAllowed(
				operation.UserName,
				resourceName);

			return Result.Success;

		} catch (OperationCanceledException) {
			// Expected - let it propagate
			throw;

		} catch (Exception ex) {
			// Unexpected runtime errors during validation
			// (e.g., database failures, network issues in validators)
			logger.LogAuthorizingResourceUnknownError(
				ex,
				operation.UserName,
				resourceName,
				ex.Message);
			return Result.Fail(ex);
		}
	}
}