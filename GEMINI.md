# FamilyFeudLive

An interactive, real-time Family Feud game show web application built with ASP.NET Core Blazor Server (.NET 9). It supports a big-screen display board for audiences, a mobile-friendly buzzer for contestants, and a host control panel for managing gameplay.

---

## Architecture & Project Structure

- **Framework**: .NET 9.0 (ASP.NET Core Blazor Interactive Server)
- **State Management**: Singleton in-memory event-driven state service (`GameStateService`)

### Key Directories & Files

- **`Components/Pages/`**:
  - `Board.razor` (`/board`): Audience display board displaying questions, concealed/revealed answers, team scores, strikes, and sound effects.
  - `Host.razor` (`/host`): Admin and game controller view to advance rounds, reveal answers, award points, trigger strikes, and import questions.
  - `Buzzer.razor` (`/buzzer`): Mobile buzzer client for players to buzz in first.
  - `Home.razor` (`/`): Landing page featuring QR codes and direct links to the board, host, and buzzer screens.
- **`Services/`**:
  - `GameStateService.cs`: Central singleton holding current game state (teams, scores, current round/question, strikes, buzzer state, sound event IDs). Coordinates real-time updates via `OnChange` action events.
  - `QrCodeService.cs`: Generates QR codes (PNG data URLs / SVGs) using `QRCoder` to simplify mobile connection.
- **`Models/`**:
  - `GameModels.cs`: Core data models including `Question`, `AnswerItem`, `Team`, `BuzzerState`, and JSON import structures.
- **`SampleRounds.json`**:
  - Default and sample round question sets ready to be imported through the Host dashboard.

---

## Development Workflow & Commands

### Prerequisites
- .NET 9.0 SDK

### Build & Run
```bash
# Build the application
dotnet build

# Run the application locally
dotnet run

# Run with hot reload during development
dotnet watch
```

> **Note on Sandbox/CLI**: When running `dotnet` commands in restricted environments, telemetry can be disabled using `$env:DOTNET_CLI_TELEMETRY_OPTOUT="1"`.

---

## Code Conventions & Guidelines

1. **Blazor Rendering Mode**:
   - All interactive pages utilize `@rendermode InteractiveServer`.
   - Subscribe to `GameStateService.OnChange` in `OnInitialized()` and trigger `InvokeAsync(StateHasChanged)` to re-render when global state changes.
   - Always implement `IDisposable` on components to unhook event handlers (`GameStateService.OnChange -= ...`) and prevent memory leaks.

2. **Concurrency & State Synchronization**:
   - `GameStateService` is registered as a singleton. All mutations to shared state (such as buzzer buzz-in timestamps or score modifications) must remain thread-safe (e.g., using `lock (_lock)` blocks).

3. **C# Standards**:
   - Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
   - Implicit usings are enabled (`<ImplicitUsings>enable</ImplicitUsings>`).
   - Keep razor component styling clean and scoped or utilize Bootstrap utility classes consistent with the existing layout.
