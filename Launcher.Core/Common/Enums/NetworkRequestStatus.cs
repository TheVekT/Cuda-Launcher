namespace Launcher.Core.Common.Enums;

public enum NetworkRequestStatus
{
    Success,
    RateLimited,   // HTTP 429 Too Many Requests
    Unauthorized,  // HTTP 401 Unauthorized
    Forbidden,     // HTTP 403 Forbidden
    NotFound,      // HTTP 404 Not Found
    BadRequest,    // HTTP 400 Bad Request
    ServerError,   // HTTP 5xx Server Errors
    Error          // Generic or network error
}
