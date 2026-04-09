using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarehouseManager.Application;
using WarehouseManager.Domain;

namespace WarehouseManager.Infrastructure;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<InventorySession> InventorySessions => Set<InventorySession>();
    public DbSet<InventoryLine> InventoryLines => Set<InventoryLine>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<StorageLocation>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<StockLot>().Property(x => x.Quantity).HasPrecision(18, 3);
        modelBuilder.Entity<StockLot>().Property(x => x.ReservedQuantity).HasPrecision(18, 3);
        modelBuilder.Entity<StockMovement>().Property(x => x.Quantity).HasPrecision(18, 3);
        modelBuilder.Entity<InventoryLine>().Property(x => x.ExpectedQuantity).HasPrecision(18, 3);
        modelBuilder.Entity<InventoryLine>().Property(x => x.CountedQuantity).HasPrecision(18, 3);
        modelBuilder.Entity<InventoryLine>().Property(x => x.RecountedQuantity).HasPrecision(18, 3);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWarehouseManagement(this IServiceCollection services, string databasePath)
    {
        services.AddLogging();
        services.AddDbContextFactory<WarehouseDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton(new DatabaseSettings(databasePath));
        services.AddScoped<IWarehouseOperationsService, WarehouseOperationsService>();
        return services;
    }
}

public sealed record DatabaseSettings(string DatabasePath);

public sealed class WarehouseOperationsService(
    IDbContextFactory<WarehouseDbContext> dbContextFactory,
    DatabaseSettings databaseSettings,
    ILogger<WarehouseOperationsService> logger) : IWarehouseOperationsService
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var locations = new[]
        {
            new StorageLocation { Code = "A-01-01", Zone = "A", Rack = "01", Slot = "01" },
            new StorageLocation { Code = "A-01-02", Zone = "A", Rack = "01", Slot = "02" },
            new StorageLocation { Code = "B-01-01", Zone = "B", Rack = "01", Slot = "01" },
            new StorageLocation { Code = "C-02-03", Zone = "C", Rack = "02", Slot = "03" }
        };

        var products = new[]
        {
            new Product { Sku = "MILK-1L", Name = "Молоко 1л", BatchNumber = "M-240401", ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)), Size = "1л", WeightKg = 1.02m },
            new Product { Sku = "CHEESE-45", Name = "Сыр 45%", BatchNumber = "C-240329", ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)), Size = "0.5кг", WeightKg = 0.50m },
            new Product { Sku = "JUICE-APPLE", Name = "Сок яблочный", BatchNumber = "J-240405", ExpirationDate = DateOnly.FromDateTime(DateTime.Today.AddDays(40)), Size = "1л", WeightKg = 1.00m }
        };

        db.AddRange(locations);
        db.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);

        var stockLots = new[]
        {
            new StockLot { ProductId = products[0].Id, LocationId = locations[0].Id, BatchNumber = "M-240401", ExpirationDate = products[0].ExpirationDate, Quantity = 120m },
            new StockLot { ProductId = products[1].Id, LocationId = locations[1].Id, BatchNumber = "C-240329", ExpirationDate = products[1].ExpirationDate, Quantity = 64m },
            new StockLot { ProductId = products[2].Id, LocationId = locations[2].Id, BatchNumber = "J-240405", ExpirationDate = products[2].ExpirationDate, Quantity = 90m }
        };
        db.StockLots.AddRange(stockLots);
        db.StockMovements.AddRange(stockLots.Select(lot => new StockMovement
        {
            ProductId = lot.ProductId,
            ToLocationId = lot.LocationId,
            BatchNumber = lot.BatchNumber,
            ExpirationDate = lot.ExpirationDate,
            Quantity = lot.Quantity,
            MovementType = MovementType.Receipt,
            DocumentNumber = $"SEED-{lot.ProductId}",
            Note = "Начальный остаток"
        }));

        var session = new InventorySession
        {
            SessionNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-001",
            InventoryType = InventoryType.Cycle,
            Scope = "A"
        };
        db.InventorySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        db.InventoryLines.AddRange(
            new InventoryLine
            {
                InventorySessionId = session.Id,
                ProductId = products[0].Id,
                LocationId = locations[0].Id,
                BatchNumber = "M-240401",
                ExpirationDate = products[0].ExpirationDate,
                ExpectedQuantity = 120m
            },
            new InventoryLine
            {
                InventorySessionId = session.Id,
                ProductId = products[1].Id,
                LocationId = locations[1].Id,
                BatchNumber = "C-240329",
                ExpirationDate = products[1].ExpirationDate,
                ExpectedQuantity = 64m
            });
        db.AuditEvents.Add(new AuditEvent
        {
            Action = "Инициализация",
            EntityName = "Warehouse",
            EntityKey = "seed",
            Details = "Созданы демонстрационные данные склада"
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.OrderBy(x => x.Name).Select(x => new LookupItemDto(x.Id, $"{x.Sku} | {x.Name}")).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.StorageLocations.Where(x => x.IsActive).OrderBy(x => x.Code).Select(x => new LookupItemDto(x.Id, $"{x.Code} ({x.Zone})")).ToListAsync(cancellationToken);
    }

    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var expiringQuery = db.StockLots.Include(x => x.Product).Include(x => x.Location)
            .Where(x => x.Quantity > 0 && x.ExpirationDate != null && x.ExpirationDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(14)));
        var totalQuantity = await db.StockLots.SumAsync(x => x.Quantity, cancellationToken);
        var distinctProducts = await db.StockLots.Select(x => x.ProductId).Distinct().CountAsync(cancellationToken);
        var expiringLots = await expiringQuery.CountAsync(cancellationToken);
        var expiring = await expiringQuery.OrderBy(x => x.ExpirationDate).Take(10)
            .Select(x => new ExpiringLotDto(x.Product.Sku, x.Product.Name, x.Location.Code, x.BatchNumber, x.ExpirationDate, x.Quantity))
            .ToListAsync(cancellationToken);
        var activeLocations = await db.StorageLocations.CountAsync(x => x.IsActive, cancellationToken);
        var occupiedLocations = await db.StockLots.Where(x => x.Quantity > 0).Select(x => x.LocationId).Distinct().CountAsync(cancellationToken);
        var openInventories = await db.InventorySessions.CountAsync(x => x.Status != InventoryStatus.Completed, cancellationToken);
        var occupancyRate = activeLocations == 0 ? 0m : Math.Round((decimal)occupiedLocations / activeLocations * 100m, 2);
        return new DashboardSummaryDto(totalQuantity, distinctProducts, expiringLots, openInventories, occupancyRate, expiring);
    }

    public async Task<IReadOnlyList<StockOverviewDto>> GetStockAsync(string? search, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.StockLots.Include(x => x.Product).Include(x => x.Location).Where(x => x.Quantity > 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Product.Sku.Contains(search) || x.Product.Name.Contains(search) || x.Location.Code.Contains(search) || x.BatchNumber.Contains(search));
        }

        return await query.OrderBy(x => x.Product.Name).ThenBy(x => x.ExpirationDate)
            .Select(x => new StockOverviewDto(x.Id, x.ProductId, x.Product.Sku, x.Product.Name, x.Location.Code, x.Location.Zone, x.BatchNumber, x.SerialNumber, x.ExpirationDate, x.Quantity, x.ReservedQuantity))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovementDto>> GetMovementsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.StockMovements.Include(x => x.Product).Include(x => x.FromLocation).Include(x => x.ToLocation)
            .OrderByDescending(x => x.CreatedUtc).Take(200)
            .Select(x => new MovementDto(x.CreatedUtc, x.MovementType.ToString(), x.Product.Sku, x.Product.Name, x.FromLocation != null ? x.FromLocation.Code : null, x.ToLocation != null ? x.ToLocation.Code : null, x.Quantity, x.DocumentNumber, x.Note, x.Actor))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventorySessionDto>> GetInventoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await db.InventorySessions
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new
            {
                x.Id,
                x.SessionNumber,
                x.InventoryType,
                x.Status,
                x.Scope,
                x.CreatedUtc,
                x.CompletedUtc
            })
            .ToListAsync(cancellationToken);

        var lineStats = await db.InventoryLines
            .GroupBy(x => x.InventorySessionId)
            .Select(group => new
            {
                InventorySessionId = group.Key,
                Positions = group.Count(),
                Discrepancies = group.Count(line =>
                    (line.RecountedQuantity ?? line.CountedQuantity ?? line.ExpectedQuantity) != line.ExpectedQuantity)
            })
            .ToListAsync(cancellationToken);

        var statsBySessionId = lineStats.ToDictionary(
            x => x.InventorySessionId,
            x => (x.Positions, x.Discrepancies));

        return sessions
            .Select(x =>
            {
                var stats = statsBySessionId.GetValueOrDefault(x.Id, (0, 0));
                return new InventorySessionDto(
                    x.Id,
                    x.SessionNumber,
                    x.InventoryType.ToString(),
                    x.Status.ToString(),
                    x.Scope,
                    x.CreatedUtc,
                    x.CompletedUtc,
                    stats.Item1,
                    stats.Item2);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryLineDto>> GetInventoryLinesAsync(int inventorySessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.InventoryLines.Include(x => x.Product).Include(x => x.Location)
            .Where(x => x.InventorySessionId == inventorySessionId)
            .OrderBy(x => x.Location.Code).ThenBy(x => x.Product.Name)
            .Select(x => new InventoryLineDto(
                x.Id,
                x.Product.Sku,
                x.Product.Name,
                x.Location.Code,
                x.BatchNumber,
                x.ExpirationDate,
                x.ExpectedQuantity,
                x.CountedQuantity,
                x.RecountedQuantity,
                x.RecountedQuantity ?? x.CountedQuantity ?? x.ExpectedQuantity,
                (x.RecountedQuantity ?? x.CountedQuantity ?? x.ExpectedQuantity) - x.ExpectedQuantity,
                x.Comment))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEventDto>> GetAuditTrailAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEvents.OrderByDescending(x => x.CreatedUtc).Take(200)
            .Select(x => new AuditEventDto(x.CreatedUtc, x.Actor, x.Action, x.EntityName, x.EntityKey, x.Details))
            .ToListAsync(cancellationToken);
    }

    public async Task ReceiveAsync(ReceiptRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositive(request.Quantity);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var lot = await db.StockLots.FirstOrDefaultAsync(x => x.ProductId == request.ProductId && x.LocationId == request.LocationId && x.BatchNumber == request.BatchNumber && x.SerialNumber == request.SerialNumber, cancellationToken);
        if (lot is null)
        {
            lot = new StockLot
            {
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                BatchNumber = request.BatchNumber,
                SerialNumber = request.SerialNumber,
                ExpirationDate = request.ExpirationDate
            };
            db.StockLots.Add(lot);
        }

        lot.Quantity += request.Quantity;
        lot.ExpirationDate = request.ExpirationDate;
        db.StockMovements.Add(new StockMovement
        {
            ProductId = request.ProductId,
            ToLocationId = request.LocationId,
            BatchNumber = request.BatchNumber,
            SerialNumber = request.SerialNumber,
            ExpirationDate = request.ExpirationDate,
            Quantity = request.Quantity,
            MovementType = MovementType.Receipt,
            DocumentNumber = request.DocumentNumber,
            Note = request.Note
        });
        db.AuditEvents.Add(CreateAudit("Приемка", "Receipt", request.DocumentNumber, $"Принято {request.Quantity:0.###} ед."));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task TransferAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromLocationId == request.ToLocationId)
        {
            throw new InvalidOperationException("Источник и целевая ячейка совпадают.");
        }

        ValidatePositive(request.Quantity);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lots = await db.StockLots.Where(x => x.ProductId == request.ProductId && x.LocationId == request.FromLocationId && x.Quantity > 0)
            .OrderBy(x => x.ExpirationDate).ThenBy(x => x.CreatedUtc).ToListAsync(cancellationToken);
        var remaining = request.Quantity;

        foreach (var sourceLot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var allocated = Math.Min(sourceLot.Quantity, remaining);
            sourceLot.Quantity -= allocated;

            var targetLot = await db.StockLots.FirstOrDefaultAsync(x =>
                x.ProductId == sourceLot.ProductId &&
                x.LocationId == request.ToLocationId &&
                x.BatchNumber == sourceLot.BatchNumber &&
                x.SerialNumber == sourceLot.SerialNumber, cancellationToken);

            if (targetLot is null)
            {
                targetLot = new StockLot
                {
                    ProductId = sourceLot.ProductId,
                    LocationId = request.ToLocationId,
                    BatchNumber = sourceLot.BatchNumber,
                    SerialNumber = sourceLot.SerialNumber,
                    ExpirationDate = sourceLot.ExpirationDate
                };
                db.StockLots.Add(targetLot);
            }

            targetLot.Quantity += allocated;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = sourceLot.ProductId,
                FromLocationId = request.FromLocationId,
                ToLocationId = request.ToLocationId,
                BatchNumber = sourceLot.BatchNumber,
                SerialNumber = sourceLot.SerialNumber,
                ExpirationDate = sourceLot.ExpirationDate,
                Quantity = allocated,
                MovementType = MovementType.Transfer,
                DocumentNumber = request.DocumentNumber,
                Note = request.Note
            });

            remaining -= allocated;
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException("Недостаточно остатка для перемещения.");
        }

        db.AuditEvents.Add(CreateAudit("Перемещение", "Transfer", request.DocumentNumber, $"Перемещено {request.Quantity:0.###} ед."));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ShipAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePositive(request.Quantity);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lots = await db.StockLots.Where(x => x.ProductId == request.ProductId && x.Quantity > 0)
            .OrderBy(x => x.ExpirationDate).ThenBy(x => x.CreatedUtc).ToListAsync(cancellationToken);
        var remaining = request.Quantity;

        foreach (var lot in lots)
        {
            if (remaining <= 0)
            {
                break;
            }

            var available = lot.Quantity - lot.ReservedQuantity;
            if (available <= 0)
            {
                continue;
            }

            var shipped = Math.Min(available, remaining);
            lot.Quantity -= shipped;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = lot.ProductId,
                FromLocationId = lot.LocationId,
                BatchNumber = lot.BatchNumber,
                SerialNumber = lot.SerialNumber,
                ExpirationDate = lot.ExpirationDate,
                Quantity = shipped,
                MovementType = MovementType.Shipment,
                DocumentNumber = request.DocumentNumber,
                Note = request.Note
            });
            remaining -= shipped;
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException("Недостаточно доступного остатка для отгрузки.");
        }

        db.AuditEvents.Add(CreateAudit("Отгрузка", "Shipment", request.DocumentNumber, $"Отгружено {request.Quantity:0.###} ед."));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> CreateInventoryAsync(CreateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var scope = string.IsNullOrWhiteSpace(request.Scope) ? "Все зоны" : request.Scope.Trim();
        var session = new InventorySession
        {
            SessionNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{await db.InventorySessions.CountAsync(cancellationToken) + 1:000}",
            InventoryType = request.InventoryType,
            Scope = scope,
            Status = InventoryStatus.InProgress
        };
        db.InventorySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var lotsQuery = db.StockLots.Include(x => x.Location).Where(x => x.Quantity >= 0);
        if (!scope.Equals("Все зоны", StringComparison.OrdinalIgnoreCase))
        {
            lotsQuery = lotsQuery.Where(x => x.Location.Zone == scope);
        }

        var lots = await lotsQuery.ToListAsync(cancellationToken);
        db.InventoryLines.AddRange(lots.Select(lot => new InventoryLine
        {
            InventorySessionId = session.Id,
            ProductId = lot.ProductId,
            LocationId = lot.LocationId,
            BatchNumber = lot.BatchNumber,
            SerialNumber = lot.SerialNumber,
            ExpirationDate = lot.ExpirationDate,
            ExpectedQuantity = lot.Quantity
        }));
        db.AuditEvents.Add(CreateAudit("Инвентаризация", "Inventory", session.SessionNumber, $"Создана инвентаризация {scope}."));
        await db.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task CountInventoryLineAsync(int lineId, decimal countedQuantity, string comment, bool isRecount, CancellationToken cancellationToken = default)
    {
        ValidateNonNegative(countedQuantity);
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var line = await db.InventoryLines.Include(x => x.InventorySession).FirstOrDefaultAsync(x => x.Id == lineId, cancellationToken)
            ?? throw new InvalidOperationException("Строка инвентаризации не найдена.");

        if (line.InventorySession.Status == InventoryStatus.Completed)
        {
            throw new InvalidOperationException("Завершенную инвентаризацию редактировать нельзя.");
        }

        if (isRecount)
        {
            line.RecountedQuantity = countedQuantity;
        }
        else
        {
            line.CountedQuantity = countedQuantity;
        }

        line.Comment = comment;
        db.AuditEvents.Add(CreateAudit(isRecount ? "Повторный пересчет" : "Пересчет", "InventoryLine", line.Id.ToString(), $"Факт={countedQuantity:0.###}."));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteInventoryAsync(int inventorySessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.InventorySessions.FirstOrDefaultAsync(x => x.Id == inventorySessionId, cancellationToken)
            ?? throw new InvalidOperationException("Инвентаризация не найдена.");
        if (session.Status == InventoryStatus.Completed)
        {
            return;
        }

        session.Status = InventoryStatus.Completed;
        session.CompletedUtc = DateTime.UtcNow;
        db.AuditEvents.Add(CreateAudit("Закрытие инвентаризации", "Inventory", session.SessionNumber, "Инвентаризация закрыта и заблокирована от редактирования."));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> BuildInventoryReportAsync(int inventorySessionId, CancellationToken cancellationToken = default)
    {
        var session = (await GetInventoriesAsync(cancellationToken)).First(x => x.Id == inventorySessionId);
        var lines = await GetInventoryLinesAsync(inventorySessionId, cancellationToken);
        var discrepancies = lines.Where(x => x.Delta != 0).ToList();
        return
            $"Сличительная ведомость {session.SessionNumber}{Environment.NewLine}" +
            $"Тип: {session.InventoryType}{Environment.NewLine}" +
            $"Статус: {session.Status}{Environment.NewLine}" +
            $"Область: {session.Scope}{Environment.NewLine}" +
            $"Позиций: {session.Positions}{Environment.NewLine}" +
            $"Расхождений: {session.Discrepancies}{Environment.NewLine}{Environment.NewLine}" +
            string.Join(Environment.NewLine, discrepancies.Select(x =>
                $"{x.Location} | {x.Sku} | учет {x.ExpectedQuantity:0.###} | факт {x.FinalQuantity:0.###} | delta {x.Delta:+0.###;-0.###;0}"));
    }

    public Task<string> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var backupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
        Directory.CreateDirectory(backupDirectory);
        var target = Path.Combine(backupDirectory, $"warehouse-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        File.Copy(databaseSettings.DatabasePath, target, overwrite: true);
        logger.LogInformation("Backup created at {Target}", target);
        return Task.FromResult(target);
    }

    private static AuditEvent CreateAudit(string action, string entityName, string entityKey, string details) =>
        new()
        {
            Action = action,
            EntityName = entityName,
            EntityKey = entityKey,
            Details = details
        };

    private static void ValidatePositive(decimal value)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException("Количество должно быть больше нуля.");
        }
    }

    private static void ValidateNonNegative(decimal value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException("Количество не может быть отрицательным.");
        }
    }
}
