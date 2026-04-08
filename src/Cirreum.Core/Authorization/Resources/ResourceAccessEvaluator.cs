namespace Cirreum.Authorization.Resources;

using Cirreum.Exceptions;
using Cirreum.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

/// <summary>
/// Sealed implementation of <see cref="IResourceAccessEvaluator"/>. Resolves effective
/// access by walking the resource hierarchy via <see cref="IAccessEntryProvider{T}"/> and
/// caching results per-request in an L1 dictionary.
/// </summary>
/// <remarks>
/// <para>
/// Registered as <b>Scoped</b> — the L1 cache lives for a single request. The evaluator
/// resolves the caller's effective roles lazily on first use and reuses them for all
/// subsequent checks within the same scope.
/// </para>
/// </remarks>
internal sealed class ResourceAccessEvaluator(
	IAuthorizationRoleRegistry roleRegistry,
	IUserStateAccessor userStateAccessor,
	IServiceProvider services,
	ILogger<ResourceAccessEvaluator> logger) : IResourceAccessEvaluator {

	// L1 per-request cache keyed by "{TypeName}:{ResourceId}"
	private readonly Dictionary<string, EffectiveAccess> _cache = [];

	// Effective roles resolved lazily on first use
	private IImmutableSet<Role>? _effectiveRoles;
	private IUserState? _userState;

	/// <inheritdoc/>
	public async ValueTask<Result> CheckAsync<T>(
		T resource,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource {

		var (userState, effectiveRoles) = await ResolveCallerAsync().ConfigureAwait(false);

		if (effectiveRoles.Count == 0) {
			logger.LogResourceAccessDenied(
				userState.Name,
				typeof(T).Name,
				resource.ResourceId,
				permission.ToString(),
				DenyCodes.ResourceAccessDenied);

			EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionDeny, DenyCodes.ResourceAccessDenied);
			return Result.Fail(new ForbiddenAccessException(
				$"User '{userState.Name}' has no roles — access denied to {typeof(T).Name}."));
		}

		var provider = services.GetRequiredService<IAccessEntryProvider<T>>();
		var effective = await ResolveEffectiveAccessAsync(resource, provider, cancellationToken).ConfigureAwait(false);

		if (effective.IsAuthorized(permission, effectiveRoles)) {
			logger.LogResourceAccessAllowed(
				userState.Name,
				typeof(T).Name,
				resource.ResourceId,
				permission.ToString());

			EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionPass, AuthorizationTelemetry.ReasonPass);
			return Result.Success;
		}

		logger.LogResourceAccessDenied(
			userState.Name,
			typeof(T).Name,
			resource.ResourceId,
			permission.ToString(),
			DenyCodes.ResourceAccessDenied);

		EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionDeny, DenyCodes.ResourceAccessDenied);
		return Result.Fail(new ForbiddenAccessException(
			$"User '{userState.Name}' does not have '{permission}' on {typeof(T).Name} '{resource.ResourceId}'."));
	}

	/// <inheritdoc/>
	public async ValueTask<Result> CheckAsync<T>(
		string? resourceId,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource {

		var provider = services.GetRequiredService<IAccessEntryProvider<T>>();

		// null resourceId → root defaults
		if (resourceId is null) {
			var rootAccess = new EffectiveAccess(provider.RootDefaults);
			var (userState, effectiveRoles) = await ResolveCallerAsync().ConfigureAwait(false);

			if (effectiveRoles.Count == 0) {
				EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionDeny, DenyCodes.ResourceAccessDenied);
				return Result.Fail(new ForbiddenAccessException(
					$"User '{userState.Name}' has no roles — access denied to {typeof(T).Name}."));
			}

			if (rootAccess.IsAuthorized(permission, effectiveRoles)) {
				EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionPass, AuthorizationTelemetry.ReasonPass);
				return Result.Success;
			}

			EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionDeny, DenyCodes.ResourceAccessDenied);
			return Result.Fail(new ForbiddenAccessException(
				$"User '{userState.Name}' does not have '{permission}' at root of {typeof(T).Name}."));
		}

		var resource = await provider.GetByIdAsync(resourceId, cancellationToken).ConfigureAwait(false);

		if (resource is null) {
			EmitTelemetry(typeof(T).Name, AuthorizationTelemetry.DecisionDeny, DenyCodes.ResourceNotFound);
			return Result.Fail(new NotFoundException(resourceId));
		}

		return await CheckAsync(resource, permission, cancellationToken).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async ValueTask<IReadOnlyList<T>> FilterAsync<T>(
		IEnumerable<T> resources,
		Permission permission,
		CancellationToken cancellationToken = default)
		where T : IProtectedResource {

		var (_, effectiveRoles) = await ResolveCallerAsync().ConfigureAwait(false);

		if (effectiveRoles.Count == 0) {
			return [];
		}

		var provider = services.GetRequiredService<IAccessEntryProvider<T>>();
		var result = new List<T>();

		foreach (var resource in resources) {
			cancellationToken.ThrowIfCancellationRequested();

			var effective = await ResolveEffectiveAccessAsync(resource, provider, cancellationToken).ConfigureAwait(false);
			if (effective.IsAuthorized(permission, effectiveRoles)) {
				result.Add(resource);
			}
		}

		return result;
	}

	// ———————————————————————— Private helpers ————————————————————————

	/// <summary>
	/// Resolves effective roles lazily and caches for the request lifetime.
	/// </summary>
	private async ValueTask<(IUserState UserState, IImmutableSet<Role> EffectiveRoles)> ResolveCallerAsync() {
		if (_effectiveRoles is not null) {
			return (_userState!, _effectiveRoles);
		}

		_userState = await userStateAccessor.GetUser().ConfigureAwait(false);

		var roles = _userState.Profile.Roles
			.Select(roleRegistry.GetRoleFromString)
			.OfType<Role>()
			.ToImmutableList();

		_effectiveRoles = roles.Count > 0
			? roleRegistry.GetEffectiveRoles(roles)
			: ImmutableHashSet<Role>.Empty;

		return (_userState, _effectiveRoles);
	}

	/// <summary>
	/// Resolves the effective access for a resource by walking the hierarchy.
	/// Uses L1 cache to avoid redundant walks (sibling optimization).
	/// </summary>
	private async ValueTask<EffectiveAccess> ResolveEffectiveAccessAsync<T>(
		T resource,
		IAccessEntryProvider<T> provider,
		CancellationToken cancellationToken)
		where T : IProtectedResource {

		var cacheKey = BuildCacheKey<T>(resource.ResourceId);

		// L1 cache check
		if (cacheKey is not null && _cache.TryGetValue(cacheKey, out var cached)) {
			return cached;
		}

		// Start with the resource's own entries
		var entries = new List<AccessEntry>(resource.AccessList);

		// Walk up the hierarchy if inheritance is enabled
		if (resource.InheritPermissions) {
			var visited = new HashSet<string>(StringComparer.Ordinal);
			if (resource.ResourceId is not null) {
				visited.Add(resource.ResourceId);
			}

			var parentId = provider.GetParentId(resource);
			var reachedRoot = parentId is null;

			while (parentId is not null) {
				cancellationToken.ThrowIfCancellationRequested();

				// Cycle detection
				if (!visited.Add(parentId)) {
					logger.LogResourceAccessCycleDetected(typeof(T).Name, parentId);
					break;
				}

				// Sibling optimization: check if parent was already resolved
				var parentCacheKey = BuildCacheKey<T>(parentId);
				if (parentCacheKey is not null && _cache.TryGetValue(parentCacheKey, out var parentCached)) {
					entries.AddRange(parentCached.Entries);
					// Parent's effective already includes its ancestors + root defaults
					reachedRoot = true;
					break;
				}

				var parent = await provider.GetByIdAsync(parentId, cancellationToken).ConfigureAwait(false);

				if (parent is null) {
					// Orphan — parent doesn't exist; stop walking
					logger.LogResourceAccessOrphanDetected(typeof(T).Name, parentId);
					break;
				}

				entries.AddRange(parent.AccessList);

				if (!parent.InheritPermissions) {
					// Inheritance broken at parent
					reachedRoot = true;
					break;
				}

				parentId = provider.GetParentId(parent);
				if (parentId is null) {
					reachedRoot = true;
				}
			}

			// Merge root defaults if we reached the root
			if (reachedRoot) {
				entries.AddRange(provider.RootDefaults);
			}
		} else if (provider.GetParentId(resource) is null) {
			// Resource is at root and doesn't inherit — still apply root defaults
			entries.AddRange(provider.RootDefaults);
		}

		var effective = new EffectiveAccess(entries);

		// Cache the result
		if (cacheKey is not null) {
			_cache[cacheKey] = effective;
		}

		return effective;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string? BuildCacheKey<T>(string? resourceId) =>
		resourceId is not null ? $"{typeof(T).Name}:{resourceId}" : null;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void EmitTelemetry(string resourceType, string decision, string reason) {
		AuthorizationTelemetry.RecordDecision(
			AuthorizationTelemetry.StageResourceAccess,
			AuthorizationTelemetry.StepResourceAccessCheck,
			decision,
			reason,
			resourceType: resourceType);
	}
}
