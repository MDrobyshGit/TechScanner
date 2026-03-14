---
title: 'Codebase Technology Scanner - Full MVP'
slug: 'codebase-technology-scanner-mvp'
created: '2026-03-14'
status: 'implementation-complete'
stepsCompleted: [1, 2, 3, 4, 5]
tech_stack:
  - .NET 8 ASP.NET Core Web API (Clean Architecture)
  - React 18 + TypeScript + Vite
  - MSSQL Server Express (EF Core 8)
  - OpenAI API / Azure OpenAI (LLM enrichment)
  - LibGit2Sharp or git CLI (repo cloning)
files_to_modify:
  - src/TechScanner.Api/ (new - Controllers, DTOs, Program.cs)
  - src/TechScanner.Core/ (new - Domain models, Interfaces)
  - src/TechScanner.Infrastructure/ (new - EF Core, LLM, Git)
  - src/TechScanner.Scanner/ (new - all scanning logic)
  - frontend/src/ (new - React app)
  - tests/TechScanner.Tests/ (new - xUnit tests)
code_patterns:
  - Clean Architecture (Core / Infrastructure / Application layers)
  - Repository pattern over EF Core
  - Background scanning via IHostedService + Channel<T>
  - Strategy pattern for manifest parsers
  - SSE (Server-Sent Events) for scan progress streaming
test_patterns:
  - xUnit for backend unit tests
  - Moq for mocking interfaces
  - Vitest + React Testing Library for frontend
---

# Tech-Spec: Codebase Technology Scanner — Full MVP

**Created:** 2026-03-14

## Overview

### Problem Statement

Разработчики и тимлиды тратят значительное время на ручной аудит технологического стека проектов: находят все фреймворки/библиотеки с версиями, определяют — активно ли они используются в коде или лишь числятся в зависимостях, и оценивают актуальность версий (заброшен ли проект/библиотека, требуется ли обновление). Этот процесс особенно болезнен при работе с незнакомыми кодовыми базами или legacy-проектами.

### Solution

Веб-приложение (React + .NET API), которое принимает проект тремя способами (путь к папке / ZIP-архив / GitHub-GitLab URL), рекурсивно сканирует все конфигурационные и манифест-файлы, использует LLM для извлечения технологий из нестандартных форматов и обогащения результатов (статус поддержки, рекомендации по обновлению), и отображает интерактивный отчёт с разбивкой по технологиям и их состоянию.

### Scope

**In Scope:**
- Три способа подачи проекта: путь к локальной папке на сервере, загрузка ZIP-архива, URL GitHub/GitLab репозитория (публичный + токен для приватных)
- Сканирование конфигурационных файлов: `.csproj`, `packages.config`, `package.json`, `yarn.lock`, `requirements.txt`, `pyproject.toml`, `pom.xml`, `build.gradle`, `Gemfile`, `go.mod`, `Cargo.toml`, `composer.json`, `*.sln`, `Dockerfile`, `docker-compose.yml`
- LLM-парсинг для нестандартных/неизвестных форматов конфигурации
- LLM-обогащение каждой найденной технологии: статус поддержки (активна / замедлена / заброшена), дата последнего релиза, рекомендация (обновить / заменить / всё ок)
- Статус активности технологии в коде: используется (есть import/using/require) vs просто числится в зависимостях
- React UI: дашборд с результатами сканирования, фильтрация по статусу, детальная карточка технологии
- Сохранение результатов сканирования в MSSQL, история сканирований
- REST API на ASP.NET Core

**Out of Scope:**
- Авторизация/многопользовательность (MVP — single-user или без auth)
- Автоматическое применение обновлений (только рекомендации)
- IDE-плагины
- Сканирование бинарных файлов / декомпиляция
- CI/CD интеграции

## Context for Development

### Codebase Patterns

**Статус: Confirmed Clean Slate** — нет legacy-ограничений, нет существующих паттернов. Все решения greenfield.

**Архитектура решения (Clean Architecture):**
```
TechScanner.sln
├── src/
│   ├── TechScanner.Api/            ← ASP.NET Core Web API
│   │   ├── Controllers/            ← ScanController.cs
│   │   ├── DTOs/                   ← StartScanRequest, ScanResultDto
│   │   ├── Hubs/                   ← (SSE endpoint для прогресса)
│   │   └── Program.cs
│   ├── TechScanner.Core/           ← Domain (чистый, без зависимостей)
│   │   ├── Entities/               ← Scan.cs, ScanTechnology.cs
│   │   ├── Interfaces/             ← ISourceProvider, IManifestParser, ILlmEnricher, IScanRepository
│   │   └── Enums/                  ← SupportStatus, ScanStatus, SourceType
│   ├── TechScanner.Infrastructure/ ← Реализации интерфейсов
│   │   ├── Data/                   ← TechScannerDbContext.cs, EF Core migrations
│   │   ├── Repositories/           ← ScanRepository.cs
│   │   ├── Llm/                    ← OpenAiEnricher.cs, AzureOpenAiEnricher.cs
│   │   └── Git/                    ← GitRepoCloner.cs
│   └── TechScanner.Scanner/        ← Ядро бизнес-логики сканирования
│       ├── Orchestrator/           ← ScanOrchestrator.cs (координирует pipeline)
│       ├── Sources/                ← LocalFolderProvider, ZipArchiveProvider, GitRepoProvider
│       ├── Parsers/                ← CsprojParser, PackageJsonParser, RequirementsTxtParser, ...
│       ├── Analysis/               ← UsageAnalyzer.cs (поиск import/using в исходниках)
│       └── Background/             ← ScanBackgroundService.cs (IHostedService + Channel<T>)
├── frontend/
│   ├── src/
│   │   ├── components/             ← TechnologyCard, StatusBadge, ScanProgress
│   │   ├── pages/                  ← HomePage, ScanPage, HistoryPage
│   │   ├── services/               ← api.ts (axios/fetch обёртка)
│   │   └── types/                  ← scan.ts (TypeScript типы)
│   ├── package.json
│   └── vite.config.ts
└── tests/
    └── TechScanner.Tests/
        ├── Scanner/                ← Unit tests для парсеров, UsageAnalyzer
        └── Api/                    ← Integration tests для контроллеров
```

**Паттерны кодирования:**
- **Clean Architecture**: зависимости только внутрь (Core ← Scanner ← Infrastructure ← Api)
- **Strategy Pattern**: каждый формат манифеста — отдельный `IManifestParser` с методом `CanHandle(string fileName)`
- **Repository Pattern**: `IScanRepository` / `ScanRepository` поверх EF Core
- **Background Processing**: `ScanBackgroundService` принимает задания через `Channel<ScanJob>`, выполняет pipeline асинхронно
- **SSE для прогресса**: `GET /api/scans/{id}/progress` возвращает `text/event-stream`

**БД схема (MSSQL + EF Core):**
```sql
Scans: Id, SourceType (enum), SourceInput, Status, CreatedAt, CompletedAt, ErrorMessage
ScanTechnologies: Id, ScanId (FK), Name, Version, ManifestFile, IsActiveInCode, 
                  SupportStatus (Active/Slowing/Abandoned), LastReleaseDate, 
                  Recommendation, LlmRawResponse
```

### Files to Reference

| File | Purpose |
| ---- | ------- |
| src/TechScanner.Core/Entities/Scan.cs | Корневая сущность, агрегат сканирования |
| src/TechScanner.Core/Entities/ScanTechnology.cs | Найденная технология с обогащёнными данными |
| src/TechScanner.Core/Interfaces/IManifestParser.cs | Контракт Strategy-парсера |
| src/TechScanner.Scanner/Orchestrator/ScanOrchestrator.cs | Главный pipeline |
| src/TechScanner.Scanner/Analysis/UsageAnalyzer.cs | Проверка активности в коде |
| src/TechScanner.Infrastructure/Llm/OpenAiEnricher.cs | LLM-обогащение через OpenAI |
| src/TechScanner.Infrastructure/Data/TechScannerDbContext.cs | EF Core контекст |
| src/TechScanner.Api/Controllers/ScanController.cs | REST API |
| frontend/src/pages/ScanPage.tsx | Главная страница результатов |
| frontend/src/services/api.ts | HTTP клиент к API |

### Technical Decisions

1. **LLM провайдер**: конфигурируется через `appsettings.json` — `LlmProvider: "OpenAI"` или `"AzureOpenAI"`. Factory выбирает нужную реализацию `ILlmEnricher`.

2. **Определение активности технологии**: после извлечения имени пакета из манифеста — рекурсивный поиск паттернов `using {package}`, `import {package}`, `require('{package}')`, `from '{package}'` в `.cs`, `.ts`, `.tsx`, `.js`, `.jsx`, `.py`, `.java`, `.kt` файлах проекта. Результат: `IsActiveInCode = true/false`.

3. **GitHub/GitLab клонирование**: `git clone --depth 1 {url} {tempDir}`, после сканирования — полная очистка temp-директории (`Directory.Delete(recursive: true)`). Поддержка токена через `Authorization: Bearer` в URL или git credential helper.

4. **ZIP-архив**: распаковка в temp-папку через `System.IO.Compression.ZipFile`, тот же pipeline что и локальная папка, cleanup после.

5. **LLM промпт для обогащения**: батч-запрос на список технологий (не по одной), чтобы минимизировать кол-во вызовов API. Один запрос на сканирование.

6. **Прогресс сканирования**: SSE events: `scanning`, `parsing`, `analyzing`, `enriching`, `complete`, `error` — с процентом и текущим шагом.

7. **Хранение истории**: все результаты персистируются в MSSQL. UI показывает последние 50 сканирований.

## Implementation Plan

### Tasks

#### Группа 1: Solution Scaffolding & Core Domain

- [ ] **Задача 1: Создать .NET solution с проектами**
  - Файл: `TechScanner.sln`, `src/TechScanner.Api/`, `src/TechScanner.Core/`, `src/TechScanner.Infrastructure/`, `src/TechScanner.Scanner/`, `tests/TechScanner.Tests/`
  - Действие: `dotnet new sln`, создать 4 class library + 1 webapi + 1 xunit проект, добавить project references (Api → Infrastructure → Scanner → Core)
  - NuGet: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `OpenAI` (или `Azure.AI.OpenAI`), `System.IO.Compression`

- [ ] **Задача 2: Создать Core domain entities и enums**
  - Файл: `src/TechScanner.Core/Entities/Scan.cs`
  - Действие: Создать класс `Scan` со свойствами: `Id` (Guid), `SourceType` (enum), `SourceInput` (string), `Status` (enum: Queued/Running/Completed/Failed), `CreatedAt`, `CompletedAt?`, `ErrorMessage?`, `Technologies` (ICollection<ScanTechnology>)
  - Файл: `src/TechScanner.Core/Entities/ScanTechnology.cs`
  - Действие: Создать класс `ScanTechnology`: `Id` (Guid), `ScanId` (Guid), `Name` (string), `Version` (string?), `ManifestFile` (string), `IsActiveInCode` (bool), `SupportStatus` (enum: Active/Slowing/Abandoned/Unknown), `LastReleaseDate` (DateOnly?), `Recommendation` (string?), `Category` (string?)
  - Файл: `src/TechScanner.Core/Enums/ScanStatus.cs`, `SourceType.cs`, `SupportStatus.cs`
  - Действие: Создать enum-файлы: `ScanStatus { Queued, Running, Completed, Failed }`, `SourceType { LocalFolder, ZipArchive, GitRepository }`, `SupportStatus { Active, Slowing, Abandoned, Unknown }`

- [ ] **Задача 3: Создать Core interfaces**
  - Файл: `src/TechScanner.Core/Interfaces/IManifestParser.cs`
  - Действие: `bool CanHandle(string fileName); IEnumerable<RawTechnology> Parse(string filePath, string content);`
  - Файл: `src/TechScanner.Core/Interfaces/ISourceProvider.cs`
  - Действие: `Task<string> PrepareAsync(string input, CancellationToken ct); void Cleanup(string tempPath);` — возвращает путь к папке с файлами
  - Файл: `src/TechScanner.Core/Interfaces/ILlmEnricher.cs`
  - Действие: `Task<IEnumerable<TechnologyEnrichment>> EnrichAsync(IEnumerable<RawTechnology> technologies, CancellationToken ct);`
  - Файл: `src/TechScanner.Core/Interfaces/IScanRepository.cs`
  - Действие: `Task<Scan> CreateAsync(Scan scan); Task<Scan?> GetByIdAsync(Guid id); Task<IEnumerable<Scan>> GetRecentAsync(int count); Task UpdateAsync(Scan scan);`
  - Файл: `src/TechScanner.Core/Models/RawTechnology.cs`, `TechnologyEnrichment.cs`
  - Действие: Создать record-типы: `RawTechnology(string Name, string? Version, string ManifestFile)`, `TechnologyEnrichment(string Name, SupportStatus Status, DateOnly? LastRelease, string? Recommendation, string? Category)`

#### Группа 2: Infrastructure — БД и EF Core

- [ ] **Задача 4: Создать EF Core DbContext и конфигурацию**
  - Файл: `src/TechScanner.Infrastructure/Data/TechScannerDbContext.cs`
  - Действие: Создать `TechScannerDbContext : DbContext` с `DbSet<Scan>` и `DbSet<ScanTechnology>`. В `OnModelCreating` настроить: `Scan` → таблица `Scans`, индекс по `CreatedAt DESC`; `ScanTechnology` → таблица `ScanTechnologies`, FK к `Scans`, индекс по `ScanId`
  - Файл: `src/TechScanner.Infrastructure/Data/Migrations/` (генерируется EF Core)
  - Действие: После создания контекста выполнить `dotnet ef migrations add InitialCreate`

- [ ] **Задача 5: Создать ScanRepository**
  - Файл: `src/TechScanner.Infrastructure/Repositories/ScanRepository.cs`
  - Действие: Реализовать `IScanRepository`. `GetByIdAsync` — Include Technologies. `GetRecentAsync` — `OrderByDescending(s => s.CreatedAt).Take(count)`. `UpdateAsync` — `_context.Update(scan); await _context.SaveChangesAsync()`

#### Группа 3: Scanner — Source Providers

- [ ] **Задача 6: Создать LocalFolderProvider**
  - Файл: `src/TechScanner.Scanner/Sources/LocalFolderProvider.cs`
  - Действие: Реализовать `ISourceProvider`. `PrepareAsync` — валидировать, что путь существует (`Directory.Exists`), бросить `ArgumentException` если нет. Вернуть тот же путь (не копировать). `Cleanup` — ничего не делать (не наш файл).

- [ ] **Задача 7: Создать ZipArchiveProvider**
  - Файл: `src/TechScanner.Scanner/Sources/ZipArchiveProvider.cs`
  - Действие: `PrepareAsync` — получает путь к загруженному ZIP-файлу. Создать `tempDir = Path.Combine(Path.GetTempPath(), "techscanner_" + Guid.NewGuid())`. Вызвать `ZipFile.ExtractToDirectory(zipPath, tempDir)`. Вернуть `tempDir`.
  - `Cleanup` — `Directory.Delete(tempPath, recursive: true)` если директория существует.

- [ ] **Задача 8: Создать GitRepoProvider**
  - Файл: `src/TechScanner.Scanner/Sources/GitRepoProvider.cs`
  - Действие: `PrepareAsync` — распарсить URL (поддержать `https://github.com/...` и `https://gitlab.com/...`). Если передан `token` в query string или header — добавить в URL как `https://{token}@github.com/...`. Создать `tempDir`. Выполнить `Process.Start("git", $"clone --depth 1 {url} {tempDir}")`, дождаться завершения (таймаут 60 сек). Если exit code != 0 — бросить `InvalidOperationException`. Вернуть `tempDir`.
  - `Cleanup` — `Directory.Delete(tempPath, recursive: true)`.
  - **Безопасность**: токен передаётся через header `X-Git-Token`, никогда не логируется.

- [ ] **Задача 9: Создать SourceProviderFactory**
  - Файл: `src/TechScanner.Scanner/Sources/SourceProviderFactory.cs`
  - Действие: `ISourceProvider GetProvider(SourceType type)` — switch по типу, возвращает нужную реализацию.

#### Группа 4: Scanner — Manifest Parsers

- [ ] **Задача 10: Создать базовый ManifestParserBase и парсеры**
  - Файл: `src/TechScanner.Scanner/Parsers/CsprojParser.cs`
  - Действие: `CanHandle` → `fileName.EndsWith(".csproj") || fileName.EndsWith("packages.config")`. Parse: XDocument парсинг тегов `<PackageReference Include="..." Version="..."/>` и `<package id="..." version="..."/>`.
  - Файл: `src/TechScanner.Scanner/Parsers/PackageJsonParser.cs`
  - Действие: `CanHandle` → `fileName == "package.json"`. Parse: `System.Text.Json` парсинг полей `dependencies`, `devDependencies`, `peerDependencies`. Имя пакета + версия (очистить `^`, `~`, `>=`).
  - Файл: `src/TechScanner.Scanner/Parsers/RequirementsTxtParser.cs`
  - Действие: `CanHandle` → `fileName == "requirements.txt"`. Parse: построчно, regex `^([A-Za-z0-9_\-\.]+)([>=<!~^]+(.+))?$`, пропускать комментарии `#`.
  - Файл: `src/TechScanner.Scanner/Parsers/PyprojectParser.cs`
  - Действие: `CanHandle` → `fileName == "pyproject.toml"`. Parse: простой TOML-парсинг секций `[tool.poetry.dependencies]` и `[project.dependencies]`.
  - Файл: `src/TechScanner.Scanner/Parsers/MavenParser.cs`
  - Действие: `CanHandle` → `fileName == "pom.xml"`. Parse: XDocument, теги `<dependency><groupId>...</groupId><artifactId>...</artifactId><version>...</version>`.
  - Файл: `src/TechScanner.Scanner/Parsers/GradleParser.cs`
  - Действие: `CanHandle` → `fileName == "build.gradle" || fileName == "build.gradle.kts"`. Parse: regex для `implementation 'group:artifact:version'` и `implementation("group:artifact:version")`.
  - Файл: `src/TechScanner.Scanner/Parsers/DockerfileParser.cs`
  - Действие: `CanHandle` → `fileName == "Dockerfile" || fileName == "docker-compose.yml"`. Parse: Dockerfile — `FROM image:tag`, docker-compose — image: поля.
  - Файл: `src/TechScanner.Scanner/Parsers/GoModParser.cs`
  - Действие: `CanHandle` → `fileName == "go.mod"`. Parse: строки `require (...)` блока, regex `\s*([^\s]+)\s+v([^\s]+)`.
  - Файл: `src/TechScanner.Scanner/Parsers/CargoTomlParser.cs`
  - Действие: `CanHandle` → `fileName == "Cargo.toml"`. Parse: секция `[dependencies]`, поля `name = "version"` или `name = { version = "..." }`.
  - Файл: `src/TechScanner.Scanner/Parsers/LlmFallbackParser.cs`
  - Действие: `CanHandle` → всегда `true` (fallback). Parse: если файл не распознан другими парсерами — возвращает `RawTechnology` с `Name = "NEEDS_LLM_PARSE"` и `ManifestFile = filePath`, чтобы LLM Enricher обработал его отдельно.

- [ ] **Задача 11: Создать FileCollector**
  - Файл: `src/TechScanner.Scanner/FileCollector.cs`
  - Действие: `IEnumerable<string> Collect(string rootPath)` — рекурсивный обход папки `Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories)`. Игнорировать папки: `node_modules`, `.git`, `bin`, `obj`, `.gradle`, `__pycache__`, `.venv`, `vendor`, `dist`, `build`. Вернуть только файлы, для которых хотя бы один парсер вернёт `CanHandle(fileName) == true`.

#### Группа 5: Scanner — Usage Analyzer

- [ ] **Задача 12: Создать UsageAnalyzer**
  - Файл: `src/TechScanner.Scanner/Analysis/UsageAnalyzer.cs`
  - Действие: `bool IsActiveInCode(string packageName, string rootPath)`. Собрать список исходных файлов: `.cs`, `.ts`, `.tsx`, `.js`, `.jsx`, `.py`, `.java`, `.kt`, `.go`, `.rs`, `.rb`, `.php`. Нормализовать имя пакета (убрать scope `@org/`, взять только последний сегмент для maven `group:artifact`). Искать паттерны: `using {name}`, `import.*{name}`, `require.*{name}`, `from.*{name}` через `File.ReadAllText` + `Regex.IsMatch`. Вернуть `true` при первом совпадении (early exit). Ограничение: пропускать файлы > 500 КБ.

#### Группа 6: Infrastructure — LLM Enricher

- [ ] **Задача 13: Создать OpenAiEnricher**
  - Файл: `src/TechScanner.Infrastructure/Llm/OpenAiEnricher.cs`
  - Действие: Реализовать `ILlmEnricher`. Собрать батч всех технологий в один промпт:
    ```
    Analyze these technologies and for each return JSON array:
    [{name, version, supportStatus (Active/Slowing/Abandoned/Unknown), 
      lastReleaseDate (YYYY-MM-DD or null), recommendation, category}]
    Technologies: [{name, version}...]
    ```
    Использовать `OpenAI` NuGet пакет, модель `gpt-4o-mini`. Парсить JSON-ответ через `System.Text.Json`. При ошибке парсинга — вернуть `SupportStatus.Unknown` для всех.
  - Файл: `src/TechScanner.Infrastructure/Llm/LlmEnricherFactory.cs`
  - Действие: Factory, читающая `appsettings.json` → `LlmSettings:Provider` (`"OpenAI"` или `"AzureOpenAI"`), возвращает нужную реализацию `ILlmEnricher`.

#### Группа 7: Scanner — Orchestrator и Background Service

- [ ] **Задача 14: Создать ScanOrchestrator**
  - Файл: `src/TechScanner.Scanner/Orchestrator/ScanOrchestrator.cs`
  - Действие: Инжектировать `SourceProviderFactory`, `IEnumerable<IManifestParser>`, `UsageAnalyzer`, `ILlmEnricher`, `IScanRepository`. Метод `ExecuteAsync(Scan scan, IProgress<ScanProgress> progress, CancellationToken ct)`:
    1. Обновить `scan.Status = Running`, сохранить
    2. `progress.Report(new ScanProgress(10, "Preparing source..."))` 
    3. Получить провайдер, `tempPath = await provider.PrepareAsync(scan.SourceInput)`
    4. `progress.Report(20, "Collecting files...")` → `FileCollector.Collect(tempPath)`
    5. `progress.Report(30, "Parsing manifests...")` → запустить все `CanHandle` парсеры, собрать `List<RawTechnology>`
    6. `progress.Report(60, "Analyzing code usage...")` → для каждой технологии `UsageAnalyzer.IsActiveInCode`
    7. `progress.Report(75, "Enriching with LLM...")` → `LlmEnricher.EnrichAsync(rawTechnologies)`
    8. Смёрджить результаты в `ScanTechnology` объекты
    9. `progress.Report(95, "Saving results...")` → обновить `scan`, сохранить
    10. `scan.Status = Completed`, `scan.CompletedAt = DateTime.UtcNow`
    11. `provider.Cleanup(tempPath)` (в `finally`)
  - Файл: `src/TechScanner.Scanner/Orchestrator/ScanProgress.cs`
  - Действие: `record ScanProgress(int Percent, string Message)`

- [ ] **Задача 15: Создать ScanBackgroundService**
  - Файл: `src/TechScanner.Scanner/Background/ScanBackgroundService.cs`
  - Действие: `BackgroundService`. Инжектировать `Channel<ScanJob>` (singleton). В `ExecuteAsync` — `await foreach (var job in _channel.Reader.ReadAllAsync(ct))` → вызвать `ScanOrchestrator.ExecuteAsync`. Прогресс пробросить в `ScanProgressHub` или `ConcurrentDictionary<Guid, ScanProgress>` для SSE.
  - Файл: `src/TechScanner.Scanner/Background/ScanJob.cs`
  - Действие: `record ScanJob(Guid ScanId, SourceType SourceType, string SourceInput, string? GitToken)`

#### Группа 8: API — Controllers и SSE

- [ ] **Задача 16: Создать ScanController**
  - Файл: `src/TechScanner.Api/Controllers/ScanController.cs`
  - Действие: `[ApiController] [Route("api/scans")]`. Эндпоинты:
    - `POST /` — `StartScanAsync(StartScanRequest req)`: создать `Scan`, положить `ScanJob` в `Channel`, вернуть `202 Accepted` с `{ scanId }`.
    - `GET /{id}` — `GetScanAsync(Guid id)`: вернуть `ScanResultDto` (статус + технологии).
    - `GET /` — `GetHistoryAsync([FromQuery] int limit = 20)`: список последних сканирований.
    - `DELETE /{id}` — `DeleteScanAsync(Guid id)`: удалить запись.
    - `GET /{id}/progress` — `GetProgressAsync(Guid id)`: SSE endpoint, `Content-Type: text/event-stream`, поллит `ConcurrentDictionary<Guid, ScanProgress>` каждые 500мс, завершается когда статус `Completed/Failed`.
  - Файл: `src/TechScanner.Api/DTOs/StartScanRequest.cs`
  - Действие: `record StartScanRequest(SourceType SourceType, string SourceInput, string? GitToken)`
  - Файл: `src/TechScanner.Api/DTOs/ScanResultDto.cs`
  - Действие: Маппинг из `Scan` entity: `{ Id, Status, CreatedAt, CompletedAt, Technologies: [{Name, Version, ManifestFile, IsActiveInCode, SupportStatus, LastReleaseDate, Recommendation, Category}] }`

- [ ] **Задача 17: Создать FileUploadController**
  - Файл: `src/TechScanner.Api/Controllers/FileUploadController.cs`
  - Действие: `POST /api/upload` — принимает `IFormFile`, валидирует расширение `.zip` и размер (макс 100 МБ), сохраняет в `Path.GetTempPath()`, возвращает `{ tempFilePath }`. Этот путь передаётся в `StartScanRequest.SourceInput` при `SourceType = ZipArchive`.
  - **Безопасность**: валидировать Content-Type `application/zip` + magic bytes `PK\x03\x04`. Не допускать path traversal в имени файла.

- [ ] **Задача 18: Настроить Program.cs**
  - Файл: `src/TechScanner.Api/Program.cs`
  - Действие: Зарегистрировать: `DbContext` с MSSQL connection string, `IScanRepository → ScanRepository`, все `IManifestParser` реализации через `AddTransient`, `UsageAnalyzer`, `ScanOrchestrator`, `Channel<ScanJob>` как singleton, `ScanBackgroundService` как `AddHostedService`, `LlmEnricherFactory`, CORS для `http://localhost:5173` (Vite dev server). Добавить `appsettings.json` с `ConnectionStrings:Default` и `LlmSettings:Provider/ApiKey/Endpoint`.

#### Группа 9: Frontend

- [ ] **Задача 19: Создать React приложение (Vite + TypeScript)**
  - Файл: `frontend/`
  - Действие: `npm create vite@latest frontend -- --template react-ts`. Установить: `axios`, `@tanstack/react-query`, `react-router-dom`. Настроить `vite.config.ts` с proxy `/api → http://localhost:5000`.

- [ ] **Задача 20: Создать API service**
  - Файл: `frontend/src/services/api.ts`
  - Действие: Axios instance с `baseURL: '/api'`. Функции: `startScan(req)`, `getScan(id)`, `getHistory(limit?)`, `deleteScan(id)`, `uploadZip(file)`. Все с типами из `frontend/src/types/scan.ts`.
  - Файл: `frontend/src/services/scanProgress.ts`
  - Действие: Хук `useScanProgress(scanId)` — подключается к `/api/scans/{id}/progress` через `EventSource`, возвращает `{ percent, message, isDone }`.

- [ ] **Задача 21: Создать страницу Home (форма запуска сканирования)**
  - Файл: `frontend/src/pages/HomePage.tsx`
  - Действие: Три вкладки: "Local Path", "ZIP Archive", "Git Repository". Для каждой — соответсвующий input (текстовое поле пути / drag-and-drop загрузка файла / текстовое поле URL + поле токена). Кнопка "Scan". При отправке вызвать `startScan` или `uploadZip + startScan`. Redirect на `/scans/{id}`.

- [ ] **Задача 22: Создать страницу результатов ScanPage**
  - Файл: `frontend/src/pages/ScanPage.tsx`
  - Действие: Если статус `Running/Queued` — показать `ScanProgressBar` с SSE через `useScanProgress`. Если `Completed` — показать `TechnologyTable`. Если `Failed` — показать сообщение об ошибке.
  - Файл: `frontend/src/components/ScanProgressBar.tsx`
  - Действие: Прогресс-бар + текущий шаг (`"Enriching with LLM... 75%"`).
  - Файл: `frontend/src/components/TechnologyTable.tsx`
  - Действие: Таблица с колонками: Название, Версия, Файл манифеста, Активна в коде (иконка ✓/✗), Статус поддержки (цветной badge: зелёный/жёлтый/красный), Последний релиз, Рекомендация. Фильтры: `All / Active in code / Needs update / Abandoned`. Сортировка по статусу.
  - Файл: `frontend/src/components/StatusBadge.tsx`
  - Действие: `Active → зелёный`, `Slowing → жёлтый`, `Abandoned → красный`, `Unknown → серый`.

- [ ] **Задача 23: Создать страницу истории HistoryPage**
  - Файл: `frontend/src/pages/HistoryPage.tsx`
  - Действие: Список карточек последних 20 сканирований: источник, дата, кол-во найденных технологий, кол-во с рекомендациями. Клик → переход на `/scans/{id}`. Кнопка удаления с подтверждением.

- [ ] **Задача 24: Создать навигацию и layout**
  - Файл: `frontend/src/App.tsx`
  - Действие: `react-router-dom` routes: `/` → `HomePage`, `/scans/:id` → `ScanPage`, `/history` → `HistoryPage`. Навигационная панель с логотипом "TechScanner" и ссылками.

#### Группа 10: Тесты

- [ ] **Задача 25: Unit тесты для парсеров**
  - Файл: `tests/TechScanner.Tests/Scanner/Parsers/PackageJsonParserTests.cs`
  - Действие: Тесты для каждого парсера: `CanHandle_ValidFileName_ReturnsTrue`, `Parse_ValidContent_ExtractsCorrectPackages`, `Parse_EmptyDependencies_ReturnsEmpty`, `Parse_MalformedContent_DoesNotThrow`. Использовать inline fixtures с типичными содержимыми файлов.
  - Файл: `tests/TechScanner.Tests/Scanner/Analysis/UsageAnalyzerTests.cs`
  - Действие: Тесты с временными файлами: `IsActiveInCode_PackageImported_ReturnsTrue`, `IsActiveInCode_PackageNotImported_ReturnsFalse`.

- [ ] **Задача 26: Unit тесты для ScanOrchestrator**
  - Файл: `tests/TechScanner.Tests/Scanner/Orchestrator/ScanOrchestratorTests.cs`
  - Действие: Moq всех зависимостей. `ExecuteAsync_SuccessfulScan_SavesCompletedStatus`, `ExecuteAsync_SourceProviderThrows_SavesFailedStatus`, `ExecuteAsync_SetsIsActiveInCode_BasedOnAnalyzer`.

---

### Acceptance Criteria

- [ ] **AC 1:** Given пользователь ввёл корректный локальный путь к папке проекта, when нажимает "Scan", then система создаёт запись сканирования, возвращает `scanId` и начинает асинхронное сканирование в фоне.

- [ ] **AC 2:** Given сканирование запущено, when пользователь открывает страницу `/scans/{id}`, then отображается прогресс-бар с текущим шагом (процент + текст), обновляемый в реальном времени через SSE.

- [ ] **AC 3:** Given проект содержит `package.json` с 10 зависимостями, when сканирование завершено, then все 10 зависимостей отображаются в таблице с именем, версией и файлом источника.

- [ ] **AC 4:** Given зависимость указана в `package.json`, but нигде не импортирована в `.ts`/`.js` файлах, when сканирование завершено, then колонка "Активна в коде" для этой зависимости показывает ✗.

- [ ] **AC 5:** Given зависимость используется в коде через `import ... from 'package-name'`, when сканирование завершено, then колонка "Активна в коде" показывает ✓.

- [ ] **AC 6:** Given LLM обогащение завершено, when пользователь смотрит результаты, then каждая технология имеет статус поддержки (`Active/Slowing/Abandoned/Unknown`) и рекомендацию (или пусто если всё ок).

- [ ] **AC 7:** Given пользователь загрузил ZIP-архив проекта, when сканирование завершено, then временная директория с распакованным архивом удалена с диска.

- [ ] **AC 8:** Given пользователь ввёл URL публичного GitHub репозитория, when нажимает "Scan", then система клонирует репозиторий `--depth 1`, сканирует, и удаляет клонированную папку после завершения.

- [ ] **AC 9:** Given пользователь ввёл некорректный/недоступный путь к папке, when нажимает "Scan", then сканирование переходит в статус `Failed` с понятным сообщением об ошибке, отображаемым в UI.

- [ ] **AC 10:** Given сканирование завершено, when пользователь открывает страницу истории (`/history`), then видит запись этого сканирования с датой, источником и кол-вом найденных технологий.

- [ ] **AC 11:** Given пользователь применяет фильтр "Abandoned" в таблице результатов, when есть заброшенные технологии, then отображаются только они; если нет — показывается пустое состояние с текстом "No abandoned technologies found".

- [ ] **AC 12:** Given пользователь пытается загрузить файл с расширением `.exe` через upload endpoint, when валидация срабатывает, then API возвращает `400 Bad Request` и файл не сохраняется.

- [ ] **AC 13:** Given проект содержит `.csproj`, `package.json` и `requirements.txt` одновременно, when сканирование завершено, then технологии из всех трёх форматов присутствуют в результатах.

- [ ] **AC 14:** Given введён URL приватного GitHub репозитория с токеном в поле `X-Git-Token`, when сканирование запускается, then токен используется для аутентификации и не отображается/не логируется нигде в системе.

---

## Additional Context

### Dependencies

**NuGet пакеты (Backend):**
- `Microsoft.EntityFrameworkCore.SqlServer` (8.x)
- `Microsoft.EntityFrameworkCore.Tools` (8.x)
- `OpenAI` (2.x) — официальный .NET клиент
- `System.IO.Compression` (встроен в .NET)
- `System.Text.Json` (встроен в .NET)
- `xunit` + `Moq` + `Microsoft.AspNetCore.Mvc.Testing` (тесты)

**npm пакеты (Frontend):**
- `axios` — HTTP клиент
- `@tanstack/react-query` — кеширование и запросы
- `react-router-dom` — маршрутизация
- `vitest` + `@testing-library/react` (тесты)

**Системные зависимости:**
- `git` CLI должен быть установлен на сервере (для клонирования репозиториев)
- SQL Server Express (локально или Docker: `mcr.microsoft.com/mssql/server:2022-latest`)
- OpenAI API ключ или Azure OpenAI endpoint + ключ

### Testing Strategy

**Unit тесты (xUnit + Moq):**
- Каждый `IManifestParser`: тест `CanHandle` + `Parse` для типичного содержимого
- `UsageAnalyzer`: тесты с временными файлами
- `ScanOrchestrator`: все ветки (успех, ошибка провайдера, ошибка LLM)
- `ScanRepository`: in-memory SQLite через `UseInMemoryDatabase`

**Компонентные тесты (Vitest + RTL):**
- `TechnologyTable`: рендер данных, фильтрация
- `StatusBadge`: правильный цвет для каждого статуса
- `ScanProgressBar`: отображение прогресса

**Ручное тестирование:**
1. Запустить сканирование на самом проекте TechScanner (локальная папка)
2. Проверить сканирование ZIP-архива
3. Проверить сканирование публичного GitHub репозитория (например `github.com/microsoft/calculator`)
4. Проверить фильтрацию в UI
5. Проверить историю сканирований

### Notes

**Высокорисковые моменты:**
- LLM может вернуть невалидный JSON — нужен robust fallback (все `Unknown`)
- `git clone` может зависнуть — обязателен таймаут (60 сек) и CancellationToken
- Большие репозитории (>500 МБ) могут исчерпать место на диске — рекомендуется лимит через `--depth 1` + проверка доступного места
- MSSQL Express имеет лимит 10 ГБ на БД — для MVP более чем достаточно

**Будущие возможности (вне скоупа):**
- Diff между двумя сканированиями одного проекта
- Экспорт отчёта в PDF/CSV
- WebHooks при появлении заброшенных технологий
- Авторизация и multi-tenant
