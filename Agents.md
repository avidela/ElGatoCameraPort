---
description: ElgatoCameraPort Architecture Guidelines
---
# Architecture Tenets
These are the strictly enforced architectural rules for all AI agents connecting to this repository. Do NOT deviate from these guidelines under any circumstances:

### 1. Server Driven UI (SDUI)
- The frontend applications (whether React in `ElgatoControl.Web` or Avalonia native desktop apps) must contain **absolutely zero** hardcoded business logic, initial state guesses, schemas, layout matrices, or device assumptions.
- The backend's `GetLayout()` endpoint returns an exact JSON definition of the entire component tree, the layout schema, and the values.
- The UI's *only* job is parsing that JSON payload and automatically wiring it to actual framework components. 
- This ensures any logic shifts push instantly to all potential clients without needing client-side deploys.
- Do NOT use local storage mechanisms for any application state schemas or user configurations. Use backend persistence files (e.g. `presets.json`) managed purely in ASP.NET Core memory.

### 2. Strict Modularity (No Monoliths)
- UI components must follow a strict, fragmented mental model (e.g., Angular-style for React, or MVVM/UserControls for Avalonia).
- Components should be extremely scoped and singular in purpose.
- Do NOT build massive monoliths that house all page state. Break code out intelligently (e.g., `/src/components/`, `/src/hooks/` for React, or `/Views/`, `/ViewModels/` for Avalonia).

### 3. Backend Truth
- If the hardware state or application boot state needs to change (e.g., forcing "Preset A" on connection), that logic belongs in the `Program.cs` or Service layer of the ASP.NET Core app. Do not hack frontend lifecycles to patch hardware workflows.
- **Multi-Client Support**: The backend architecture is designed to support multiple frontends (Electron, Avalonia, Web). All clients MUST consume the same API. 
- **User Choice**: We provide both Electron (design-focused) and Avalonia (performance-focused) packages. Agents must ensure that any new feature is implemented in the backend first to be instantly available across all clients.

### 4. Naming Conventions (SOLID)
- Respect C# MVC/MVVM naming scopes. Hardware abstractions are `Device` wrappers (e.g., `LinuxCameraDevice.cs`), not `Controller` endpoints unless they literally extend `ControllerBase`.
