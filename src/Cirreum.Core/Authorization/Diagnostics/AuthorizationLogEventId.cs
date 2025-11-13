namespace Cirreum.Authorization.Diagnostics;
internal static class AuthorizationLogEventId {

    public const int BeginAuthorizingResourceId = 10_001;
    public const int AuthorizingResourceDeniedId = 10_002;
    public const int AuthorizingResourceDeniedById = 10_003;
    public const int AuthorizingResourceAllowedId = 10_004;
    public const int AuthorizingResourceUnknownErrorId = 10_005;
}