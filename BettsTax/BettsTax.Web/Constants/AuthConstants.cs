namespace BettsTax.Web.Constants;

/// <summary>
/// Central place to keep authentication/authorization constants in sync between services.
/// </summary>
public static class AuthConstants
{
    public const string AccessTokenCookieName = "ctis_access_token";
    public const string RefreshTokenCookieName = "ctis_refresh_token";
    public const string CsrfCookieName = "ctis_csrf";
    public const string CsrfHeaderName = "X-CSRF-TOKEN";
}
