import React from 'react';
import type { VideoFormat } from '../types';

interface CameraPreviewProps {
    streamUrl: string;
    isPreviewOn: boolean;
    showGrid: boolean;
    onTogglePreview: () => void;
    onToggleGrid: () => void;
    onScreenshot: () => void;
    onOpenFolder: () => void;
    formats: VideoFormat[];
    selectedFormat: VideoFormat | null;
    onFormatChange: (format: VideoFormat) => void;
}

const CameraPreview: React.FC<CameraPreviewProps> = ({
    streamUrl, isPreviewOn, showGrid, onTogglePreview, onToggleGrid, onScreenshot, onOpenFolder, formats, selectedFormat, onFormatChange
}) => {
    return (
        <main className="main-content">
            <div className="preview-container">
                {isPreviewOn && streamUrl ? (
                    <div className={`stream-wrapper ${showGrid ? 'show-grid' : ''}`}>
                        <img crossOrigin="anonymous" src={streamUrl} alt="Live Preview" className="live-stream" />
                        <div className="grid-overlay">
                            <div className="v-line"></div><div className="v-line"></div>
                            <div className="h-line"></div><div className="h-line"></div>
                        </div>
                    </div>
                ) : (
                    <div className="stream-placeholder">
                        <p>Camera Disengaged</p>
                        <button className="primary-btn" onClick={onTogglePreview}>
                            Engage Camera
                        </button>
                    </div>
                )}
            </div>

            <div className="preview-toolbar">
                <div className="toolbar-left">
                    <span className="preview-format-label">Preview Format</span>
                    <select
                        className="preview-format-select"
                        value={selectedFormat ? `${selectedFormat.width}x${selectedFormat.height}@${selectedFormat.fps}` : ''}
                        onChange={(e) => {
                            const match = formats.find(f => `${f.width}x${f.height}@${f.fps}` === e.target.value);
                            if (match) onFormatChange(match);
                        }}
                    >
                        {formats.map((fmt, idx) => (
                            <option key={idx} value={`${fmt.width}x${fmt.height}@${fmt.fps}`}>
                                {fmt.height}p{fmt.fps}
                            </option>
                        ))}
                    </select>
                </div>

                <div className="toolbar-center">
                    <button className="icon-btn screenshot-btn" onClick={onScreenshot} title="Capture Screenshot">
                        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M9 2L7.17 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2h-3.17L15 2H9zm3 15c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5z" /></svg>
                    </button>
                    <button className="icon-btn folder-btn" onClick={onOpenFolder} title="Open Screenshot Folder">
                        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z" /></svg>
                    </button>
                </div>

                <div className="toolbar-right">
                    <button
                        className={`icon-btn toggle-btn ${isPreviewOn ? 'active' : ''}`}
                        onClick={onTogglePreview}
                        title={isPreviewOn ? "Stop Preview" : "Start Preview"}
                    >
                        {isPreviewOn ?
                            <svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z" /></svg> :
                            <svg viewBox="0 0 24 24" fill="currentColor"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 14.5v-9l6 4.5-6 4.5z" /></svg>
                        }
                    </button>
                    <button
                        className={`icon-btn toggle-btn ${showGrid ? 'active' : ''}`}
                        onClick={onToggleGrid}
                        title="Toggle Grid"
                    >
                        <svg viewBox="0 0 24 24" fill="currentColor"><path d="M20 2H4c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zM8 20H4v-4h4v4zm0-6H4v-4h4v4zm0-6H4V4h4v4zm6 12h-4v-4h4v4zm0-6h-4v-4h4v4zm0-6h-4V4h4v4zm6 12h-4v-4h4v4zm0-6h-4v-4h4v4zm0-6h-4V4h4v4z" /></svg>
                    </button>
                </div>
            </div>
        </main>
    );
};

export default CameraPreview;
