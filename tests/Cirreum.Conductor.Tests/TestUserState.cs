namespace Cirreum.Conductor.Tests;

using Cirreum;
using Cirreum.Authorization;
using Cirreum.Security;
using System.Security.Claims;

public sealed class TestUserState : UserStateBase {

	public TestUserState()
		: this(
			principal: AnonymousUser.Shared,
			isAuthenticated: false,
			authType: AuthenticationLibraryType.None,
			timeZoneId: TimeZoneInfo.Local.Id) {
	}

	public TestUserState(
		ClaimsPrincipal principal,
		bool isAuthenticated = true,
		AuthenticationLibraryType authType = AuthenticationLibraryType.None,
		string? timeZoneId = null) {
		this._principal = principal;
		this._identity = (ClaimsIdentity)principal.Identity!;
		this._profile = new UserProfile(principal, timeZoneId ?? TimeZoneInfo.Local.Id);
		this._isAuthenticated = isAuthenticated;
		this._authenticationType = authType;

		if (isAuthenticated) {
			this.StartSession();
		}
	}

	/// <summary>
	/// Convenience factory for an authenticated test user with a simple principal.
	/// </summary>
	public static TestUserState CreateAuthenticated(
		string id = "user-123",
		string name = "Test User",
		AuthenticationLibraryType authType = AuthenticationLibraryType.None,
		string? timeZoneId = null,
		IApplicationUser? appUser = null) {
		var principal = CreatePrincipal(id, name);
		var state = new TestUserState(
			principal: principal,
			isAuthenticated: true,
			authType: authType,
			timeZoneId: timeZoneId ?? TimeZoneInfo.Local.Id) {
			// Override provider if you care about it in tests
			_profile = new UserProfile(principal, timeZoneId ?? TimeZoneInfo.Local.Id)
		};

		if (appUser is not null) {
			state.SetApplicationUser(appUser);
		}

		return state;
	}

	private static ClaimsPrincipal CreatePrincipal(string id, string name) {
		var identity = new ClaimsIdentity(
			authenticationType: "mock",
			nameType: ClaimTypes.Name,
			roleType: ClaimTypes.Role);

		identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));
		identity.AddClaim(new Claim(ClaimTypes.Name, name));
		//identity.AddClaim(new Claim(ClaimTypes.Anonymous, "true"));

		identity.AddClaim(new Claim(ClaimTypes.Role, ApplicationRoles.AppUserRole));

		return new ClaimsPrincipal(identity);

	}

}