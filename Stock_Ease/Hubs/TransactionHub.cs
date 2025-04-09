using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Stock_Ease.Hubs
{
    public class TransactionHub : Hub
    {
        public async Task SendWeightUpdate(int productId, double newWeight)
        {
            // Clients will listen for the "ReceiveWeightUpdate" message
            await Clients.All.SendAsync("ReceiveWeightUpdate", productId, newWeight);
        }
    }
}
