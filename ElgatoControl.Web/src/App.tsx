import { useState, useEffect } from 'react';
import './App.css';
import ControlSection from './components/ControlSection';
import CameraPreview from './components/CameraPreview';
import DeviceCard from './components/DeviceCard';
import ActionFooter from './components/ActionFooter';
import { useCameraLayout } from './hooks/useCameraLayout';
import { useCameraPreview } from './hooks/useCameraPreview';
import type { VideoFormat } from './types';

function App() {
  const [isPreviewOn, setIsPreviewOn] = useState(false); // Default OFF per user request
  const [status, setStatus] = useState('Ready');
  const [activePreset, setActivePreset] = useState<string | null>(null);

  const [values, setValues] = useState<Record<string, number>>({});

  // Abstracted Services (Angular style logic extraction)
  const { sections, collapsed, toggleCollapse } = useCameraLayout(setValues);
  const { formats, selectedFormat, setSelectedFormat, streamUrl, showGrid, setShowGrid } = useCameraPreview(isPreviewOn);

  const updateCamera = async (prop: string, val: number) => {
    try {
      const response = await fetch(`http://localhost:5000/api/camera/set`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prop, val })
      });
      const data = await response.json();
      if (!data.success) {
        setStatus(`Error: ${data.message}`);
      }
    } catch (err) {
      setStatus('Failed to connect to API');
    }
  };

  const saveToCamera = async () => {
    try {
      setStatus('Saving to hardware...');

      if (activePreset) {
        await fetch(`http://localhost:5000/api/camera/preset/save/${activePreset}`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ zoom: values.zoom, pan: values.pan, tilt: values.tilt })
        });
      }

      const response = await fetch(`http://localhost:5000/api/camera/save`, { method: 'POST' });
      const data = await response.json();

      if (data.success) {
        setStatus(activePreset ? `Saved to Camera & Preset ${activePreset}` : 'Saved to Camera successfully');
      }
    } catch (err) {
      setStatus('Failed to save settings');
    }
  };

  const resetDefaults = async () => {
    try {
      setStatus('Resetting...');
      const response = await fetch(`http://localhost:5000/api/camera/reset`, { method: 'POST' });
      const data = await response.json();
      if (data.success) {
        // Reset local state based on active dynamic layout
        const refreshedValues = { ...values };
        sections.forEach(sec => {
          sec.controls.forEach(c => {
            refreshedValues[c.id] = c.defaultValue;
          });
        });
        setValues(refreshedValues);
        setStatus('Restored defaults');
      }
    } catch (err) {
      setStatus('Failed to reset');
    }
  };

  const resetSection = (sectionId: string) => {
    const section = sections.find(s => s.id === sectionId);
    if (!section) return;

    const newValues = { ...values };
    section.controls.forEach(c => {
      newValues[c.id] = c.defaultValue;
      updateCamera(c.id, c.defaultValue);
    });
    setValues(newValues);
    setStatus(`Reset ${section.title} section`);
  };

  const handlePreset = async (presetId: string) => {
    setActivePreset(presetId);
    setStatus(`Loading Preset ${presetId}...`);

    try {
      const response = await fetch(`http://localhost:5000/api/camera/preset/load/${presetId}`);
      const data = await response.json();

      if (data.success && data.state) {
        const { zoom, pan, tilt } = data.state;
        const newValues = { ...values, zoom, pan, tilt };
        setValues(newValues);
        setStatus(`Loaded Preset ${presetId}`);
      } else {
        // Default empty preset state
        const defaultZoom = 100;
        const defaultPan = 0;
        const defaultTilt = 0;

        const newValues = { ...values, zoom: defaultZoom, pan: defaultPan, tilt: defaultTilt };
        setValues(newValues);
        updateCamera('zoom', defaultZoom);
        updateCamera('pan', defaultPan);
        updateCamera('tilt', defaultTilt);
        setStatus(`Loaded Default Preset ${presetId} (Unsaved)`);
      }
    } catch (err) {
      setStatus(`Failed to load preset ${presetId}`);
    }
  };

  const handleScreenshot = () => {
    const img = document.querySelector('.live-stream') as HTMLImageElement;
    if (!img) return;

    const canvas = document.createElement('canvas');
    canvas.width = img.naturalWidth || img.width;
    canvas.height = img.naturalHeight || img.height;
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.drawImage(img, 0, 0);
      const link = document.createElement('a');
      link.download = `Facecam_Capture_${new Date().toISOString()}.jpg`;
      link.href = canvas.toDataURL('image/jpeg', 0.95);
      link.click();
      setStatus('Screenshot captured and downloaded');
    }
  };

  const openScreenshotFolder = () => {
    // In a real desktop app (Electron/Tauri) we would trigger an OS shell command here.
    // Since this is a browser wrapper, we will notify the user where downloads went.
    setStatus('Check your browser Downloads folder');
  };

  const handleChange = (id: string, value: number) => {
    setValues(prev => ({ ...prev, [id]: value }));
  };

  return (
    <div className="elgato-layout pro-theme">
      {/* Sidebar Panel - now a flex container for cards */}
      <aside className="sidebar">
        <div className="sidebar-content">
          <DeviceCard
            selectedFormat={selectedFormat}
            onSave={saveToCamera}
            onReset={resetDefaults}
          />

          {sections.map(section => (
            <ControlSection
              key={section.id}
              title={section.title}
              id={section.id}
              controls={section.controls}
              values={values}
              collapsed={collapsed[section.id]}
              onToggle={toggleCollapse}
              onChange={handleChange}
              onCommit={updateCamera}
              onResetSection={resetSection}
              onPresetClick={handlePreset}
            />
          ))}
        </div>

        <ActionFooter status={status} />
      </aside>

      <CameraPreview
        streamUrl={streamUrl}
        isPreviewOn={isPreviewOn}
        showGrid={showGrid}
        onTogglePreview={() => setIsPreviewOn(!isPreviewOn)}
        onToggleGrid={() => setShowGrid(!showGrid)}
        onScreenshot={handleScreenshot}
        onOpenFolder={openScreenshotFolder}
        formats={formats}
        selectedFormat={selectedFormat}
        onFormatChange={(fmt) => {
          setSelectedFormat(fmt);
          if (isPreviewOn) {
            // Restart preview briefly to apply format
            setIsPreviewOn(false);
            setTimeout(() => setIsPreviewOn(true), 200);
          }
        }}
      />
    </div>
  )
}

export default App
