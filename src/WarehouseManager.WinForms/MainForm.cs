using WarehouseManager.Application;
using WarehouseManager.Domain;

namespace WarehouseManager.WinForms;

public sealed class MainForm : Form
{
    private readonly IWarehouseOperationsService warehouseService;
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };

    private readonly Label totalQuantityLabel = BuildMetricLabel();
    private readonly Label distinctProductsLabel = BuildMetricLabel();
    private readonly Label expiringLabel = BuildMetricLabel();
    private readonly Label occupancyLabel = BuildMetricLabel();
    private readonly DataGridView dashboardExpiringGrid = BuildGrid();

    private readonly DataGridView productGrid = BuildGrid();
    private readonly TextBox productSkuBox = new();
    private readonly TextBox productNameBox = new();
    private readonly TextBox productSizeBox = new() { Text = "Стандарт" };
    private readonly NumericUpDown productWeightBox = BuildQty();
    private readonly TextBox productBatchBox = new();
    private readonly TextBox productSerialBox = new();
    private readonly CheckBox productExpirationCheckBox = new() { Text = "Указывать срок годности", AutoSize = true };
    private readonly DateTimePicker productExpirationBox = new() { Format = DateTimePickerFormat.Short, Enabled = false };

    private readonly DataGridView locationGrid = BuildGrid();
    private readonly TextBox locationCodeBox = new();
    private readonly TextBox locationZoneBox = new();
    private readonly TextBox locationRackBox = new();
    private readonly TextBox locationSlotBox = new();

    private readonly TextBox stockSearchBox = new() { PlaceholderText = "Поиск по артикулу, товару, ячейке, партии" };
    private readonly DataGridView stockGrid = BuildGrid();
    private readonly DataGridView movementGrid = BuildGrid();
    private readonly DataGridView inventoryGrid = BuildGrid();
    private readonly DataGridView inventoryLinesGrid = BuildGrid();
    private readonly DataGridView auditGrid = BuildGrid();
    private readonly TextBox inventoryReportText = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both };

    private readonly ComboBox receiptProductBox = BuildCombo();
    private readonly ComboBox receiptLocationBox = BuildCombo();
    private readonly NumericUpDown receiptQuantityBox = BuildQty();
    private readonly TextBox receiptBatchBox = new();
    private readonly TextBox receiptDocBox = new();
    private readonly DateTimePicker receiptExpiryBox = new() { Format = DateTimePickerFormat.Short };

    private readonly ComboBox transferProductBox = BuildCombo();
    private readonly ComboBox transferFromBox = BuildCombo();
    private readonly ComboBox transferToBox = BuildCombo();
    private readonly NumericUpDown transferQuantityBox = BuildQty();
    private readonly TextBox transferDocBox = new();

    private readonly ComboBox shipmentProductBox = BuildCombo();
    private readonly NumericUpDown shipmentQuantityBox = BuildQty();
    private readonly TextBox shipmentDocBox = new();

    private int? selectedInventoryId;

    public MainForm(IWarehouseOperationsService warehouseService)
    {
        this.warehouseService = warehouseService;
        Text = "Складской учет";
        Width = 1500;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        BuildLayout();
        ApplyLayoutFixes();
        productExpirationCheckBox.CheckedChanged += (_, _) => productExpirationBox.Enabled = productExpirationCheckBox.Checked;
        Shown += async (_, _) => await ReloadAllAsync();
    }

    private void BuildLayout()
    {
        tabs.TabPages.Add(BuildDashboardPage());
        tabs.TabPages.Add(BuildCatalogPage());
        tabs.TabPages.Add(BuildStockPage());
        tabs.TabPages.Add(BuildMovementsPage());
        tabs.TabPages.Add(BuildInventoryPage());
        tabs.TabPages.Add(BuildAdminPage());
        Controls.Add(tabs);
    }

    private TabPage BuildDashboardPage()
    {
        var page = new TabPage("Панель");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        for (var i = 0; i < 4; i++)
        {
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        metrics.Controls.Add(BuildMetricCard("Остаток, ед.", totalQuantityLabel), 0, 0);
        metrics.Controls.Add(BuildMetricCard("Товаров в остатках", distinctProductsLabel), 1, 0);
        metrics.Controls.Add(BuildMetricCard("Истекают за 14 дней", expiringLabel), 2, 0);
        metrics.Controls.Add(BuildMetricCard("Занятость ячеек, %", occupancyLabel), 3, 0);

        var expiringPanel = new Panel { Dock = DockStyle.Fill };
        expiringPanel.Controls.Add(dashboardExpiringGrid);
        expiringPanel.Controls.Add(new Label
        {
            Text = "Партии с ближайшим сроком годности",
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Height = 28
        });

        root.Controls.Add(metrics, 0, 0);
        root.Controls.Add(expiringPanel, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildCatalogPage()
    {
        var page = new TabPage("Справочники");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var forms = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        forms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        forms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        forms.Controls.Add(BuildProductGroup(), 0, 0);
        forms.Controls.Add(BuildLocationGroup(), 1, 0);

        var grids = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 800 };
        var left = new Panel { Dock = DockStyle.Fill };
        left.Controls.Add(productGrid);
        left.Controls.Add(new Label { Text = "Товары", Dock = DockStyle.Top, Height = 28, Font = new Font(Font, FontStyle.Bold) });
        var right = new Panel { Dock = DockStyle.Fill };
        right.Controls.Add(locationGrid);
        right.Controls.Add(new Label { Text = "Ячейки хранения", Dock = DockStyle.Top, Height = 28, Font = new Font(Font, FontStyle.Bold) });
        grids.Panel1.Controls.Add(left);
        grids.Panel2.Controls.Add(right);

        root.Controls.Add(forms, 0, 0);
        root.Controls.Add(grids, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildStockPage()
    {
        var page = new TabPage("Остатки");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var refreshButton = new Button { Text = "Обновить", AutoSize = true };
        refreshButton.Click += async (_, _) => await LoadStockAsync();
        stockSearchBox.Width = 420;
        stockSearchBox.TextChanged += async (_, _) => await LoadStockAsync();
        searchPanel.Controls.Add(stockSearchBox);
        searchPanel.Controls.Add(refreshButton);
        root.Controls.Add(searchPanel, 0, 0);
        root.Controls.Add(stockGrid, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildMovementsPage()
    {
        var page = new TabPage("Операции");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 980 };
        split.Panel1.Controls.Add(movementGrid);
        split.Panel2.AutoScroll = true;
        var actions = new TableLayoutPanel { Dock = DockStyle.Top, RowCount = 4, Padding = new Padding(8), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        actions.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actions.Controls.Add(BuildReceiptGroup(), 0, 0);
        actions.Controls.Add(BuildTransferGroup(), 0, 1);
        actions.Controls.Add(BuildShipmentGroup(), 0, 2);
        var refreshButton = new Button { Text = "Обновить журнал", Dock = DockStyle.Top };
        refreshButton.Click += async (_, _) => await LoadMovementsAsync();
        actions.Controls.Add(refreshButton, 0, 3);
        split.Panel2.Controls.Add(actions);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildInventoryPage()
    {
        var page = new TabPage("Инвентаризация");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 540 };
        split.Panel1.AutoScroll = true;
        split.Panel2.AutoScroll = true;
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(8) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var createFullButton = new Button { Text = "Полная", AutoSize = true };
        createFullButton.Click += async (_, _) => await CreateInventoryAsync(InventoryType.Full, "Все зоны");
        var createCycleButton = new Button { Text = "Циклическая по зоне A", AutoSize = true };
        createCycleButton.Click += async (_, _) => await CreateInventoryAsync(InventoryType.Cycle, "A");
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        actions.Controls.Add(createFullButton);
        actions.Controls.Add(createCycleButton);
        left.Controls.Add(actions, 0, 0);
        inventoryGrid.SelectionChanged += async (_, _) => await OnInventoryChangedAsync();
        left.Controls.Add(inventoryGrid, 0, 1);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        var inventoryButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        var countButton = new Button { Text = "Зафиксировать факт", AutoSize = true };
        countButton.Click += async (_, _) => await CountSelectedLineAsync(false);
        var recountButton = new Button { Text = "Повторный пересчет", AutoSize = true };
        recountButton.Click += async (_, _) => await CountSelectedLineAsync(true);
        var closeButton = new Button { Text = "Завершить", AutoSize = true };
        closeButton.Click += async (_, _) => await CompleteInventoryAsync();
        var reportButton = new Button { Text = "Сформировать акт", AutoSize = true };
        reportButton.Click += async (_, _) => await BuildInventoryReportAsync();
        inventoryButtons.Controls.Add(countButton);
        inventoryButtons.Controls.Add(recountButton);
        inventoryButtons.Controls.Add(closeButton);
        inventoryButtons.Controls.Add(reportButton);
        right.Controls.Add(inventoryButtons, 0, 0);
        right.Controls.Add(inventoryLinesGrid, 0, 1);
        right.Controls.Add(inventoryReportText, 0, 2);

        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildAdminPage()
    {
        var page = new TabPage("Администрирование");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var backupButton = new Button { Text = "Создать резервную копию", AutoSize = true };
        backupButton.Click += async (_, _) =>
        {
            try
            {
                var path = await warehouseService.CreateBackupAsync();
                MessageBox.Show($"Резервная копия создана:\n{path}", "Резервная копия", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        };
        var reloadButton = new Button { Text = "Обновить журнал", AutoSize = true };
        reloadButton.Click += async (_, _) => await LoadAuditAsync();
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        top.Controls.Add(backupButton);
        top.Controls.Add(reloadButton);
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(auditGrid, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private GroupBox BuildProductGroup()
    {
        var group = new GroupBox { Text = "Добавление товара", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddInputRow(layout, 0, "Артикул", productSkuBox);
        AddInputRow(layout, 1, "Наименование", productNameBox);
        AddInputRow(layout, 2, "Размер / фасовка", productSizeBox);
        AddInputRow(layout, 3, "Вес, кг", productWeightBox);
        AddInputRow(layout, 4, "Партия по умолчанию", productBatchBox);
        AddInputRow(layout, 5, "Серийный номер", productSerialBox);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.Controls.Add(productExpirationCheckBox, 1, 6);
        AddInputRow(layout, 7, "Срок годности", productExpirationBox);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true };
        var addButton = new Button { Text = "Сохранить товар", AutoSize = true };
        addButton.Click += async (_, _) => await CreateProductAsync();
        var refreshButton = new Button { Text = "Обновить список", AutoSize = true };
        refreshButton.Click += async (_, _) => await LoadCatalogsAsync();
        buttons.Controls.Add(addButton);
        buttons.Controls.Add(refreshButton);
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.Controls.Add(buttons, 1, 8);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildLocationGroup()
    {
        var group = new GroupBox { Text = "Добавление ячейки", Dock = DockStyle.Fill };
        var layout = BuildInputGrid();
        AddInputRow(layout, 0, "Код ячейки", locationCodeBox);
        AddInputRow(layout, 1, "Зона", locationZoneBox);
        AddInputRow(layout, 2, "Стеллаж", locationRackBox);
        AddInputRow(layout, 3, "Место", locationSlotBox);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = true };
        var addButton = new Button { Text = "Сохранить ячейку", AutoSize = true };
        addButton.Click += async (_, _) => await CreateLocationAsync();
        var refreshButton = new Button { Text = "Обновить список", AutoSize = true };
        refreshButton.Click += async (_, _) => await LoadCatalogsAsync();
        buttons.Controls.Add(addButton);
        buttons.Controls.Add(refreshButton);
        layout.Controls.Add(buttons, 1, 4);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildReceiptGroup()
    {
        var group = new GroupBox { Text = "Приемка", Dock = DockStyle.Fill };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddInputRow(layout, 0, "Товар", receiptProductBox);
        AddInputRow(layout, 1, "Ячейка", receiptLocationBox);
        AddInputRow(layout, 2, "Количество", receiptQuantityBox);
        AddInputRow(layout, 3, "Партия", receiptBatchBox);
        AddInputRow(layout, 4, "Документ", receiptDocBox);
        AddInputRow(layout, 5, "Срок годности", receiptExpiryBox);
        var button = new Button { Text = "Провести приемку", Dock = DockStyle.Fill };
        button.Click += async (_, _) => await ExecuteReceiptAsync();
        layout.Controls.Add(button, 0, 6);
        layout.SetColumnSpan(button, 2);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildTransferGroup()
    {
        var group = new GroupBox { Text = "Перемещение", Dock = DockStyle.Fill };
        var layout = BuildInputGrid();
        AddInputRow(layout, 0, "Товар", transferProductBox);
        AddInputRow(layout, 1, "Из ячейки", transferFromBox);
        AddInputRow(layout, 2, "В ячейку", transferToBox);
        AddInputRow(layout, 3, "Количество", transferQuantityBox);
        AddInputRow(layout, 4, "Документ", transferDocBox);
        var button = new Button { Text = "Переместить", Dock = DockStyle.Fill };
        button.Click += async (_, _) => await ExecuteTransferAsync();
        layout.Controls.Add(button, 0, 5);
        layout.SetColumnSpan(button, 2);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildShipmentGroup()
    {
        var group = new GroupBox { Text = "Отгрузка", Dock = DockStyle.Fill };
        var layout = BuildInputGrid();
        AddInputRow(layout, 0, "Товар", shipmentProductBox);
        AddInputRow(layout, 1, "Количество", shipmentQuantityBox);
        AddInputRow(layout, 2, "Документ", shipmentDocBox);
        var button = new Button { Text = "Отгрузить по FEFO", Dock = DockStyle.Fill };
        button.Click += async (_, _) => await ExecuteShipmentAsync();
        layout.Controls.Add(button, 0, 3);
        layout.SetColumnSpan(button, 2);
        group.Controls.Add(layout);
        return group;
    }

    private static TableLayoutPanel BuildInputGrid()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static void AddInputRow(TableLayoutPanel layout, int row, string title, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }

    private async Task ReloadAllAsync()
    {
        await LoadCatalogsAsync();
        await LoadDashboardAsync();
        await LoadStockAsync();
        await LoadMovementsAsync();
        await LoadInventoriesAsync();
        await LoadAuditAsync();
    }

    private async Task LoadCatalogsAsync()
    {
        var productCatalog = await warehouseService.GetProductCatalogAsync();
        SetGridData(productGrid, productCatalog, new Dictionary<string, string>
        {
            ["Id"] = "ID",
            ["Sku"] = "Артикул",
            ["Name"] = "Наименование",
            ["DefaultBatchNumber"] = "Партия по умолчанию",
            ["DefaultSerialNumber"] = "Серийный номер",
            ["DefaultExpirationDate"] = "Срок годности",
            ["Size"] = "Размер / фасовка",
            ["WeightKg"] = "Вес, кг",
            ["IsActive"] = "Активен"
        });

        var locations = await warehouseService.GetStorageLocationsAsync();
        SetGridData(locationGrid, locations, new Dictionary<string, string>
        {
            ["Id"] = "ID",
            ["Code"] = "Код ячейки",
            ["Zone"] = "Зона",
            ["Rack"] = "Стеллаж",
            ["Slot"] = "Место",
            ["IsActive"] = "Активна"
        });

        await LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        var products = await warehouseService.GetProductsAsync();
        var locations = await warehouseService.GetLocationsAsync();
        BindLookup(receiptProductBox, products);
        BindLookup(transferProductBox, products);
        BindLookup(shipmentProductBox, products);
        BindLookup(receiptLocationBox, locations);
        BindLookup(transferFromBox, locations);
        BindLookup(transferToBox, locations);
        receiptExpiryBox.Value = DateTime.Today.AddDays(7);
    }

    private async Task LoadDashboardAsync()
    {
        var dashboard = await warehouseService.GetDashboardAsync();
        totalQuantityLabel.Text = dashboard.TotalQuantity.ToString("0.###");
        distinctProductsLabel.Text = dashboard.DistinctProducts.ToString();
        expiringLabel.Text = dashboard.ExpiringLots.ToString();
        occupancyLabel.Text = dashboard.OccupancyRate.ToString("0.##");
        SetGridData(dashboardExpiringGrid, dashboard.Expiring, new Dictionary<string, string>
        {
            ["Sku"] = "Артикул",
            ["ProductName"] = "Товар",
            ["Location"] = "Ячейка",
            ["BatchNumber"] = "Партия",
            ["ExpirationDate"] = "Срок годности",
            ["Quantity"] = "Количество"
        });
    }

    private async Task LoadStockAsync()
    {
        var data = await warehouseService.GetStockAsync(stockSearchBox.Text);
        SetGridData(stockGrid, data, new Dictionary<string, string>
        {
            ["LotId"] = "Партия ID",
            ["ProductId"] = "Товар ID",
            ["Sku"] = "Артикул",
            ["ProductName"] = "Товар",
            ["Location"] = "Ячейка",
            ["Zone"] = "Зона",
            ["BatchNumber"] = "Партия",
            ["SerialNumber"] = "Серийный номер",
            ["ExpirationDate"] = "Срок годности",
            ["Quantity"] = "Количество",
            ["ReservedQuantity"] = "В резерве"
        });
    }

    private async Task LoadMovementsAsync()
    {
        var data = await warehouseService.GetMovementsAsync();
        SetGridData(movementGrid, data, new Dictionary<string, string>
        {
            ["CreatedUtc"] = "Дата",
            ["Type"] = "Операция",
            ["Sku"] = "Артикул",
            ["ProductName"] = "Товар",
            ["FromLocation"] = "Из ячейки",
            ["ToLocation"] = "В ячейку",
            ["Quantity"] = "Количество",
            ["DocumentNumber"] = "Документ",
            ["Note"] = "Примечание",
            ["Actor"] = "Исполнитель"
        });
    }

    private async Task LoadInventoriesAsync()
    {
        var data = await warehouseService.GetInventoriesAsync();
        SetGridData(inventoryGrid, data, new Dictionary<string, string>
        {
            ["Id"] = "ID",
            ["SessionNumber"] = "Номер",
            ["InventoryType"] = "Тип",
            ["Status"] = "Статус",
            ["Scope"] = "Область",
            ["CreatedUtc"] = "Создано",
            ["CompletedUtc"] = "Завершено",
            ["Positions"] = "Позиций",
            ["Discrepancies"] = "Расхождений"
        });
        await OnInventoryChangedAsync();
    }

    private async Task OnInventoryChangedAsync()
    {
        if (inventoryGrid.CurrentRow?.DataBoundItem is not InventorySessionDto selected)
        {
            inventoryLinesGrid.DataSource = null;
            selectedInventoryId = null;
            return;
        }

        selectedInventoryId = selected.Id;
        var data = await warehouseService.GetInventoryLinesAsync(selected.Id);
        SetGridData(inventoryLinesGrid, data, new Dictionary<string, string>
        {
            ["Id"] = "ID",
            ["Sku"] = "Артикул",
            ["ProductName"] = "Товар",
            ["Location"] = "Ячейка",
            ["BatchNumber"] = "Партия",
            ["ExpirationDate"] = "Срок годности",
            ["ExpectedQuantity"] = "Учетное количество",
            ["CountedQuantity"] = "Факт",
            ["RecountedQuantity"] = "Повторный пересчет",
            ["FinalQuantity"] = "Итог",
            ["Delta"] = "Отклонение",
            ["Comment"] = "Комментарий"
        });
        inventoryReportText.Text = string.Empty;
    }

    private async Task LoadAuditAsync()
    {
        var data = await warehouseService.GetAuditTrailAsync();
        SetGridData(auditGrid, data, new Dictionary<string, string>
        {
            ["CreatedUtc"] = "Дата",
            ["Actor"] = "Исполнитель",
            ["Action"] = "Действие",
            ["EntityName"] = "Сущность",
            ["EntityKey"] = "Ключ",
            ["Details"] = "Описание"
        });
    }

    private async Task CreateProductAsync()
    {
        try
        {
            await warehouseService.CreateProductAsync(new CreateProductRequest(
                productSkuBox.Text,
                productNameBox.Text,
                productSizeBox.Text,
                productWeightBox.Value,
                productBatchBox.Text,
                productSerialBox.Text,
                productExpirationCheckBox.Checked ? DateOnly.FromDateTime(productExpirationBox.Value.Date) : null));
            ClearProductForm();
            await ReloadAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task CreateLocationAsync()
    {
        try
        {
            await warehouseService.CreateStorageLocationAsync(new CreateStorageLocationRequest(
                locationCodeBox.Text,
                locationZoneBox.Text,
                locationRackBox.Text,
                locationSlotBox.Text));
            ClearLocationForm();
            await ReloadAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task ExecuteReceiptAsync()
    {
        try
        {
            await warehouseService.ReceiveAsync(new ReceiptRequest(
                GetLookupId(receiptProductBox, "товар"),
                GetLookupId(receiptLocationBox, "ячейку"),
                receiptQuantityBox.Value,
                receiptBatchBox.Text.Trim(),
                null,
                DateOnly.FromDateTime(receiptExpiryBox.Value.Date),
                receiptDocBox.Text.Trim(),
                "Приемка из интерфейса"));
            await ReloadAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task ExecuteTransferAsync()
    {
        try
        {
            await warehouseService.TransferAsync(new TransferRequest(
                GetLookupId(transferProductBox, "товар"),
                GetLookupId(transferFromBox, "исходную ячейку"),
                GetLookupId(transferToBox, "целевую ячейку"),
                transferQuantityBox.Value,
                transferDocBox.Text.Trim(),
                "Перемещение из интерфейса"));
            await ReloadAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task ExecuteShipmentAsync()
    {
        try
        {
            await warehouseService.ShipAsync(new ShipmentRequest(
                GetLookupId(shipmentProductBox, "товар"),
                shipmentQuantityBox.Value,
                shipmentDocBox.Text.Trim(),
                "Отгрузка из интерфейса"));
            await ReloadAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task CreateInventoryAsync(InventoryType inventoryType, string scope)
    {
        try
        {
            await warehouseService.CreateInventoryAsync(new CreateInventoryRequest(inventoryType, scope));
            await LoadInventoriesAsync();
            await LoadAuditAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task CountSelectedLineAsync(bool recount)
    {
        if (inventoryLinesGrid.CurrentRow?.DataBoundItem is not InventoryLineDto line)
        {
            return;
        }

        using var dialog = new QuantityDialog(recount ? "Повторный пересчет" : "Фактический остаток", line.FinalQuantity, line.Comment);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await warehouseService.CountInventoryLineAsync(line.Id, dialog.Quantity, dialog.Comment, recount);
            await OnInventoryChangedAsync();
            await LoadInventoriesAsync();
            await LoadAuditAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task CompleteInventoryAsync()
    {
        if (!selectedInventoryId.HasValue)
        {
            return;
        }

        try
        {
            await warehouseService.CompleteInventoryAsync(selectedInventoryId.Value);
            await LoadInventoriesAsync();
            await LoadAuditAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task BuildInventoryReportAsync()
    {
        if (!selectedInventoryId.HasValue)
        {
            return;
        }

        try
        {
            inventoryReportText.Text = await warehouseService.BuildInventoryReportAsync(selectedInventoryId.Value);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private void ClearProductForm()
    {
        productSkuBox.Clear();
        productNameBox.Clear();
        productSizeBox.Text = "Стандарт";
        productWeightBox.Value = 0;
        productBatchBox.Clear();
        productSerialBox.Clear();
        productExpirationCheckBox.Checked = false;
        productExpirationBox.Value = DateTime.Today;
    }

    private void ClearLocationForm()
    {
        locationCodeBox.Clear();
        locationZoneBox.Clear();
        locationRackBox.Clear();
        locationSlotBox.Clear();
    }

    private static void BindLookup(ComboBox comboBox, IReadOnlyList<LookupItemDto> items)
    {
        comboBox.DataSource = items.ToList();
        comboBox.DisplayMember = nameof(LookupItemDto.Label);
        comboBox.ValueMember = nameof(LookupItemDto.Id);
    }

    private static int GetLookupId(ComboBox comboBox, string what)
    {
        return comboBox.SelectedValue is int id
            ? id
            : throw new InvalidOperationException($"Выберите {what} из списка.");
    }

    private static void SetGridData<T>(DataGridView grid, IReadOnlyList<T> data, IReadOnlyDictionary<string, string> headers)
    {
        grid.DataSource = data.ToList();
        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (headers.TryGetValue(column.DataPropertyName, out var header))
            {
                column.HeaderText = header;
            }
        }
        grid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
    }

    private static Label BuildMetricLabel() =>
        new()
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 20, FontStyle.Bold)
        };

    private static ComboBox BuildCombo() => new() { DropDownStyle = ComboBoxStyle.DropDownList };

    private static NumericUpDown BuildQty() =>
        new()
        {
            DecimalPlaces = 3,
            Maximum = 100000,
            Minimum = 0,
            Increment = 1
        };

    private static Panel BuildMetricCard(string title, Control content)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke, Margin = new Padding(8) };
        panel.Controls.Add(content);
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        });
        return panel;
    }

    private static DataGridView BuildGrid() =>
        new()
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = true,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };

    private static void ShowError(Exception exception)
    {
        MessageBox.Show(exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    private void ApplyLayoutFixes()
    {
        FixCatalogLayout();
        FixOperationsLayout();
    }

    private void FixCatalogLayout()
    {
        if (tabs.TabPages.Count < 2 || tabs.TabPages[1].Controls.Count == 0)
        {
            return;
        }

        if (tabs.TabPages[1].Controls[0] is TableLayoutPanel root && root.RowStyles.Count > 0)
        {
            root.RowStyles[0].Height = 380;
        }

        foreach (var group in FindDescendants<GroupBox>(tabs.TabPages[1]))
        {
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        foreach (var panel in FindDescendants<FlowLayoutPanel>(tabs.TabPages[1]))
        {
            panel.AutoSize = true;
            panel.WrapContents = true;
            panel.AutoScroll = true;
            panel.Padding = new Padding(3);
            panel.MinimumSize = new Size(0, 44);
        }

        foreach (var table in FindDescendants<TableLayoutPanel>(tabs.TabPages[1]))
        {
            table.AutoScroll = true;
        }
    }

    private void FixOperationsLayout()
    {
        if (tabs.TabPages.Count < 4 || tabs.TabPages[3].Controls.Count == 0)
        {
            return;
        }

        if (tabs.TabPages[3].Controls[0] is not SplitContainer split)
        {
            return;
        }

        split.Panel2.AutoScroll = true;

        var actionsTable = split.Panel2.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (actionsTable is not null)
        {
            actionsTable.Dock = DockStyle.Top;
            actionsTable.AutoSize = true;
            actionsTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            if (actionsTable.RowStyles.Count >= 4)
            {
                actionsTable.RowStyles[0].Height = 260;
                actionsTable.RowStyles[1].Height = 230;
                actionsTable.RowStyles[2].Height = 170;
                actionsTable.RowStyles[3].SizeType = SizeType.AutoSize;
            }
        }

        foreach (var group in FindDescendants<GroupBox>(split.Panel2))
        {
            group.AutoSize = true;
            group.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            group.MinimumSize = new Size(420, 0);
        }

        foreach (var button in FindDescendants<Button>(split.Panel2))
        {
            button.AutoSize = true;
            if (button.Dock == DockStyle.Fill)
            {
                button.Dock = DockStyle.Top;
                button.Height = 36;
            }
        }
    }

    private static IEnumerable<T> FindDescendants<T>(Control root) where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var nested in FindDescendants<T>(child))
            {
                yield return nested;
            }
        }
    }
}

internal sealed class QuantityDialog : Form
{
    private readonly NumericUpDown quantityBox = new() { Dock = DockStyle.Top, DecimalPlaces = 3, Maximum = 100000, Minimum = 0 };
    private readonly TextBox commentBox = new() { Dock = DockStyle.Fill, Multiline = true };

    public QuantityDialog(string title, decimal quantity, string comment)
    {
        Text = title;
        Width = 420;
        Height = 220;
        StartPosition = FormStartPosition.CenterParent;
        quantityBox.Value = quantity;
        commentBox.Text = comment;

        var okButton = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Dock = DockStyle.Right };
        var cancelButton = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.Controls.Add(new Label { Text = "Количество", Dock = DockStyle.Fill }, 0, 0);
        layout.Controls.Add(quantityBox, 0, 1);
        layout.Controls.Add(commentBox, 0, 2);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public decimal Quantity => quantityBox.Value;
    public string Comment => commentBox.Text.Trim();
}
