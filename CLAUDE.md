# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Portal Calendar is a two-part system:
- **Client**: ESP32 PlatformIO C++ firmware for an e-ink display device
- **Server**: ASP.NET Core C# backend that generates and serves rendered bitmap images

The ESP32 wakes from deep sleep, connects to WiFi, fetches a pre-rendered bitmap from the server, displays it on e-paper, then sleeps again. The server renders HTML themes via Playwright (headless browser) and converts them to device-specific bitmap formats.

## Build Commands

### Client (ESP32 Firmware)
```bash
# Build a specific environment
pio run -e <environment>

# Upload to device
pio run -e <environment> -t upload --upload-port <COM port>

# Run test environments
pio run -e test1 -e test2 -e test3 -e test4

# Build example environments
pio run -e example1 -e example2 -e example3
```

Key environments in `platformio.ini`: `example1`–`example3` (demo builds), `test1`–`test4` (unit test builds), plus personal board configs.

### Server (ASP.NET Core)
```bash
cd server

# Restore, build, test
dotnet restore
dotnet build
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ApiControllerTests"

# Run locally (uses localdata/ for SQLite, http://localhost:5252)
dotnet run --project PortalCalendarServer
```

The server uses `appsettings.Development.json` in dev mode, which points databases and generated files to `server/localdata/`.

## Architecture

### Client Data Flow (`client/src/`)
1. `main.cpp` — Entry point: init hardware → connect WiFi → fetch image → display → deep sleep
2. `http_client_manager.cpp` — HTTP GET to server with device params (MAC, firmware version, display dimensions, battery voltage)
3. `display_manager.cpp` — E-paper control via GxEPD2 library
4. `ota_manager.cpp` — OTA firmware updates (checked before displaying calendar)

The client validates image checksums stored in RTC memory to skip re-rendering unchanged content.

### Server Data Flow (`server/PortalCalendarServer/`)
1. `Controllers/ApiController.cs` — Receives ESP32 requests at `GET /api/device/config` and bitmap endpoints
2. `Services/PageGeneratorService.cs` — Builds HTML from Razor themes + module config
3. `Services/Web2PngService.cs` (IWeb2PngService) — Playwright renders HTML → PNG
4. Bitmap conversion — PNG converted to device-specific format (B&W, 3-color, 4-color) with optional dithering
5. `Data/CalendarContext.cs` — EF Core 9.0 + SQLite; entities: `Displays`, `Themes`, `Configs`, `Cache`, `Galleries`, `Users`

### Module System
Pluggable modules registered in `Program.cs`. Each module contributes config UI and rendering logic. Built-in modules include: `CalendarModule`, `MetNoWeatherModule`, `OpenWeatherModule`, `GoogleFitModule`, `GalleryModule`, `XkcdModule`, `TelegramModule`, `MqttModule`, `PortalIconsModule`, `WebImageModule`.

### Authentication
Two schemes: Cookie auth (web UI) + InternalToken auth (ESP32 API). Sessions stored in a separate `sessions.db` SQLite database.

### Background Services
Registered in `Program.cs`: bitmap pre-generation, cache cleanup, missed connection tracking.

### Configuration Placeholder System
`appsettings.json` tokens like `{LocalAppDataPath}` and `{ContentRootPath}` are substituted at startup — do not treat these as literal strings.

## Key Directories

| Path | Purpose |
|------|---------|
| `client/src/` | ESP32 firmware source |
| `client/include/` | Headers and board-specific config |
| `server/PortalCalendarServer/` | Main ASP.NET Core app |
| `server/PortalCalendarServer/Controllers/` | API + UI controllers |
| `server/PortalCalendarServer/Services/` | Business logic and integrations |
| `server/PortalCalendarServer/Views/CalendarThemes/` | Razor templates for e-ink themes |
| `server/PortalCalendarServer/wwwroot/` | Static assets (fonts, icons, CSS) |
| `server/PortalCalendarServer/Data/` | EF Core DbContext + migrations |
| `server/PortalCalendarServer.Tests/` | xUnit tests with Moq |
| `server/localdata/` | Dev-only SQLite databases and generated images |
| `GxEPD2/` | E-paper display library (git submodule) |

## Localization

The server UI uses **OrchardCore Localization** with GNU gettext `.po` files. Localization is UI-only — the REST API is not localized.

- **Package**: `OrchardCore.Localization.Core`
- **Translation files**: `server/PortalCalendarServer/Localization/{culture}.po` (currently `en.po` and `cs.po`)
- **Culture selection**: Cookie-based only (`CookieRequestCultureProvider`), no Accept-Language fallback
- **Thread culture**: Explicitly set to `InvariantCulture` to avoid disturbing number formatting in forms and logs; UI culture is separate

### Adding/updating translations
1. Use `@Localizer["Key"]` in Razor views (supports parameters: `@Localizer["Text {0}", value]`)
2. Add the corresponding `msgid`/`msgstr` entries to **all** `.po` files
3. Run `bash server/check-missing-localizations.sh` to verify no keys are missing — this also runs in CI

### Adding a new language
1. Create a new `{culture}.po` file in `server/PortalCalendarServer/Localization/`
2. Add the culture code to the `supportedCultures` array in `Program.cs`
3. The language switcher in the sidebar auto-discovers supported cultures

## CI/CD

- `.github/workflows/client-build.yml` — PlatformIO builds on self-hosted Linux runner
- `.github/workflows/server-build.yml` — .NET 9 build + xUnit tests + localization check
- `.github/workflows/server-release.yml` — Release automation

### Command Execution (ALL AGENTS)
- Stay in project root, don't `cd` or `Set-Location` to subdirectories unless necessary for a specific command.

### Database migrations

- Any database migrations should be created and applied via EF Core CLI (`dotnet ef migrations`)
- `CalendarContext` is the common DB context here.
- Do not run `dotnet ef database update`. Only run `dotnet ef migrations add` as I want to preview all changes (also the server applies pending migrations at startup automatically)

### .NET code standards
- With Linq check whether `.AsSingleQuery()` is needed to avoid cartesian explosion when including multiple related entities. Use `.AsSplitQuery()` if you want to force separate queries instead.

### C++ code standards
- When updating client firmware always increment the version constant in `client/include/version.h`
