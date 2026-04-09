using Microsoft.Extensions.DependencyInjection;
using WarehouseManager.Application;
using WarehouseManager.Infrastructure;

namespace WarehouseManager.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, "warehouse.db");

        services.AddWarehouseManagement(databasePath);
        services.AddScoped<MainForm>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var warehouseService = scope.ServiceProvider.GetRequiredService<IWarehouseOperationsService>();
        warehouseService.InitializeAsync().GetAwaiter().GetResult();
        System.Windows.Forms.Application.Run(scope.ServiceProvider.GetRequiredService<MainForm>());
    }
}
