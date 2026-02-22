import { useState } from 'react';
import './styles/main.scss';
import ControlSection from './components/ControlSection';
import CameraPreview from './components/CameraPreview';
import DeviceCard from './components/DeviceCard';
import ActionFooter from './components/ActionFooter';
import { useCameraLayout } from './hooks/useCameraLayout';
import { useCameraPreview } from './hooks/useCameraPreview';
import { useCameraActions } from './hooks/useCameraActions';
import { useScreenshot } from './hooks/useScreenshot';

function App() {
  const [isPreviewOn, setIsPreviewOn] = useState(false);

  const { sections, collapsed, toggleCollapse, values, setValues, handleChange } = useCameraLayout();
  const { formats, selectedFormat, setSelectedFormat, streamUrl, showGrid, setShowGrid } = useCameraPreview(isPreviewOn);
  const { status, setStatus, updateCamera, saveToCamera, resetDefaults, resetSection, handlePreset } = useCameraActions({ sections, values, setValues });
  const { handleScreenshot, openScreenshotFolder } = useScreenshot(setStatus);

  return (
    <div className="elgato-layout pro-theme">
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
        onTogglePreview={() => setIsPreviewOn(v => !v)}
        onToggleGrid={() => setShowGrid(v => !v)}
        onScreenshot={handleScreenshot}
        onOpenFolder={openScreenshotFolder}
        formats={formats}
        selectedFormat={selectedFormat}
        onFormatChange={fmt => {
          setSelectedFormat(fmt);
          if (isPreviewOn) {
            setIsPreviewOn(false);
            setTimeout(() => setIsPreviewOn(true), 200);
          }
        }}
      />
    </div>
  );
}

export default App;
