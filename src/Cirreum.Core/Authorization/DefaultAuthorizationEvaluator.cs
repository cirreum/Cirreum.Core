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

/// <summary>
/// The default implementation of the <see cref="IAuthorizationEvaluator"/>.
/// </summary>
/// <param name="registry">The authorization role registry for resolving effective roles.</param>
/// <param name="applicationEnvironment">The application environment information.</param>
/// <param name="userAccessor">The accessor for retrieving current user state.</param>
/// <param name="services">The service provider for resolving validators.</param>
/// <param name="logger">The logger for authorization events.</param>
public sealed class DefaultAuthorizationEvaluator(
	IAuthorizationRoleRegistry registry,
	IApplicationEnvironment applicationEnvironment,
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
			applicationEnvironment.EnvironmentName,
			userState,
			operationId: Guid.NewGuid().ToString("N")[..16],
			correlationId: Guid.NewGuid().ToString("N"));

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

		// Check cancellation early
		cancellationToken.ThrowIfCancellationRequested();

		var resourceRuntimeType = resource.GetType();
		var resourceName = resourceRuntimeType.Name;
		var resourceCompileTimeType = typeof(TResource);

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
			.Where(pv => pv.SupportedRuntimeTypes.Contains(operation.Runtime))
			.ToList();

		if (resourceAuthorizors.Count == 0 && policyAuthorizors.Count == 0) {
			//******************************************
			//
			// RESOURCE HAS NO AUTHORIZORS AND
			// RUNTIME HAS NO POLICY AUTHORIZORS
			//
			//******************************************
			throw new InvalidOperationException(
				$"Resource '{resourceName}' has no authorizors or applicable policies.");
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

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
			var ex = new ForbiddenAccessException(
				$"User '{operation.UserName}' has no assigned roles.");

			logger.LogAuthorizingResourceDenied(
				operation.UserName,
				resourceName,
				ex.Message);

			return Result.Fail(ex);
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
			if (resourceAuthorizors.Count > 0) {
				var resourceTasks = resourceAuthorizors
					.Select(v => v.ValidateAsync(validationContext, cancellationToken));

				var resourceResults = await Task.WhenAll(resourceTasks).ConfigureAwait(false);

				var failures = resourceResults
					.SelectMany(result => result.Errors)
					.Where(f => f != null)
					.ToList();

				allFailures.AddRange(failures);
			}

			// Run applicable Policy Authorizors
			var applicablePolicyValidators = policyAuthorizors
				.Where(pv => pv.AppliesTo(resource, operation.Runtime, operation.Timestamp))
				.OrderBy(pv => pv.Order)
				.ToList();

			foreach (var policyValidator in applicablePolicyValidators) {
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
				var errorMessage = string.Join(',', allFailures.Select(f => f.ErrorMessage));

				logger.LogAuthorizingResourceDenied(
					operation.UserName,
					resourceName,
					errorMessage);

				var ex = new ForbiddenAccessException(errorMessage);
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
			logger.LogAuthorizingResourceUnknownError(ex, operation.UserName, resourceName, ex.Message);
			return Result.Fail(ex);
		}
	}
}