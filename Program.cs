using HelpDesk.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<HelpDeskContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("HelpDeskDB")));

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(4);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // necessário para servir os anexos gravados em wwwroot/uploads

            app.UseRouting();
            app.UseSession(); // precisa vir antes de UseAuthorization/roteamento dos controllers
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Tickets}/{action=Index}/{id?}");

            app.Run();
        }
    }
}