using GleamTech.AspNet.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MiResiliencia.Controllers.API;
using MiResiliencia.Helpers;
using MiResiliencia.Models;
using MiResiliencia.Resources;
using NLog;
using NLog.Web;
using System.Globalization;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{

    var cultureInfo = new CultureInfo("es-BO");
    CultureInfo[] supportedCultures = new[]
               {
            new CultureInfo("es")
        };

    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new RequestCulture(cultureInfo);
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
        options.RequestCultureProviders = new List<IRequestCultureProvider>
                    {
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider()
                    };

    });

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<MiResilienciaContext>();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<MiResilienciaContext>();

    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add(new AddHistoryHeaderAttribute());

    }).AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    }).AddDataAnnotationsLocalization(
                     options =>
                     {
                         options.DataAnnotationLocalizerProvider = (type, factory) =>
                             factory.Create(typeof(Global));
                     }).AddSessionStateTempDataProvider();
    builder.Services.AddSession();
    builder.Services.AddGleamTech();

    //-----------------------------------------------------------------
    // Calculation Kernel Service
    builder.Services.AddTransient<DamageExtentService>();
    //-----------------------------------------------------------------

    var app = builder.Build();

    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(cultureInfo),
        SupportedCultures = new List<CultureInfo>
    {
        cultureInfo,
    },
        SupportedUICultures = new List<CultureInfo>
    {
        cultureInfo,
    }
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();

        app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
            string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));
    }
    else
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();
    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseGleamTech();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(
            name: "MapImageProxy",
            pattern: "MapImageProxy/{Layer}/{TileMatrix}/{TileCol}/{TileRow}",
            defaults: new { controller = "MapImageProxy", action = "GetProxyImage" }
            );
        endpoints.MapControllerRoute(
            name: "GeoServerProxy",
            pattern: "proxy/{*path}",
            defaults: new { controller = "GeoserverProxy", action = "Http" }
            );
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}").RequireAuthorization();
    });


    string baseDir = app.Environment.ContentRootPath;
    AppDomain.CurrentDomain.SetData("DataDirectory", System.IO.Path.Combine(baseDir, "App_Data"));

    app.MapRazorPages();
    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}