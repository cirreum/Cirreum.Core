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

	public bool IsAuthenticated => this._isAuthenticated;

	public string Id => this._profile.Id;

	public string Name => this._profile.Name;

	public IdentityProviderType Provider => this._profile.Provider;

	public AuthenticationLibraryType AuthenticationType => this._authenticationType;

	public UserProfile Profile => this._profile;

	public ClaimsPrincipal Principal => this._principal;

	public ClaimsIdentity Identity => this._identity;

	protected void EnrichmentComplete() {
		this.Profile.IsEnriched = true;
	}

	#region IUserSession

	public DateTimeOffset? SessionStartTime { get; private set; }

	public DateTimeOffset? LastActivityTime { get; private set; }

	protected void StartSession() {
		this.SessionStartTime = DateTimeOffset.UtcNow;
		this.LastActivityTime = DateTimeOffset.UtcNow;
	}

	protected void EndSession() {
		this.SessionStartTime = null;
		this.LastActivityTime = null;
	}

	public void UpdateActivity() {
		if (this.IsAuthenticated && this.SessionStartTime.HasValue) {
			this.LastActivityTime = DateTimeOffset.UtcNow;
		}
	}

	public bool IsSessionExpired(TimeSpan timeout) {
		if (!this.SessionStartTime.HasValue || !this.LastActivityTime.HasValue) {
			return true;
		}

		return DateTimeOffset.UtcNow - this.LastActivityTime.Value > timeout;
	}

	#endregion

	public IApplicationUser? ApplicationUser => this._applicationUser;

	public bool IsApplicationUserLoaded => this._applicationUserLoaded;

	public T? GetApplicationUser<T>() where T : class, IApplicationUser {
		if (_applicationUser is not null
			&& _applicationUserType is not null
			&& _applicationUserType == typeof(T)) {
			return (T)_applicationUser;
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