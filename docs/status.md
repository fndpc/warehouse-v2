# Status

## Snapshot
- Current phase: M4. Валидация, polishing и документация запуска
- Plan file: `docs/plans.md`
- Status: green
- Last updated: 2026-04-09

## Done
- Проанализирован `README.md`.
- Зафиксированы целевая архитектура и execution docs.
- Подняты solution и слои `Domain`, `Application`, `Infrastructure`, `WinForms`, `Tests`.
- Реализованы SQLite persistence, seed-данные, складские сервисы и аудит.
- Собран WinForms UI для дашборда, остатков, движений, инвентаризаций и backup.
- Пройдены сборки ключевых проектов и `dotnet test`.

## In Progress
- Финальная ручная smoke-проверка WinForms UI на локальном запуске.

## Next
- Прогнать ручной сценарий запуска desktop-приложения и зафиксировать результат smoke-check.

## Decisions Made
- Выбран `net8.0-windows` вместо preview-target, чтобы опираться на стабильный runtime.
- Используется WinForms + EF Core SQLite, потому что это лучше всего покрывает требования `README.md` без внешней инфраструктуры.
- Приложение строится как модульный desktop monolith с чистым разделением слоев, чтобы сохранить простоту деплоя и расширяемость.
- Для валидации использовались проектные `dotnet build`, так как `dotnet build` по `slnx` в sandbox этой среды возвращал недиагностируемый workload-resolution failure.

## Assumptions In Force
- Единственная рабочая роль в текущем срезе: `Кладовщик`.
- Генерация печатных документов реализуется внутри desktop-приложения без интеграции с офисными пакетами.
- Backup выполняется как копирование SQLite-базы в пользовательскую папку резервных копий.

## Commands
```sh
dotnet restore WarehouseManager.slnx
dotnet build src/WarehouseManager.Infrastructure/WarehouseManager.Infrastructure.csproj
dotnet build src/WarehouseManager.WinForms/WarehouseManager.WinForms.csproj
dotnet build tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj
dotnet test tests/WarehouseManager.Tests/WarehouseManager.Tests.csproj --no-build
```

## Current Blockers
- None

## Audit Log
| Date | Milestone | Files | Commands | Result | Next |
| --- | --- | --- | --- | --- | --- |
| 2026-04-09 | M1 | `README.md`, `docs/plans.md`, `docs/status.md`, `docs/test-plan.md` | `Get-Content README.md -Encoding UTF8` | pass | создать solution и проекты |
| 2026-04-09 | M1-M3 | `src/*`, `tests/*` | `dotnet restore WarehouseManager.slnx` | pass | сборка проектов |
| 2026-04-09 | M2-M4 | `src/WarehouseManager.Infrastructure`, `src/WarehouseManager.WinForms`, `tests/WarehouseManager.Tests` | `dotnet build ...`, `dotnet test ... --no-build` | pass | manual smoke desktop UI |

## Smoke / Demo Checklist
- [ ] Приложение стартует и инициализирует SQLite на локальном диске
- [ ] Основной сценарий приемки/перемещения/отгрузки проходит на seeded данных
- [x] Инвентаризация создается, закрывается и блокируется от редактирования
