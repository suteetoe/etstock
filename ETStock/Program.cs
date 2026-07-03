using Avalonia;
using ETStock.Data;
using ETStock.Data.Repositories;
using ETStock.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace ETStock;

internal sealed class Program
{
    private const string DefaultConnectionStringEnvironmentVariable = "ETSTOCK_DB_DSN";

    public static IServiceProvider? Services { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                var connectionString = GetDatabaseConnectionString(ctx.Configuration);
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
                services.AddScoped<ICompanyRepository, CompanyRepository>();
                services.AddScoped<IMonthlyStockRepository, MonthlyStockRepository>();
                services.AddScoped<IAbbrInvoiceRepository, AbbrInvoiceRepository>();
                services.AddScoped<IInvoicePrintService, InvoicePrintService>();
                services.AddScoped<IInvoiceGeneratorService, InvoiceGeneratorService>();
                services.AddSingleton<IExcelImportService, ExcelImportService>();
                services.AddSingleton<IExcelExportService, ExcelExportService>();
            })
            .Build();

        Services = host.Services;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var environmentVariableName =
            configuration["Database:ConnectionStringEnvironmentVariable"]
            ?? DefaultConnectionStringEnvironmentVariable;

        var connectionString =
            configuration[environmentVariableName]
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Database connection string is not configured. Set the {environmentVariableName} environment variable.");
        }

        return connectionString;
    }
}
