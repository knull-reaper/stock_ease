using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Stock_Ease.Hubs
{
    public class TransactionHub : Hub
    {
        public async Task SendWeightUpdate(int productId, double newWeight)
        {
            await Clients.All.SendAsync("ReceiveWeightUpdate", productId, newWeight);
        }
    }
}
