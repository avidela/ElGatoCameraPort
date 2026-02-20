import { useState } from 'react'
import './App.css'

interface CameraControl {
  id: string;
  label: string;
  min: number;
  max: number;
  step: number;
  defaultValue: number;
}

const controls: CameraControl[] = [
  { id: 'zoom', label: 'Digital Zoom', min: 100, max: 400, step: 1, defaultValue: 100 },
  { id: 'exposure', label: 'Exposure', min: 1, max: 5000, step: 1, defaultValue: 100 },
  { id: 'gain', label: 'Gain', min: 0, max: 255, step: 1, defaultValue: 0 },
  { id: 'white_balance', label: 'White Balance (K)', min: 2800, max: 7500, step: 100, defaultValue: 4500 },
  { id: 'brightness', label: 'Brightness', min: 0, max: 255, step: 1, defaultValue: 128 },
  { id: 'contrast', label: 'Contrast', min: 0, max: 255, step: 1, defaultValue: 128 },
];

function App() {
  const [status, setStatus] = useState<string>('Ready');
  const [isPreviewOn, setIsPreviewOn] = useState(true);
  const [values, setValues] = useState<Record<string, number>>(
    Object.fromEntries(controls.map(c => [c.id, c.defaultValue]))
  );

  const updateCamera = async (prop: string, val: number) => {
    try {
      const response = await fetch(`http://localhost:5000/api/camera/set`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ prop, val })
      });
      const data = await response.json();
      if (data.success) {
        setStatus(`Updated ${prop} to ${val}`);
      } else {
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
      if (data.success) {
        setStatus('Settings stored in camera flash memory');
      }
    } catch (err) {
      setStatus('Failed to save settings');
    }
  };

  const handleChange = (id: string, value: number) => {
    setValues(prev => ({ ...prev, [id]: value }));
  };

  return (
    <div className="app-container">
      <header>
        <h1>Facecam MK.2</h1>
        <div className={`status-badge ${status.includes('Error') ? 'error' : ''}`}>
          {status}
        </div>
      </header>

      <div className="preview-section">
        {isPreviewOn ? (
          <img 
            src="http://localhost:5000/api/camera/stream" 
            alt="Live Preview" 
            className="live-stream"
            onError={() => {
              setStatus('Stream Error: Is ffmpeg installed?');
              setIsPreviewOn(false);
            }}
          />
        ) : (
          <div className="stream-placeholder">Preview Off</div>
        )}
        <button className="toggle-preview" onClick={() => setIsPreviewOn(!isPreviewOn)}>
          {isPreviewOn ? 'Stop Preview' : 'Start Preview'}
        </button>
      </div>

      <main className="controls-grid">
        {controls.map((control) => (
          <div key={control.id} className="control-card">
            <div className="control-header">
              <label>{control.label}</label>
              <span className="control-value">{values[control.id]}</span>
            </div>
            <input
              type="range"
              min={control.min}
              max={control.max}
              step={control.step}
              value={values[control.id]}
              onChange={(e) => handleChange(control.id, parseInt(e.target.value))}
              onMouseUp={() => updateCamera(control.id, values[control.id])}
              onTouchEnd={() => updateCamera(control.id, values[control.id])}
            />
          </div>
        ))}
      </main>

      <footer>
        <button className="save-btn" onClick={saveToCamera}>
          Save to Camera
        </button>
      </footer>
    </div>
  )
}

export default App
