using tournamentApp.AuditService;
using tournamentApp.Data.Repository;

namespace tournamentApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

			builder.Services.AddSingleton<string>(connection => builder.Configuration.GetConnectionString("DefaultConnection"));

            builder.Services.AddTransient<RegisterUser>();

            builder.Services.AddTransient<LoginUser>();

            builder.Services.AddTransient<CreateTournament>();

            builder.Services.AddTransient<DisplayTournaments>();

            builder.Services.AddTransient<DisplayPlayers>();

            builder.Services.AddTransient<Profile>();

			builder.Services.AddTransient<Matches>();

			builder.Services.AddTransient<DisplayRatingPlayers>();

			builder.Services.AddSingleton<PasswordHasherService>();

			builder.Services.AddSingleton<FileLogger>();

			builder.Services.AddSession();

			var app = builder.Build();

			app.UseSession();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
