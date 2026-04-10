# Warehouse Manager

Desktop-приложение для базового складского учета на `C#`, `WinForms`, `.NET 8` и `SQLite`.

Проект запускается локально без внешней инфраструктуры. База данных создается автоматически при первом старте приложения.

## Возможности

- Справочники товаров и ячеек хранения
- Добавление и удаление товаров
- Добавление и удаление ячеек хранения
- Просмотр каталога товаров и списка ячеек
- Приемка товара на склад
- Перемещение между ячейками
- Отгрузка по логике `FEFO`
- Просмотр текущих остатков с поиском
- Журнал движений
- Полная и циклическая инвентаризация
- Фиксация факта и повторный пересчет
- Формирование текстового акта расхождений
- Журнал аудита
- Создание резервной копии базы

## Ограничения удаления

Удаление товара и ячейки реализовано как деактивация.

- Нельзя удалить товар, если по нему есть остатки
- Нельзя удалить ячейку, если в ней есть остатки
- История движений и аудита при этом сохраняется

## Стек

- `C#`
- `.NET 8`
- `WinForms`
- `Entity Framework Core`
- `SQLite`
- `Microsoft.Extensions.DependencyInjection`

## Структура проекта

- `src/WarehouseManager.Domain`  
  Доменная модель: товары, ячейки, остатки, движения, инвентаризация, аудит.

- `src/WarehouseManager.Application`  
  DTO, контракты и интерфейс прикладного сервиса.

- `src/WarehouseManager.Infrastructure`  
  Работа с `SQLite`, реализация складских операций, аудит, резервное копирование.

- `src/WarehouseManager.WinForms`  
  Пользовательский интерфейс приложения.

- `tests/WarehouseManager.Tests`  
  Автотесты для сервисного слоя.

## Запуск

Из корня репозитория:

```powershell
dotnet restore WarehouseManager.slnx
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet run --project src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
```

## Где хранится база

После запуска файл базы создается автоматически по пути:

```text
src/WarehouseManager.WinForms/bin/Debug/net8.0-windows/Data/warehouse.db
```

Если нужна полностью пустая база, удалите этот файл перед следующим запуском приложения.

## Сценарий первого запуска

1. Откройте вкладку `Справочники`
2. Создайте товары
3. Создайте ячейки хранения
4. Перейдите на вкладку `Операции` и выполните первую приемку
5. Используйте вкладки `Остатки`, `Инвентаризация` и `Администрирование`

## Проверка и тесты

```powershell
dotnet build src/WarehouseManager.Infrastructure/WarehouseManager.Infrastructure.csproj
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet build tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj
dotnet test tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj --no-build
```

## Текущее состояние

Проект ориентирован на локальную работу в Windows.

Сейчас в приложении нет:

- многопользовательского режима
- разграничения ролей и прав доступа
- интеграций с ERP, ТСД, сканерами и печатными устройствами
- экспорта отчетов в `PDF` или `Excel`
