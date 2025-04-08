using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Stock_Ease.Data;
using Stock_Ease.Hubs;
using Stock_Ease.Controllers;

namespace Stock_Ease
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllersWithViews();


            builder.Services.AddDbContext<Stock_EaseContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Stock_EaseContext")));


            builder.Services.AddSignalR();


            builder.Services.AddScoped<AlertsController>();

            var app = builder.Build();


            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");


            app.MapHub<TransactionHub>("/transactionHub");

            app.Run();
        }
    }
}
