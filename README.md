# Warehouse Manager v2

Демо-видео: [warehousev2.mp4](https://raw.githubusercontent.com/whoaskeddd/portfolio/main/assets/videos/warehousev2.mp4)

![Warehouse v2 Demo](https://raw.githubusercontent.com/whoaskeddd/portfolio/main/assets/videos/warehousev2.mp4)

## О проекте
Warehouse Manager v2 — desktop-приложение складского учета на `C#`, `WinForms`, `.NET 8`, `SQLite`.

## Архитектура
- `src/WarehouseManager.WinForms` — клиентское приложение (UI)
- `src/WarehouseManager.Infrastructure` — доступ к БД и реализация операций
- `src/WarehouseManager.Application` — контракты/DTO
- `src/WarehouseManager.Domain` — доменные сущности
- `tests/WarehouseManager.Tests` — тесты

## Важно про frontend/backend
- Отдельного web-frontend нет: UI реализован как WinForms desktop-клиент.
- Отдельного backend-сервиса нет: логика и БД работают локально в приложении.

## Требования
- Windows
- .NET SDK 8

## Запуск проекта
```powershell
cd C:\develop\portfolio\repos\warehouse-v2
dotnet restore WarehouseManager.slnx
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet run --project src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
```

## Где хранится база
После первого запуска SQLite создается автоматически:
`src/WarehouseManager.WinForms/bin/Debug/net8.0-windows/Data/warehouse.db`

## Использование функций
После запуска приложения:
1. Вкладка `Справочники`: создайте товары и ячейки.
2. Вкладка `Операции`: выполните приемку, перемещение, отгрузку.
3. Вкладка `Остатки`: проверьте текущие остатки и поиск.
4. Вкладка `Инвентаризация`: проведите полную или циклическую инвентаризацию.
5. Вкладка `Администрирование`: аудит и резервное копирование.

## Проверка и тесты
```powershell
cd C:\develop\portfolio\repos\warehouse-v2
dotnet build src/WarehouseManager.Infrastructure/WarehouseManager.Infrastructure.csproj
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet build tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj
dotnet test tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj --no-build
```

