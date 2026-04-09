using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseManager.Application;
using WarehouseManager.Infrastructure;

namespace WarehouseManager.Tests;

[TestClass]
public sealed class WarehouseOperationsServiceTests
{
    [TestMethod]
    public async Task ShipAsync_ReducesAvailableStock()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var scope = await CreateScopeAsync(connection);
        var service = scope.ServiceProvider.GetRequiredService<IWarehouseOperationsService>();

        var productId = (await service.GetProductsAsync()).First().Id;
        var initialQty = (await service.GetStockAsync(null)).Where(x => x.ProductId == productId).Sum(x => x.Quantity);

        await service.ShipAsync(new ShipmentRequest(productId, 10m, "SHIP-001", "test"));

        var afterQty = (await service.GetStockAsync(null)).Where(x => x.ProductId == productId).Sum(x => x.Quantity);
        Assert.AreEqual(initialQty - 10m, afterQty);
    }

    [TestMethod]
    public async Task CompletedInventory_CannotBeEdited()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var scope = await CreateScopeAsync(connection);
        var service = scope.ServiceProvider.GetRequiredService<IWarehouseOperationsService>();

        var inventoryId = (await service.GetInventoriesAsync()).First().Id;
        var lineId = (await service.GetInventoryLinesAsync(inventoryId)).First().Id;
        await service.CompleteInventoryAsync(inventoryId);

        try
        {
            await service.CountInventoryLineAsync(lineId, 99m, "late edit", false);
            Assert.Fail("Expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static async Task<IServiceScope> CreateScopeAsync(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<WarehouseDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton(new DatabaseSettings(Path.Combine(Path.GetTempPath(), $"warehouse-{Guid.NewGuid():N}.db")));
        services.AddSingleton<ILogger<WarehouseOperationsService>>(NullLogger<WarehouseOperationsService>.Instance);
        services.AddScoped<IWarehouseOperationsService, WarehouseOperationsService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IWarehouseOperationsService>();
        await service.InitializeAsync();
        return scope;
    }
}
