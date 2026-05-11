namespace Cirreum.Security;

using Cirreum.Authorization;
using System.Security.Claims;

/// <summary>
/// Base implementation of <see cref="IUserState"/>
/// </summary>
public abstract class UserStateBase : IUserState {

	protected ClaimsPrincipal _principal = AnonymousUser.Shared;
	protected ClaimsIdentity _identity = AnonymousUser.Shared.Identity;
	protected UserProfile _profile = UserProfile.Anonymous;
	protected bool _isAuthenticated;
	protected AuthenticationLibraryType _authenticationType = AuthenticationLibraryType.None;

	private Type? _applicationUserType; // Track the actual type
	private bool _applicationUserLoaded;
	private IApplicationUser? _applicationUser;
	private bool _authenticationBoundaryResolved;
	private AuthenticationBoundary _authenticationBoundary = AuthenticationBoundary.None;
	private IActorContext? _actor;
	private bool _delegationResolved;

	/// <inheritdoc/>
	public bool IsAuthenticated => this._isAuthenticated;

	/// <inheritdoc/>
	public abstract bool IsAuthenticationComplete { get; }

	/// <inheritdoc/>
	public string Id => this._profile.Id;

	/// <inheritdoc/>
	public string Name => this._profile.Name;

	/// <inheritdoc/>
	public IdentityProviderType Provider => this._profile.Provider;

	/// <inheritdoc/>
	public AuthenticationLibraryType AuthenticationType => this._authenticationType;

	// UserProfile
	// -------------------------------------------------------------

	/// <inheritdoc/>
	public UserProfile Profile => this._profile;

	/// <inheritdoc/>
	public ClaimsPrincipal Principal => this._principal;

	/// <inheritdoc/>
	public ClaimsIdentity Identity => this._identity;

	protected void EnrichmentComplete() {
		this.Profile.IsEnriched = true;
	}

	// IUserSession
	// -------------------------------------------------------------

	/// <inheritdoc/>
	public DateTimeOffset? SessionStartTime { get; private set; }

	/// <inheritdoc/>
	public DateTimeOffset? LastActivityTime { get; private set; }

	protected void StartSession() {
		this.SessionStartTime = DateTimeOffset.UtcNow;
		this.LastActivityTime = DateTimeOffset.UtcNow;
	}

	protected void EndSession() {
		this.SessionStartTime = null;
		this.LastActivityTime = null;
	}

	/// <inheritdoc/>
	public void UpdateActivity() {
		if (this.IsAuthenticated && this.SessionStartTime.HasValue) {
			this.LastActivityTime = DateTimeOffset.UtcNow;
		}
	}

	/// <inheritdoc/>
	public bool IsSessionExpired(TimeSpan timeout) {
		if (!this.SessionStartTime.HasValue || !this.LastActivityTime.HasValue) {
			return true;
		}

		return DateTimeOffset.UtcNow - this.LastActivityTime.Value > timeout;
	}

	// AuthenticationBoundary
	// -------------------------------------------------------------

	/// <inheritdoc/>
	public AuthenticationBoundary AuthenticationBoundary => this._authenticationBoundary;

	/// <inheritdoc/>
	public bool IsAuthenticationBoundaryResolved => this._authenticationBoundaryResolved;

	/// <summary>
	/// Sets the access scope for the current instance.
	/// </summary>
	/// <param name="scope">The access scope to assign to the instance.</param>
	protected virtual void SetAuthenticationBoundary(AuthenticationBoundary scope) {
		this._authenticationBoundary = scope;
		this._authenticationBoundaryResolved = true;
	}


	// IApplicationUser
	// -------------------------------------------------------------

	/// <inheritdoc/>
	public IApplicationUser? ApplicationUser => this._applicationUser;

	/// <inheritdoc/>
	public bool IsApplicationUserLoaded => this._applicationUserLoaded;

	/// <inheritdoc/>
	public T? GetApplicationUser<T>() where T : class, IApplicationUser {
		if (this._applicationUser is not null
			&& this._applicationUserType is not null
			&& this._applicationUserType == typeof(T)) {
			return (T)this._applicationUser;
		}
		return null;
	}

	protected virtual void SetApplicationUser(IApplicationUser? applicationUser) {
		this._applicationUser = applicationUser;
		this._applicationUserType = applicationUser?.GetType();
		this._applicationUserLoaded = true;
	}

	protected virtual void ClearApplicationUser() {
		this._applicationUser = null;
		this._applicationUserType = null;
		this._applicationUserLoaded = false;
	}


	// Actor / Delegation
	// -------------------------------------------------------------

	/// <inheritdoc/>
	public IActorContext? Actor => this._actor;

	/// <inheritdoc/>
	public bool IsDelegated => this._actor is not null;

	/// <inheritdoc/>
	public bool IsDelegationResolved => this._delegationResolved;

	/// <summary>
	/// Resolves the delegation state for this user state — both successful delegation
	/// (non-null <paramref name="actor"/>) and "delegation processed, none applied"
	/// (null <paramref name="actor"/>) cases. Called exactly once by the upstream auth
	/// handler (via the server's user state accessor) after the delegation orchestration
	/// step completes.
	/// </summary>
	/// <param name="actor">
	/// The actor snapshot when delegation was applied, or <see langword="null"/> when
	/// the auth handler completed delegation processing without finding a delegation
	/// to apply (no evidence present in the request).
	/// </param>
	/// <exception cref="InvalidOperationException">
	/// Thrown when delegation has already been resolved on this user state. Delegation
	/// chains (actor-of-actor) are not supported — each user state can represent at most
	/// one resolution event. The framework's M2M auth handler enforces this naturally by
	/// running at most once per invocation; this guard catches programmatic misuse.
	/// </exception>
	protected virtual void SetActor(IActorContext? actor) {
		if (this._delegationResolved) {
			throw new InvalidOperationException(
				"Delegation has already been resolved on this user state. " +
				"Delegation chains (actor-of-actor) are not supported.");
		}
		this._actor = actor;
		this._delegationResolved = true;
	}

}