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

	private IApplicationUser? _applicationUser;
	private bool _applicationUserLoaded;
	private Type? _applicationUserType; // Track the actual type

	/// <inheritdoc/>
	public bool IsAuthenticated => this._isAuthenticated;

	/// <summary>
	/// Gets a value indicating whether the user state pipeline has completed
	/// and all state is settled. Consumers can safely read properties without
	/// encountering stale or partial data.
	/// </summary>
	public abstract bool IsReady { get; }

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

}