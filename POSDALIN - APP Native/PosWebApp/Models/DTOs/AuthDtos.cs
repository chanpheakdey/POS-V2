namespace PosWebApp.Models.DTOs;

/// <summary>
/// Result returned by the Backend_Service login endpoint.
/// </summary>
public record LoginResult(
    bool IsSuccess,
    bool IsLockedOut,
    string? ErrorMessage,
    int UserID,
    string UserLevel,
    string FullName);
