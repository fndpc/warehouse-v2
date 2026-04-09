# Test Plan

## Source
- Task: разработать production-ready WinForms приложение для администрирования склада
- Plan file: `docs/plans.md`
- Status file: `docs/status.md`
- Repo context: весь репозиторий
- Last updated: 2026-04-09

## Validation Scope
- In scope: сборка solution, бизнес-правила учета остатков, движения товара, инвентаризация, аудит, backup, навигация и ключевые WinForms экраны.
- Out of scope: аппаратные интеграции, многопользовательская авторизация, UI automation через внешние frameworks.

## Environment / Fixtures
- Data fixtures: seeded SQLite база с зонами хранения, товарами, паллетами, движениями и открытой/закрытой инвентаризацией.
- External dependencies: локальный файл SQLite, .NET 8 Windows Desktop runtime.
- Setup assumptions: Windows host с установленным .NET SDK; запуск из корня репозитория.

## Test Levels

### Unit
- Проверка расчета доступного остатка, aging/FEFO метрик и агрегатов дашборда.
- Проверка правил домена: запрет редактирования завершенной инвентаризации, валидация количества, смена статусов документов.

### Integration
- Проверка `DbContext` + сервисов приемки, отгрузки, перемещения и закрытия инвентаризации на SQLite.
- Проверка записи аудита и создания резервной копии базы.

### End-to-End / Smoke
- Старт приложения и первичная инициализация БД.
- Просмотр дашборда и каталога.
- Создание приемки, перемещения, отгрузки.
- Создание и завершение инвентаризации с формированием расхождений и preview отчета.

## Negative / Edge Cases
- Попытка отгрузить больше доступного остатка.
- Попытка изменить завершенную инвентаризацию.
- Перемещение в несуществующую или неактивную локацию.
- Приемка/отгрузка товара с истекшим сроком годности и проверка сигнализации на дашборде.
- Ошибка доступа к файлу БД при backup.

## Acceptance Gates
- [x] `dotnet restore WarehouseManager.slnx`
- [x] `dotnet build src/WarehouseManager.Infrastructure/WarehouseManager.Infrastructure.csproj`
- [x] `dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj`
- [x] `dotnet build tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj`
- [x] `dotnet test tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj --no-build`
- [ ] Ручной smoke старта WinForms приложения
- [ ] Демонстрация основного складского сценария на seeded данных

## Release / Demo Readiness
- [ ] Core scenario works end to end
- [ ] Primary regression checks are green
- [ ] No blocker-level known issue remains
- [ ] Demo steps are reproducible

## Command Matrix
```sh
dotnet restore WarehouseManager.slnx
dotnet build src/WarehouseManager.Infrastructure/WarehouseManager.Infrastructure.csproj
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet build tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj
dotnet test tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj --no-build
dotnet run --project src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
```

## Open Risks
- Полноценная автоматизация WinForms UI не входит в текущий срез, поэтому часть приемки останется manual smoke.
- `dotnet build` по `WarehouseManager.slnx` в sandbox среды может падать без диагностики из-за workload resolver; project-level build проходит успешно.

## Deferred Coverage
- Автоматизированные UI tests.
- Экспорт печатных форм в PDF/Excel.
- Расширенная ролевая модель beyond single-role warehouse clerk.
