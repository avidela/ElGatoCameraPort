# ElgatoCameraPort

A Linux desktop app for controlling the **Elgato Facecam MK.2** (and compatible UVC cameras). Packaged as a native Electron window — no browser required.

Built with a .NET 9 API backend, a React + TypeScript frontend, and Electron.NET for the desktop shell.

---

## Install (Linux — Debian/Ubuntu/Mint)

Download the latest `.deb` from the [Releases](../../releases) page and install it:

```bash
sudo apt install ./elgato-camera-control_1.0.0_amd64.deb
```

This will automatically install `ffmpeg` and `v4l-utils` as dependencies. Launch **Elgato Camera Control** from your application menu.

---

## Architecture

```
ElgatoCameraPort/
├── ElgatoControl.Api/         # .NET 9 ASP.NET Core API + Electron shell
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

---

## System Requirements

### Linux

- `ffmpeg` and `v4l-utils` — declared as `.deb` dependencies, installed automatically via `apt`
- Camera must appear as `/dev/videoN` (standard UVC device)

> **Why v4l-utils?** The Linux camera service calls `v4l2-ctl --set-ctrl` under the hood. Without it, no camera controls work.

> **Why ffmpeg?** The `/api/stream` endpoint pipes FFmpeg output as an MJPEG stream for the live preview.

---

## Development

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Node.js 20+](https://nodejs.org/)
- `dotnet tool install ElectronNET.CLI -g` (for running the Electron window locally)

### 1. Start the API (browser mode)

```bash
cd ElgatoControl.Api
dotnet run
# http://localhost:5000
```

### 2. Start the frontend dev server

```bash
cd ElgatoControl.Web
npm install
npm run dev
# http://localhost:5173
```

### 3. Run as a desktop Electron window

```bash
# Build the React frontend first
cd ElgatoControl.Web && npm run build && cd ..
cp -r ElgatoControl.Web/dist ElgatoControl.Api/wwwroot

# Start Electron
cd ElgatoControl.Api
DOTNET_ROLL_FORWARD=LatestMajor electronize start
```

> **Note:** `DOTNET_ROLL_FORWARD=LatestMajor` is needed because the `electronize` CLI targets .NET 6 but ships with .NET 8+. The built `.deb` is fully self-contained and doesn't require .NET on the user's machine.

---

## Building the .deb

```bash
./scripts/build-deb.sh
# Output: ElgatoControl.Api/bin/Desktop/elgato-camera-control_1.0.0_amd64.deb
```

### Releasing via GitHub Actions

Tag a commit to trigger a release build that automatically attaches the `.deb` to a GitHub Release:

```bash
git tag v1.0.0
git push --tags
```

---

## Running with Docker (headless / server mode)

```bash
docker build -t elgato-control .
docker run -d \
  --device /dev/video0:/dev/video0 \
  -p 5000:5000 \
  elgato-control
```

Open `http://localhost:5000` in your browser.

> Use `v4l2-ctl --list-devices` to find the correct `/dev/videoN` path.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core Minimal APIs |
| Frontend | React 19, TypeScript, Vite |
| Styling | SCSS (BEM), per-component partials |
| Desktop shell | Electron.NET |
| Linux camera | `v4l2-ctl` (v4l-utils) |
| Stream | FFmpeg → MJPEG |
| Container | Docker (Alpine base) |

---

## Preset System

Presets **A, B, C, D** store `zoom`, `pan`, and `tilt` values in `presets.json` on the backend. Preset A is applied automatically on API startup.

- **Load preset** — `GET /api/camera/preset/load/{id}`
- **Save preset** — `POST /api/camera/preset/save/{id}`
