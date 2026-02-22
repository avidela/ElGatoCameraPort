import React, { useRef, useState, useEffect } from 'react';

interface ZoomPreviewProps {
    zoom: number; // 100 to 400
    pan: number;  // -2592000 to 2592000
    tilt: number; // -1458000 to 1458000
    onPresetClick: (id: string) => void;
    onDragChange: (type: 'pan' | 'tilt', value: number) => void;
}

const ZoomPreview: React.FC<ZoomPreviewProps> = ({ zoom, pan, tilt, onPresetClick, onDragChange }) => {
    // Map zoom percentage to box size (e.g., Zoom 100 = 100%, Zoom 400 = 25%)
    const sizePerc = 100 / (zoom / 100);

    // Normalize Pan (-100 to 100) to percentage (-50% to 50% relative to center)
    // If box size is S, its center can move by (100-S)/2 in each direction.
    const maxPan = 100;
    const maxTilt = 100;

    const panPerc = (pan / maxPan) * ((100 - sizePerc) / 2);
    const tiltPerc = -(tilt / maxTilt) * ((100 - sizePerc) / 2); // Tilt is usually inverted up/down

    const containerRef = useRef<HTMLDivElement>(null);
    const [isDragging, setIsDragging] = useState(false);

    const handleMouseDown = (e: React.MouseEvent) => {
        setIsDragging(true);
        updatePanTiltFromMouse(e.clientX, e.clientY, false);
    };

    const updatePanTiltFromMouse = (clientX: number, clientY: number, commit: boolean) => {
        if (!containerRef.current) return;

        // At zoom 100%, pan/tilt does nothing so we should bail.
        if (sizePerc >= 100) return;

        const rect = containerRef.current.getBoundingClientRect();

        // Calculate click position as a percentage of the container (-50% to 50%)
        // Center is 0, right is 50%, left is -50%, bottom is 50%, top is -50%
        let xPerc = ((clientX - rect.left) / rect.width) * 100 - 50;
        let yPerc = ((clientY - rect.top) / rect.height) * 100 - 50;

        // Clamp to allowed travel radius based on current zoom level box size
        const maxTravel = (100 - sizePerc) / 2;
        xPerc = Math.max(-maxTravel, Math.min(xPerc, maxTravel));
        yPerc = Math.max(-maxTravel, Math.min(yPerc, maxTravel));

        // Remap percentage back to absolute values
        const newPan = (xPerc / maxTravel) * maxPan;
        const newTilt = -(yPerc / maxTravel) * maxTilt; // Reverse back

        const finalPan = Math.round(newPan);
        const finalTilt = Math.round(newTilt);

        if (commit) {
            onDragChange('pan', finalPan);
            onDragChange('tilt', finalTilt);
        } else {
            onDragChange('pan', finalPan);
            onDragChange('tilt', finalTilt);
        }
    };

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (isDragging) updatePanTiltFromMouse(e.clientX, e.clientY, false);
        };
        const handleMouseUp = (e: MouseEvent) => {
            if (isDragging) {
                setIsDragging(false);
                updatePanTiltFromMouse(e.clientX, e.clientY, true);
            }
        };

        if (isDragging) {
            window.addEventListener('mousemove', handleMouseMove);
            window.addEventListener('mouseup', handleMouseUp);
        }

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
        // Include update logic deps so React tracks state through closures
    }, [isDragging, sizePerc, pan, tilt]);

    const boxStyle: React.CSSProperties = {
        width: `${sizePerc}%`,
        height: `${sizePerc}%`,
        left: `${50 + panPerc - sizePerc / 2}%`,
        top: `${50 + tiltPerc - sizePerc / 2}%`,
        position: 'absolute',
        border: `2px solid ${isDragging ? '#00ccff' : '#00aaff'}`,
        backgroundColor: `rgba(0, 170, 255, ${isDragging ? 0.5 : 0.3})`,
        transition: isDragging ? 'none' : 'all 0.1s ease-out',
        cursor: isDragging ? 'grabbing' : 'grab',
        boxSizing: 'border-box'
    };

    return (
        <div className="zoom-preview-container">
            <div
                className="sensor-area"
                ref={containerRef}
                onMouseDown={handleMouseDown}
            >
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
