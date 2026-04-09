using WarehouseManager.Domain;

namespace WarehouseManager.Application;

public sealed record LookupItemDto(int Id, string Label);

public sealed record DashboardSummaryDto(
    decimal TotalQuantity,
    int DistinctProducts,
    int ExpiringLots,
    int OpenInventories,
    decimal OccupancyRate,
    IReadOnlyList<ExpiringLotDto> Expiring);

public sealed record ExpiringLotDto(
    string Sku,
    string ProductName,
    string Location,
    string BatchNumber,
    DateOnly? ExpirationDate,
    decimal Quantity);

public sealed record StockOverviewDto(
    int LotId,
    int ProductId,
    string Sku,
    string ProductName,
    string Location,
    string Zone,
    string BatchNumber,
    string? SerialNumber,
    DateOnly? ExpirationDate,
    decimal Quantity,
    decimal ReservedQuantity);

public sealed record MovementDto(
    DateTime CreatedUtc,
    string Type,
    string Sku,
    string ProductName,
    string? FromLocation,
    string? ToLocation,
    decimal Quantity,
    string DocumentNumber,
    string Note,
    string Actor);

public sealed record InventorySessionDto(
    int Id,
    string SessionNumber,
    string InventoryType,
    string Status,
    string Scope,
    DateTime CreatedUtc,
    DateTime? CompletedUtc,
    int Positions,
    int Discrepancies);

public sealed record InventoryLineDto(
    int Id,
    string Sku,
    string ProductName,
    string Location,
    string BatchNumber,
    DateOnly? ExpirationDate,
    decimal ExpectedQuantity,
    decimal? CountedQuantity,
    decimal? RecountedQuantity,
    decimal FinalQuantity,
    decimal Delta,
    string Comment);

public sealed record AuditEventDto(
    DateTime CreatedUtc,
    string Actor,
    string Action,
    string EntityName,
    string EntityKey,
    string Details);

public sealed record ReceiptRequest(
    int ProductId,
    int LocationId,
    decimal Quantity,
    string BatchNumber,
    string? SerialNumber,
    DateOnly? ExpirationDate,
    string DocumentNumber,
    string Note);

public sealed record TransferRequest(
    int ProductId,
    int FromLocationId,
    int ToLocationId,
    decimal Quantity,
    string DocumentNumber,
    string Note);

public sealed record ShipmentRequest(
    int ProductId,
    decimal Quantity,
    string DocumentNumber,
    string Note);

public sealed record CreateInventoryRequest(
    InventoryType InventoryType,
    string Scope);

public interface IWarehouseOperationsService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupItemDto>> GetLocationsAsync(CancellationToken cancellationToken = default);
    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockOverviewDto>> GetStockAsync(string? search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovementDto>> GetMovementsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventorySessionDto>> GetInventoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLineDto>> GetInventoryLinesAsync(int inventorySessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEventDto>> GetAuditTrailAsync(CancellationToken cancellationToken = default);
    Task ReceiveAsync(ReceiptRequest request, CancellationToken cancellationToken = default);
    Task TransferAsync(TransferRequest request, CancellationToken cancellationToken = default);
    Task ShipAsync(ShipmentRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateInventoryAsync(CreateInventoryRequest request, CancellationToken cancellationToken = default);
    Task CountInventoryLineAsync(int lineId, decimal countedQuantity, string comment, bool isRecount, CancellationToken cancellationToken = default);
    Task CompleteInventoryAsync(int inventorySessionId, CancellationToken cancellationToken = default);
    Task<string> BuildInventoryReportAsync(int inventorySessionId, CancellationToken cancellationToken = default);
    Task<string> CreateBackupAsync(CancellationToken cancellationToken = default);
}
