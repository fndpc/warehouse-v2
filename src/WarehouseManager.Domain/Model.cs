namespace WarehouseManager.Domain;

public enum MovementType
{
    Receipt = 1,
    Shipment = 2,
    Transfer = 3,
    InventoryAdjustment = 4
}

public enum InventoryType
{
    Full = 1,
    Selective = 2,
    Cycle = 3
}

public enum InventoryStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public string Size { get; set; } = "Стандарт";
    public decimal WeightKg { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StorageLocation
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Zone { get; set; }
    public required string Rack { get; set; }
    public required string Slot { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StockLot
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int LocationId { get; set; }
    public StorageLocation Location { get; set; } = null!;
    public string BatchNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StockMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? FromLocationId { get; set; }
    public StorageLocation? FromLocation { get; set; }
    public int? ToLocationId { get; set; }
    public StorageLocation? ToLocation { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public MovementType MovementType { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Actor { get; set; } = "Кладовщик";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class InventorySession
{
    public int Id { get; set; }
    public required string SessionNumber { get; set; }
    public InventoryType InventoryType { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.InProgress;
    public string Scope { get; set; } = "Все зоны";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }
    public string CreatedBy { get; set; } = "Кладовщик";
    public ICollection<InventoryLine> Lines { get; set; } = [];
}

public sealed class InventoryLine
{
    public int Id { get; set; }
    public int InventorySessionId { get; set; }
    public InventorySession InventorySession { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int LocationId { get; set; }
    public StorageLocation Location { get; set; } = null!;
    public string BatchNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal? CountedQuantity { get; set; }
    public decimal? RecountedQuantity { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class AuditEvent
{
    public int Id { get; set; }
    public string Actor { get; set; } = "Кладовщик";
    public required string Action { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
