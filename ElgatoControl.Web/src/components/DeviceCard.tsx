import React from 'react';
import type { VideoFormat } from '../types';

interface DeviceCardProps {
    selectedFormat: VideoFormat | null;
    onSave: () => void;
}

const DeviceCard: React.FC<DeviceCardProps> = ({ selectedFormat, onSave }) => {
    return (
        <div className="section static-section">
            <div className="section-header no-collapse device-header">
                <span className="section-title">Device</span>
                <button className="save-icon-btn" title="Save to Memory" onClick={onSave}>
                    <svg viewBox="0 0 24 24" fill="currentColor" width="16"><path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z" /></svg>
                    SAVE
                </button>
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
