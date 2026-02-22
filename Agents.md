---
description: ElgatoCameraPort Architecture Guidelines
---
# Architecture Tenets
These are the strictly enforced architectural rules for all AI agents connecting to this repository. Do NOT deviate from these guidelines under any circumstances:

### 1. Server Driven UI (SDUI)
- The React application (`ElgatoControl.Web`) must contain **absolutely zero** hardcoded business logic, initial state guesses, schemas, layout matrices, or device assumptions.
- The backend's `GetLayout()` endpoint returns an exact JSON definition of the entire component tree, the layout schema, and the values.
- The UI's *only* job is parsing that JSON payload and automatically wiring it to actual React components. 
- This ensures any logic shifts push instantly to all potential clients without needing client-side deploys.
- Do NOT use `localStorage` for any application state schemas or user configurations. Use backend persistence files (e.g. `presets.json`) managed purely in ASP.NET Core memory.

### 2. Angular-Style Modularity (No Monoliths)
- React components must follow a strict, fragmented Angular mental model.
- Components should be extremely scoped and singular in purpose.
- Do NOT build massive 300+ line monoliths like `App.tsx` that house all page state. Break code out into `/src/components/`, `/src/hooks/`, and `/src/services/` intelligently.

### 3. Backend Truth
- If the hardware state or application boot state needs to change (e.g., forcing "Preset A" on connection), that logic belongs in the `Program.cs` or Service layer of the ASP.NET Core app. Do not hack frontend lifecycles (`useEffect`) to patch hardware workflows.

### 4. Naming Conventions (SOLID)
- Respect C# MVC naming scopes. Hardware abstractions are `Device` wrappers (e.g., `LinuxCameraDevice.cs`), not `Controller` endpoints unless they literally extend `ControllerBase`.
