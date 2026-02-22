import { useState, useEffect } from 'react';
import './App.css';
import ControlSection from './components/ControlSection';
import CameraPreview from './components/CameraPreview';
import type { ControlSectionData, VideoFormat } from './types';

function App() {
  const [isPreviewOn, setIsPreviewOn] = useState(false); // Default OFF per user request
  const [showGrid, setShowGrid] = useState(true);
  const [sections, setSections] = useState<ControlSectionData[]>([]);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
  const [streamUrl, setStreamUrl] = useState('');
  const [status, setStatus] = useState('Ready');
  const [activePreset, setActivePreset] = useState<string | null>(null);

  // Format state
  const [formats, setFormats] = useState<VideoFormat[]>([]);
  const [selectedFormat, setSelectedFormat] = useState<VideoFormat | null>(null);
  const [values, setValues] = useState<Record<string, number>>({});

  useEffect(() => {
    // Fetch Dynamic Backend Layout
    fetch('http://localhost:5000/api/camera/layout')
      .then(res => res.json())
      .then(data => {
        if (data.success && data.layout) {
          setSections(data.layout);

          // Initialize default collapse states and values
          const newCollapsed: Record<string, boolean> = {};
          const newValues: Record<string, number> = {};

          data.layout.forEach((sec: ControlSectionData) => {
            newCollapsed[sec.id] = false;
            sec.controls.forEach(c => {
              newValues[c.id] = c.defaultValue;
            });
          });

          setCollapsed(newCollapsed);
          setValues(newValues);
        }
      })
      .catch(console.error);

    fetch('http://localhost:5000/api/camera/formats')
      .then(res => res.json())
      .then(data => {
        if (data.success && data.formats.length > 0) {
          const sorted = data.formats.sort((a: VideoFormat, b: VideoFormat) =>
            (b.width * b.height) - (a.width * a.height) || b.fps - a.fps
          );
          setFormats(sorted);
          setSelectedFormat(sorted[0]);
        }
      })
      .catch(console.error);
  }, []);

  useEffect(() => {
    let timer: number;
    if (isPreviewOn) {
      if (selectedFormat) {
        setStreamUrl(`http://localhost:5000/api/camera/stream?t=${Date.now()}&w=${selectedFormat.width}&h=${selectedFormat.height}&fps=${selectedFormat.fps}`);
      } else {
        setStreamUrl(`http://localhost:5000/api/camera/stream?t=${Date.now()}`);
      }
    } else {
      // Explicitly tell backend to kill ffmpeg so device is released instantly
      fetch(`http://localhost:5000/api/camera/stream/stop`, { method: 'POST' }).catch(console.error);
      setStreamUrl('about:blank');
      setStatus('Camera released for other apps');
      timer = window.setTimeout(() => setStreamUrl(''), 100);
    }
    return () => clearTimeout(timer);
  }, [isPreviewOn]);

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

  const toggleCollapse = (sectionId: string) => {
    setCollapsed(prev => ({ ...prev, [sectionId]: !prev[sectionId] }));
  };

  return (
    <div className="elgato-layout pro-theme">
      {/* Sidebar Panel - now a flex container for cards */}
      <aside className="sidebar">
        <div className="sidebar-content">
          <div className="section static-section">
            <div className="section-header no-collapse device-header">
              <span className="section-title">Device</span>
              <button className="save-icon-btn" title="Save to Memory" onClick={saveToCamera}>
                <svg viewBox="0 0 24 24" fill="currentColor" width="16"><path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z" /></svg>
                SAVE
              </button>
            </div>
            <div className="section-body">
              <div className="device-info">
                <div className="info-label">Input</div>
                <select className="info-value-select">
                  <option>Elgato Facecam MK.2</option>
                </select>
              </div>
              <div className="device-info">
                <div className="info-label">Format</div>
                <div className="info-value format-text">
                  {selectedFormat ? `${selectedFormat.width}x${selectedFormat.height} @ ${selectedFormat.fps}fps (${selectedFormat.codec})` : 'Loading...'}
                </div>
              </div>
            </div>
          </div>

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

        <div className="sidebar-footer">
          <button className="full-reset-btn" onClick={resetDefaults}>Reset to Defaults</button>
          <div className="status-bar">{status}</div>
        </div>
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
