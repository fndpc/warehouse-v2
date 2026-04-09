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
    private readonly TextBox stockSearchBox = new() { PlaceholderText = "Поиск по SKU, товару, ячейке, партии" };
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
        Text = "Warehouse Manager";
        Width = 1500;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        BuildLayout();
        Shown += async (_, _) => await ReloadAllAsync();
    }

    private void BuildLayout()
    {
        tabs.TabPages.Add(BuildDashboardPage());
        tabs.TabPages.Add(BuildStockPage());
        tabs.TabPages.Add(BuildMovementsPage());
        tabs.TabPages.Add(BuildInventoryPage());
        tabs.TabPages.Add(BuildAdminPage());
        Controls.Add(tabs);
    }

    private TabPage BuildDashboardPage()
    {
        var page = new TabPage("Дашборд");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var metrics = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        for (var i = 0; i < 4; i++)
        {
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        metrics.Controls.Add(BuildMetricCard("Остаток, ед.", totalQuantityLabel), 0, 0);
        metrics.Controls.Add(BuildMetricCard("SKU в обороте", distinctProductsLabel), 1, 0);
        metrics.Controls.Add(BuildMetricCard("Истекают до 14 дней", expiringLabel), 2, 0);
        metrics.Controls.Add(BuildMetricCard("Занятость ячеек, %", occupancyLabel), 3, 0);

        var expiringPanel = new Panel { Dock = DockStyle.Fill };
        expiringPanel.Controls.Add(dashboardExpiringGrid);
        expiringPanel.Controls.Add(new Label
        {
            Text = "Критичные партии по FEFO",
            Dock = DockStyle.Top,
            Font = new Font(Font, FontStyle.Bold),
            Height = 28
        });

        root.Controls.Add(metrics, 0, 0);
        root.Controls.Add(expiringPanel, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private TabPage BuildStockPage()
    {
        var page = new TabPage("Номенклатура и остатки");
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
        var page = new TabPage("Движение товара");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 980 };
        split.Panel1.Controls.Add(movementGrid);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(8) };
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
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
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(8) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var createFullButton = new Button { Text = "Полная", AutoSize = true };
        createFullButton.Click += async (_, _) => await CreateInventoryAsync(InventoryType.Full, "Все зоны");
        var createCycleButton = new Button { Text = "Циклическая зона A", AutoSize = true };
        createCycleButton.Click += async (_, _) => await CreateInventoryAsync(InventoryType.Cycle, "A");
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        actions.Controls.Add(createFullButton);
        actions.Controls.Add(createCycleButton);
        left.Controls.Add(actions, 0, 0);
        inventoryGrid.SelectionChanged += async (_, _) => await OnInventoryChangedAsync();
        left.Controls.Add(inventoryGrid, 0, 1);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        var inventoryButtons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var countButton = new Button { Text = "Факт", AutoSize = true };
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
        var page = new TabPage("Отчеты и администрирование");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var backupButton = new Button { Text = "Создать backup", AutoSize = true };
        backupButton.Click += async (_, _) =>
        {
            try
            {
                var path = await warehouseService.CreateBackupAsync();
                MessageBox.Show($"Резервная копия создана:\n{path}", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        };
        var reloadButton = new Button { Text = "Обновить журнал", AutoSize = true };
        reloadButton.Click += async (_, _) => await LoadAuditAsync();
        var top = new FlowLayoutPanel { Dock = DockStyle.Fill };
        top.Controls.Add(backupButton);
        top.Controls.Add(reloadButton);
        root.Controls.Add(top, 0, 0);
        root.Controls.Add(auditGrid, 0, 1);
        page.Controls.Add(root);
        return page;
    }

    private GroupBox BuildReceiptGroup()
    {
        var group = new GroupBox { Text = "Приемка", Dock = DockStyle.Fill };
        var layout = BuildInputGrid();
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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return layout;
    }

    private static void AddInputRow(TableLayoutPanel layout, int row, string title, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }

    private async Task ReloadAllAsync()
    {
        await LoadLookupsAsync();
        await LoadDashboardAsync();
        await LoadStockAsync();
        await LoadMovementsAsync();
        await LoadInventoriesAsync();
        await LoadAuditAsync();
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
        dashboardExpiringGrid.DataSource = dashboard.Expiring.ToList();
    }

    private async Task LoadStockAsync()
    {
        stockGrid.DataSource = (await warehouseService.GetStockAsync(stockSearchBox.Text)).ToList();
    }

    private async Task LoadMovementsAsync()
    {
        movementGrid.DataSource = (await warehouseService.GetMovementsAsync()).ToList();
    }

    private async Task LoadInventoriesAsync()
    {
        inventoryGrid.DataSource = (await warehouseService.GetInventoriesAsync()).ToList();
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
        inventoryLinesGrid.DataSource = (await warehouseService.GetInventoryLinesAsync(selected.Id)).ToList();
        inventoryReportText.Text = string.Empty;
    }

    private async Task LoadAuditAsync()
    {
        auditGrid.DataSource = (await warehouseService.GetAuditTrailAsync()).ToList();
    }

    private async Task ExecuteReceiptAsync()
    {
        try
        {
            await warehouseService.ReceiveAsync(new ReceiptRequest(GetLookupId(receiptProductBox), GetLookupId(receiptLocationBox), receiptQuantityBox.Value, receiptBatchBox.Text.Trim(), null, DateOnly.FromDateTime(receiptExpiryBox.Value.Date), receiptDocBox.Text.Trim(), "Приемка из UI"));
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
            await warehouseService.TransferAsync(new TransferRequest(GetLookupId(transferProductBox), GetLookupId(transferFromBox), GetLookupId(transferToBox), transferQuantityBox.Value, transferDocBox.Text.Trim(), "Перемещение из UI"));
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
            await warehouseService.ShipAsync(new ShipmentRequest(GetLookupId(shipmentProductBox), shipmentQuantityBox.Value, shipmentDocBox.Text.Trim(), "Отгрузка из UI"));
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

    private static void BindLookup(ComboBox comboBox, IReadOnlyList<LookupItemDto> items)
    {
        comboBox.DataSource = items.ToList();
        comboBox.DisplayMember = nameof(LookupItemDto.Label);
        comboBox.ValueMember = nameof(LookupItemDto.Id);
    }

    private static int GetLookupId(ComboBox comboBox)
    {
        return comboBox.SelectedValue is int id ? id : throw new InvalidOperationException("Выберите значение из списка.");
    }

    private static Label BuildMetricLabel() => new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 20, FontStyle.Bold) };
    private static ComboBox BuildCombo() => new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private static NumericUpDown BuildQty() => new() { DecimalPlaces = 3, Maximum = 100000, Minimum = 0, Increment = 1 };

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
