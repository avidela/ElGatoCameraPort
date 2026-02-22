import { useState, useEffect } from 'react';
import './App.css';
import ControlSection from './components/ControlSection';
import CameraPreview from './components/CameraPreview';
import type { ControlSectionData } from './types';

const sections: ControlSectionData[] = [
  {
    title: 'Frame',
    id: 'frame',
    controls: [
      { id: 'zoom', label: 'Zoom', min: 100, max: 400, step: 1, defaultValue: 100, unit: '%' },
      { id: 'pan', label: 'Pan (X)', min: -2592000, max: 2592000, step: 3600, defaultValue: 0 },
      { id: 'tilt', label: 'Tilt (Y)', min: -1458000, max: 1458000, step: 3600, defaultValue: 0 },
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
  const [status, setStatus] = useState<string>('Ready');
  const [isPreviewOn, setIsPreviewOn] = useState(true);
  const [showGrid, setShowGrid] = useState(false);
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({
    frame: false, picture: false, exposure: false
  });
  const [streamUrl, setStreamUrl] = useState<string>('');
  const [values, setValues] = useState<Record<string, number>>(
    Object.fromEntries(allControls.map(c => [c.id, c.defaultValue]))
  );

  useEffect(() => {
    let timer: number;
    if (isPreviewOn) {
      setStreamUrl(`http://localhost:5000/api/camera/stream?t=${Date.now()}`);
    } else {
      setStreamUrl('about:blank');
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
            <button className="tab active">Camera</button>
            <button className="tab">Effects</button>
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
                <div className="info-value">Elgato Facecam MK.2</div>
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
      />
    </div>
  )
}

export default App
