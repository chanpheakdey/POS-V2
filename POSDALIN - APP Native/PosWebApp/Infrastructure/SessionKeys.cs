namespace PosWebApp.Infrastructure;

/// <summary>
/// Centralised string constants for keys used in ASP.NET Core session state.
/// </summary>
public static class SessionKeys
{
    /// <summary>
    /// The Current_Cart — the session-based collection of items for the customer
    /// currently being served by staff. Stored as a JSON-serialised <c>CartState</c>.
    /// </summary>
    public const string Cart = "Current_Cart";
}
