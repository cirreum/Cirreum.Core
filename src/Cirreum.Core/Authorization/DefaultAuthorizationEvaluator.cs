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
/// <param name="registry"></param>
/// <param name="userAccessor"></param>
/// <param name="services"></param>
/// <param name="logger"></param>
sealed class DefaultAuthorizationEvaluator(
	IAuthorizationRoleRegistry registry,
	IUserStateAccessor userAccessor,
	IServiceProvider services,
	ILogger<DefaultAuthorizationEvaluator> logger
) : IAuthorizationEvaluator {

	/// <inheritdoc/>
	public async ValueTask<Result> Evaluate<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		try {
			await this.Enforce(resource, requestId, correlationId, cancellationToken);
			return Result.Success;
		} catch (UnauthenticatedAccessException ex) {
			return Result.Fail(ex);
		} catch (ForbiddenAccessException ex) {
			return Result.Fail(ex);
		}
		// InvalidOperationException still throws - it's a config error
	}

	/// <inheritdoc/>
	public async ValueTask Enforce<TResource>(
		TResource resource,
		string requestId,
		string correlationId,
		CancellationToken cancellationToken = default)
		where TResource : IAuthorizableResource {

		// Check cancellation early
		cancellationToken.ThrowIfCancellationRequested();

		var resourceName = resource.GetType().Name;
		var typeTypeName = typeof(TResource).Name;

		// Check if the runtime type matches the compile-time type
		if (resource.GetType() != typeof(TResource)) {
			throw new ArgumentException(
				$"Resource must be a concrete type. Expected {resourceName} but got {typeTypeName}. " +
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
			.Where(pv => pv.SupportedRuntimeTypes.Contains(ApplicationRuntime.Current.RuntimeType))
			.ToList();

		if (resourceAuthorizors.Count == 0 && policyAuthorizors.Count == 0) {
			//******************************************
			//
			// RESOURCE HAS NO AUTHORIZORS AND
			// RUNTIME HAS NO POLCIY AUTHORIZORS
			//
			// Throw invalid operation exception to prevent access...
			//
			throw new InvalidOperationException(
				$"Resource '{resourceName}' has no authorizors or applicable policies.");
		}

		// Check cancellation before potentially expensive user state retrieval
		cancellationToken.ThrowIfCancellationRequested();

		// Get the current user
		var user = await userAccessor.GetUser();
		if (!user.IsAuthenticated) {
			//******************************************
			//
			// NOT AUTHENTICATED
			//
			// Throw unauthenticated exception to prevent access...
			//
			throw new UnauthenticatedAccessException(
				"User is not Authenticated");
			//******************************************
		}

		// Get the user's roles
		var roles = user.Profile.Roles
			.Select(registry.GetRoleFromString)
			.OfType<Role>()
			.ToImmutableList();
		if (roles.Count == 0) {
			//******************************************
			//
			// USER HAS NO REGISTERED ROLES
			//
			// Throw forbidden exception to prevent access...
			//
			throw new ForbiddenAccessException(
				$"User '{user.Name}' has no assigned roles");
			//******************************************
		}

		// Check cancellation before entering validation logic
		cancellationToken.ThrowIfCancellationRequested();

		const string scopeTemplate = "Authorizing user '{UserName}' for Resource '{runtimeTypeName}'";
		using (var logScope = logger.BeginScope(scopeTemplate, user.Name, resourceName)) {
			try {

				// Create current Authorization context
				var effectiveRoles =
					registry.GetEffectiveRoles(roles);

				// Authorization Context
				var authorizationContext = AuthorizationContext<TResource>.Create(
					resource,
					effectiveRoles,
					user,
					requestId,
					correlationId);

				// Validation Context...
				var validationContext = new ValidationContext<AuthorizationContext<TResource>>(authorizationContext);

				// Collect all failures
				var allFailures = new List<ValidationFailure>();

				// Run all Resource Authorizors
				if (resourceAuthorizors.Count > 0) {
					var resourceTasks = resourceAuthorizors
						.Select(v => v.ValidateAsync(validationContext, cancellationToken));
					var resourceResults = await Task.WhenAll(resourceTasks);
					var failures = resourceResults
						.SelectMany(result => result.Errors)
						.Where(f => f != null)
						.ToList();
					allFailures.AddRange(failures);
				}

				// Run applicable Policy Authorizors
				var applicablePolicyValidators = policyAuthorizors
					.Where(pv => pv.AppliesTo(resource, authorizationContext.ExecutionContext))
					.OrderBy(pv => pv.Order)
					.ToList();

				foreach (var policyValidator in applicablePolicyValidators) {
					cancellationToken.ThrowIfCancellationRequested();

					var policyResult = await policyValidator.ValidateAsync(authorizationContext, cancellationToken);
					if (!policyResult.IsValid) {
						allFailures.AddRange(policyResult.Errors);
					}
				}

				if (allFailures.Count != 0) {
					//******************************************
					//
					// NOT AUTHORIZED
					//
					// If we get here, at least one condition failed
					//
					var errorMessage = string.Join(',', allFailures.Select(f => f.ErrorMessage));
					logger.LogAuthorizingResourceDenied(
						user.Name,
						resourceName,
						requestId,
						errorMessage);
					throw new ForbiddenAccessException(errorMessage);
				}
				//******************************************

				//******************************************
				//
				// AUTHORIZED
				//
				logger.LogAuthorizingResourceAllowed(
					user.Name,
					resourceName,
					requestId);

				//******************************************


			} catch (Exception ex) when (
				  ex is not UnauthenticatedAccessException
				  and not ForbiddenAccessException) {
				logger.LogAuthorizingResourceUnknownError(
					ex,
					user.Name,
					resourceName,
					requestId);
				throw;
			}

		}

	}

}