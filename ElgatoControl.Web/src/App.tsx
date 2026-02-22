import { useState, useEffect } from 'react';
import './App.css';
import ControlSection from './components/ControlSection';
import CameraPreview from './components/CameraPreview';
import type { ControlSectionData, VideoFormat } from './types';

const sections: ControlSectionData[] = [
  {
    title: 'Frame',
    id: 'frame',
    controls: [
      { id: 'zoom', label: 'Zoom / FOV', min: 100, max: 400, step: 1, defaultValue: 100, unit: '%' },
      { id: 'pan', label: 'Pan', min: -2592000, max: 2592000, step: 3600, defaultValue: 0 },
      { id: 'tilt', label: 'Tilt', min: -1458000, max: 1458000, step: 3600, defaultValue: 0 },
    ]
  },
  {
    title: 'Picture',
    id: 'picture',
    controls: [
      { id: 'contrast', label: 'Contrast', min: 0, max: 100, step: 1, defaultValue: 80, unit: '%' },
      { id: 'saturation', label: 'Saturation', min: 0, max: 127, step: 1, defaultValue: 64, unit: '%' },
      { id: 'sharpness', label: 'Sharpness', min: 0, max: 255, step: 1, defaultValue: 128 },
    ]
  },
  {
    title: 'Exposure',
    id: 'exposure',
    controls: [
      { id: 'exposure', label: 'Shutter Speed', min: 1, max: 2500, step: 1, defaultValue: 156 },
      { id: 'gain', label: 'ISO (Gain)', min: 0, max: 88, step: 1, defaultValue: 0 },
      { id: 'white_balance', label: 'White Balance', min: 2800, max: 7500, step: 10, defaultValue: 5000, unit: 'K' },
      { id: 'brightness', label: 'Brightness', min: -9, max: 9, step: 1, defaultValue: 0 },
    ]
  }
];

const allControls = sections.flatMap(s => s.controls);

function App() {
  const [isPreviewOn, setIsPreviewOn] = useState(true);
  const [showGrid, setShowGrid] = useState(true);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({
    frame: false, picture: false, exposure: false
  });
  const [streamUrl, setStreamUrl] = useState('');
  const [status, setStatus] = useState('Ready');

  // Format state
  const [formats, setFormats] = useState<VideoFormat[]>([]);
  const [selectedFormat, setSelectedFormat] = useState<VideoFormat | null>(null);
  const [values, setValues] = useState<Record<string, number>>(
    Object.fromEntries(allControls.map(c => [c.id, c.defaultValue]))
  );

  useEffect(() => {
    // Fetch initial controls and formats
    fetch('http://localhost:5000/api/camera/controls')
      .then(res => res.json())
      .then(() => {
        // Here we'd map raw active values, ignoring for this demo
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
      const response = await fetch(`http://localhost:5000/api/camera/save`, { method: 'POST' });
      const data = await response.json();
      if (data.success) setStatus('Settings stored.');
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
        setValues(Object.fromEntries(allControls.map(c => [c.id, c.defaultValue])));
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

  const handlePreset = (presetId: string) => {
    const presetKey = `preset_${presetId}`;
    const saved = localStorage.getItem(presetKey);

    // For now, if no preset exists, we "save" current state to it
    // In a real app we might have a dedicated save button, but let's toggle: save if same, load if different?
    // User asked to "build this" - let's make it load if something is there, or save if empty for demo.
    if (!saved) {
      const state = JSON.stringify({ zoom: values.zoom, pan: values.pan, tilt: values.tilt });
      localStorage.setItem(presetKey, state);
      setStatus(`Saved Preset ${presetId}`);
    } else {
      const { zoom, pan, tilt } = JSON.parse(saved);
      const newValues = { ...values, zoom, pan, tilt };
      setValues(newValues);
      updateCamera('zoom', zoom);
      updateCamera('pan', pan);
      updateCamera('tilt', tilt);
      setStatus(`Loaded Preset ${presetId}`);
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
      setStatus('Screenshot captured');
    }
  };

  const handleChange = (id: string, value: number) => {
    setValues(prev => ({ ...prev, [id]: value }));
  };

  const toggleCollapse = (sectionId: string) => {
    setCollapsed(prev => ({ ...prev, [sectionId]: !prev[sectionId] }));
  };

  return (
    <div className="elgato-layout pro-theme">
      {/* Sidebar Panel */}
      <aside className="sidebar">
        <div className="sidebar-header">
          <div className="nav-tabs">
            <button className="tab active"><svg viewBox="0 0 24 24" width="14" fill="currentColor"><path d="M12 11c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm6 2h-1.42l-1.58-5.54A2.003 2.003 0 0013.06 6h-2.12c-.89 0-1.68.59-1.9 1.46L7.42 13H2v2h6.58l.74-2.6L11 17l-1 5h2l1.45-7.25L15 17h5v-2z" /></svg> Camera</button>
            <button className="tab"><svg viewBox="0 0 24 24" width="14" fill="currentColor"><path d="M22 6H2v12h20V6zm-2 10H4V8h16v8zm-8.8-5.32L9.5 8.5H7.72l2.36 2.82L7.54 14h1.74l1.92-2.32L13.12 14h1.76l-2.5-3zM15.5 14v-1.5h3V14h-3z" /></svg> Effects</button>
            <button className="tab"><svg viewBox="0 0 24 24" width="14" fill="currentColor"><path d="M3 13h2v-2H3v2zm0 4h2v-2H3v2zm0-8h2V7H3v2zm4 4h14v-2H7v2zm0 4h14v-2H7v2zM7 7v2h14V7H7z" /></svg> Prompter</button>
          </div>
          <div className="header-actions">
            <button className="save-icon-btn" title="Save to Memory" onClick={saveToCamera}>
              <svg viewBox="0 0 24 24" fill="currentColor" width="16"><path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z" /></svg>
              SAVE
            </button>
          </div>
        </div>

        <div className="sidebar-content">
          <div className="section static-section">
            <div className="section-header no-collapse">
              <span className="section-title">Device</span>
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
