import React from 'react';

interface ZoomPreviewProps {
    zoom: number; // 100 to 400
    pan: number;  // -2592000 to 2592000
    tilt: number; // -1458000 to 1458000
    onPresetClick: (id: string) => void;
}

const ZoomPreview: React.FC<ZoomPreviewProps> = ({ zoom, pan, tilt, onPresetClick }) => {
    // Map zoom percentage to box size (e.g., Zoom 100 = 100%, Zoom 400 = 25%)
    const sizePerc = 100 / (zoom / 100);

    // Normalize Pan (-2592000 to 2592000) to percentage (-50% to 50% relative to center)
    // But we need total travel distance. 
    // If box size is S, its center can move by (100-S)/2 in each direction.
    const maxPan = 2592000;
    const maxTilt = 1458000;

    const panPerc = (pan / maxPan) * ((100 - sizePerc) / 2);
    const tiltPerc = -(tilt / maxTilt) * ((100 - sizePerc) / 2); // Tilt is usually inverted up/down

    const boxStyle: React.CSSProperties = {
        width: `${sizePerc}%`,
        height: `${sizePerc}%`,
        left: `${50 + panPerc - sizePerc / 2}%`,
        top: `${50 + tiltPerc - sizePerc / 2}%`,
        position: 'absolute',
        border: '2px solid #00aaff',
        backgroundColor: 'rgba(0, 170, 255, 0.3)',
        transition: 'all 0.1s ease-out'
    };

    return (
        <div className="zoom-preview-container">
            <div className="sensor-area">
                <div className="zoom-box" style={boxStyle}></div>
            </div>
            <div className="presets-row">
                {['A', 'B', 'C', 'D'].map(p => (
                    <button key={p} className="preset-btn" onClick={() => onPresetClick(p)}>
                        {p}
                    </button>
                ))}
            </div>
        </div>
    );
};

export default ZoomPreview;
