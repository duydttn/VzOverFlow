using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace VzOverFlow.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}