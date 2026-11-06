using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PosWebApp.Hubs;

/// <summary>
/// SignalR hub for real-time cart badge updates.
/// Full implementation added in Task 10.
/// </summary>
[Authorize]
public class CartHub : Hub
{
    /// <summary>
    /// Broadcasts the updated cart item count to the authenticated user's browser connections.
    /// Called server-side by CartService after every cart mutation.
    /// </summary>
    public async Task NotifyCartUpdated(int itemCount)
    {
        await Clients.User(Context.UserIdentifier!)
                     .SendAsync("CartUpdated", itemCount);
    }
}
