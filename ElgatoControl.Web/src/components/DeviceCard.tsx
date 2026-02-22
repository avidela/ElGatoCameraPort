import React, { useState } from 'react';
import type { VideoFormat } from '../types';

interface DeviceCardProps {
    selectedFormat: VideoFormat | null;
    onSave: () => void;
    onReset: () => void;
}

const DeviceCard: React.FC<DeviceCardProps> = ({ selectedFormat, onSave, onReset }) => {
    const [collapsed, setCollapsed] = useState(false);

    return (
        <div className={`section ${collapsed ? 'collapsed' : ''}`}>
            <div className="section-header device-header" onClick={() => setCollapsed(c => !c)}>
                <span className="arrow">{collapsed ? '▶' : '▼'}</span>
                <span className="section-title">Device</span>
                <div className="device-header-actions" onClick={e => e.stopPropagation()}>
                    <button className="action-icon-btn reset-btn" title="Reset to Defaults" onClick={onReset}>
                        <svg viewBox="0 0 24 24" fill="currentColor" width="14">
                            <path d="M12 5V1L7 6l5 5V7c3.31 0 6 2.69 6 6s-2.69 6-6 6-6-2.69-6-6H4c0 4.42 3.58 8 8 8s8-3.58 8-8-3.58-8-8-8z" />
                        </svg>
                    </button>
                    <button className="action-icon-btn save-btn" title="Save to Memory" onClick={onSave}>
                        <svg viewBox="0 0 24 24" fill="currentColor" width="14">
                            <path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z" />
                        </svg>
                        SAVE
                    </button>
                </div>
            </div>
            <div className="section-body">
                <div className="device-info">
                    <div className="info-label">Input</div>
                    <select className="info-value-select" disabled>
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
    );
};

export default DeviceCard;
