using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using netcore_postgres_oauth_boiler.Models;
using netcore_postgres_oauth_boiler.Models.Config;
using netcore_postgres_oauth_boiler.Policies;
using System;

namespace netcore_postgres_oauth_boiler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(1);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Latest);
            services.AddControllersWithViews();

            services.AddDbContext<DatabaseContext>(options =>
                 options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            // Get the Google config from appsettings.json
            services.Configure<OAuthConfig>(Configuration.GetSection("OAuthConfig"));

            // adding authorization service
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IAuthorizationHandler, AuthHandler>();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Authorized",
                                  policy => policy.Requirements.Add(new AuthRequirement(true)));

                options.AddPolicy("UnAuthorized",
                                 policy => policy.Requirements.Add(new AuthRequirement(false)));
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                    options =>
                    {
                        // The login path is index as a workaround to avoid adding unneeded complexity with identity checking. Currently, if a user is authenticated
                        // and a route only accepts unautheticated users, the connection triggers an 'unauthenticated' response and redirects to '/'
                        options.LoginPath = new PathString("/");
                        options.LogoutPath = new PathString("/Session/Logout");
                        options.AccessDeniedPath = new PathString("/");
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DatabaseContext context)
        {
            // Database setup
            context.Database.Migrate();

            // Nginx compatibility
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                         name: "default",
                         pattern: "{controller=Home}/{action=Index}/{id?}");


                endpoints.MapControllerRoute("Catch", "{*url}", defaults: new { controller = "Home", action = "NotFound" });
            });
        }
    }
}
