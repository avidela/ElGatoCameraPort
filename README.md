# ElgatoCameraPort

A cross-platform web UI for controlling the **Elgato Facecam MK.2** (and compatible UVC cameras) on **Linux** and **Windows**. Built with a .NET 9 API backend and a React + TypeScript frontend, served as a single self-contained app on `http://localhost:5000`.

---

## Architecture

```
ElgatoCameraPort/
├── ElgatoControl.Api/         # .NET 9 ASP.NET Core API (port 5000)
│   ├── Endpoints/             # Minimal API endpoint groups
│   │   ├── CameraEndpoints    # GET layout, GET controls, POST set/save/reset
│   │   ├── PresetEndpoints    # GET/POST preset load & save (A–D)
│   │   └── StreamEndpoints    # MJPEG stream via FFmpeg
│   ├── Services/
│   │   ├── ICameraDevice      # OS-agnostic camera interface
│   │   ├── LinuxCameraDevice  # v4l2-ctl implementation (Linux)
│   │   └── WindowsCameraDevice# DirectShow / WMF implementation (Windows)
│   └── Utilities/FFmpegUtility# FFmpeg process management
│
└── ElgatoControl.Web/         # React + TypeScript frontend (Vite)
    └── src/
        ├── hooks/             # All business logic
        │   ├── useCameraLayout   # Sections schema + values state
        │   ├── useCameraPreview  # Stream URL + format selection
        │   ├── useCameraActions  # All API calls, status, presets
        │   └── useScreenshot     # Canvas capture
        ├── components/        # Pure UI components
        └── styles/            # Per-component SCSS (BEM)
```

The backend auto-selects `LinuxCameraDevice` or `WindowsCameraDevice` at startup based on `OperatingSystem.IsWindows()`. On boot it applies **Preset A** (if saved) to the hardware immediately.

---

## System Requirements

### Linux

```bash
# v4l2-ctl — reads and writes camera controls (zoom, pan, tilt, etc.)
sudo apt install v4l-utils

# ffmpeg — MJPEG stream for the live preview
sudo apt install ffmpeg
```

> **Why v4l-utils?** The Linux camera service calls `v4l2-ctl --set-ctrl` and `v4l2-ctl --get-ctrl` under the hood. Without it, no camera controls will work.

> **Why ffmpeg?** The `/api/stream` endpoint pipes FFmpeg output as a browser-readable MJPEG stream. Without it the live preview will not function (camera controls still work).

### Windows

No additional system tools required. Camera access uses the built-in Windows Media Foundation / DirectShow APIs via .NET.

---

## Running Locally (Development)

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Node.js 20+](https://nodejs.org/)

### 1. Start the API

```bash
cd ElgatoControl.Api
dotnet run
# Listening on http://localhost:5000
```

### 2. Start the frontend dev server

```bash
cd ElgatoControl.Web
npm install
npm run dev
# Listening on http://localhost:5173
```

The frontend dev server proxies API calls to `http://localhost:5000`.

### 3. Run as a desktop app (Electron)

```bash
# Install the CLI once
dotnet tool install ElectronNET.CLI -g

# Start in Electron window (from ElgatoControl.Api/)
cd ElgatoControl.Api
DOTNET_ROLL_FORWARD=LatestMajor electronize start
```

> **Note:** `DOTNET_ROLL_FORWARD=LatestMajor` is needed because the `electronize` CLI targets .NET 6 but you have .NET 8+. This is a dev-time-only workaround — the built `.deb` is fully self-contained.

---

## Running with Docker (Linux only)

The Dockerfile builds both the frontend and backend, installs `ffmpeg` and `v4l-utils` in the final image, and serves everything on port 5000.

```bash
# Build
docker build -t elgato-control .

# Run — pass through the camera device
docker run -d \
  --device /dev/video0:/dev/video0 \
  -p 5000:5000 \
  elgato-control
```

Open `http://localhost:5000` in your browser.

> **Note:** The `--device` flag is required on Linux so the container can access the camera. Use `v4l2-ctl --list-devices` on the host to find the correct `/dev/videoN` path.

---

## Building for Production

```bash
# Frontend
cd ElgatoControl.Web
npm run build          # outputs to dist/

# Backend (serves the frontend from wwwroot/)
cd ElgatoControl.Api
dotnet publish -c Release -o out
cp -r ../ElgatoControl.Web/dist out/wwwroot
dotnet out/ElgatoControl.Api.dll
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core Minimal APIs |
| Frontend | React 19, TypeScript, Vite |
| Styling | SCSS (BEM), per-component partials |
| Linux camera | `v4l2-ctl` (v4l-utils) |
| Stream | FFmpeg → MJPEG |
| Container | Docker (Alpine base) |

---

## Preset System

Presets **A, B, C, D** store `zoom`, `pan`, and `tilt` values in `presets.json` on the backend. Preset A is applied automatically on API startup.

- **Load preset** — `GET /api/camera/preset/load/{id}`
- **Save preset** — `POST /api/camera/preset/save/{id}`
