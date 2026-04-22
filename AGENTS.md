# Repository Guidelines

## Project Structure & Module Organization
The solution root contains `DeviceController.sln`, the WPF kiosk app under `Kiosk`, and docs. Inside `Kiosk`, feature logic is split across `Domain` (core models and services), `Infrastructure` (EF Core data access, hardware drivers, MySQL connector configs), and `Presentation` (WPF views/view-models). Assets such as flags, GIFs, and fonts live under `Assets`, while database scripts migrate through `Migrations`. Native vendor DLLs stay in `libs`; keep each architecture folder intact when updating.

## Build, Test, and Development Commands
```bash
dotnet restore DeviceController.sln                    # Restore NuGet packages
dotnet build DeviceController.sln -c Debug             # Compile WPF app for desktop testing
dotnet run --project Kiosk --framework net9.0-windows  # Launch kiosk shell locally
dotnet ef database update --project Kiosk              # Apply EF Core migrations to the configured MySQL target
```
Use the `Remote|x86` configuration only when you intentionally push binaries to the shared UNC output folder defined in the project file.

## Coding Style & Naming Conventions
Follow standard C# conventions: 4-space indentation, PascalCase for public classes/methods, camelCase for locals/fields, and `IName` prefixes for interfaces. Keep XAML files grouped by feature (see `Presentation/Features/*/Pages`). Bindings should stay strongly typed via CommunityToolkit.MVVM attributes. When editing EF entities, align property names with column names already used in migrations to prevent drift.

## Testing Guidelines
There is no dedicated test project yet; when adding automated coverage, create `*.Tests` projects beside `Kiosk` and ensure files end with `Tests.cs`. For now, rely on `Presentation/Features/**/ResxLocalizationTest*` pages for localization smoke tests and exercise interactive flows by running the app in Debug.

## Commit & Pull Request Guidelines
Recent commits mix languages and placeholders, so standardize on imperative English messages such as `feat: add remittance popup navigation`. Reference the feature area at the start (`navigation: fix back stack`) when helpful. Pull requests must include: a concise summary, screenshots or screen recordings for UI updates, reproduction steps for bug fixes, linked issue IDs, and notes on migrations or hardware dependencies. Keep changes scoped, rebase onto the latest main branch, and ensure `dotnet build` passes before requesting review.

## Security & Configuration Tips
Never commit connection strings, API keys, or kiosk-specific PINs; use `dotnet user-secrets` or environment variables consumed by `appsettings.json`. DLLs in `libs` are proprietary—distribute updates through the secure share rather than public package feeds. Validate that any new serial-port or payment integrations handle disconnections gracefully and log via Serilog with the existing JSON formatter.
