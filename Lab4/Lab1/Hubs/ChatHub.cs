using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lab1.Hubs;

[Authorize]
public class ChatHub : Hub
{
}
