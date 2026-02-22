import React from 'react';

interface CameraPreviewProps {
    streamUrl: string;
    isPreviewOn: boolean;
    showGrid: boolean;
    onTogglePreview: () => void;
    onToggleGrid: () => void;
}

const CameraPreview: React.FC<CameraPreviewProps> = ({
    streamUrl, isPreviewOn, showGrid, onTogglePreview, onToggleGrid
}) => {
    return (
        <main className="main-content">
            <div className="preview-container">
                {isPreviewOn && streamUrl ? (
                    <div className={`stream-wrapper ${showGrid ? 'show-grid' : ''}`}>
                        <img src={streamUrl} alt="Live Preview" className="live-stream" />
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
                <button
                    className={`toolbar-btn ${showGrid ? 'active' : ''}`}
                    onClick={onToggleGrid}
                    title="Toggle Grid"
                >
                    <svg viewBox="0 0 24 24" fill="currentColor" width="20">
                        <path d="M20 2H4c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zM8 20H4v-4h4v4zm0-6H4v-4h4v4zm0-6H4V4h4v4zm6 12h-4v-4h4v4zm0-6h-4v-4h4v4zm0-6h-4V4h4v4zm6 12h-4v-4h4v4zm0-6h-4v-4h4v4zm0-6h-4V4h4v4z" />
                    </svg>
                </button>
                <button
                    className="toolbar-btn"
                    onClick={onTogglePreview}
                    title={isPreviewOn ? "Stop Preview" : "Start Preview"}
                >
                    {isPreviewOn ?
                        <svg viewBox="0 0 24 24" fill="currentColor" width="20">
                            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-1 14H9V8h2v8zm4 0h-2V8h2v8z" />
                        </svg> :
                        <svg viewBox="0 0 24 24" fill="currentColor" width="20">
                            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 14.5v-9l6 4.5-6 4.5z" />
                        </svg>
                    }
                </button>
            </div>
        </main>
    );
};

export default CameraPreview;
