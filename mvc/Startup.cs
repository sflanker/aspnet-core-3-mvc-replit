using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MvcSample {
  public class Startup {
    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      services.AddDefaultIdentity<IdentityUser>();

      services.AddSingleton<IUserStore<IdentityUser>>(MemoryUserStore.Instance);

      EnableReplitIFrameHosting(services);

      services.AddControllersWithViews();
      services.AddRazorPages();
    }

    private static void EnableReplitIFrameHosting(IServiceCollection services) {
      services.ConfigureApplicationCookie(options => {
        options.Cookie.SameSite = SameSiteMode.None;
      });

      services.Configure<ForwardedHeadersOptions>(options => {
        options.ForwardedHeaders =
          ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
      });

      services.AddAntiforgery(options => {
        options.SuppressXFrameOptionsHeader = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      app.UseForwardedHeaders();

      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      } else {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts();
      }

      // app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      /*
      // HTTP Request Debugging
      app.Use((context, next) => {
        Console.Error.WriteLine($"{context.Request.Method} {context.Request.Path} HTTP/1.1");
        foreach (var header in context.Request.Headers) {
          foreach (var value in header.Value) {
            Console.Error.WriteLine($"{header.Key}: {value}");
          }
        }
        Console.Error.WriteLine();

        return next();
      });
      */

      app.UseEndpoints(endpoints => {
        endpoints.MapControllerRoute(
          name: "default",
          pattern: "{controller=Home}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
      });
    }
  }
}
