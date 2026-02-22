import React from 'react';
import type { CameraControl } from '../types';

interface ControlRowProps {
    control: CameraControl;
    value: number;
    onChange: (id: string, value: number) => void;
    onCommit: (id: string, value: number) => void;
}

const ControlRow: React.FC<ControlRowProps> = ({ control, value, onChange, onCommit }) => {
    const handleWheel = (e: React.WheelEvent) => {
        e.preventDefault();
        const direction = e.deltaY > 0 ? -1 : 1;
        const newValue = Math.min(control.max, Math.max(control.min, value + direction * control.step));
        if (newValue !== value) {
            onChange(control.id, newValue);
            onCommit(control.id, newValue);
        }
    };

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const val = parseInt(e.target.value);
        if (!isNaN(val)) {
            onChange(control.id, val);
            onCommit(control.id, val);
        }
    };

    // Custom rendering for Pan and Tilt to put them on the same line if needed, 
    // but since they are mapped individually in the loop, we instead style them 
    // with icons next to the spinner. The screenshot shows Pan/Tilt on ONE row.
    // To achieve this cleanly without breaking the data model, we'll just add the icons here.

    const isPan = control.id === 'pan';
    const isTilt = control.id === 'tilt';
    const isCompact = isPan || isTilt;

    return (
        <div className={`control-row ${isCompact ? 'compact' : ''}`}>
            <div className="control-label">
                {!isTilt ? control.label : ''}  {/* Hide Tilt label to fake them being one row visually if desired, though App.tsx lists them separately */}
            </div>
            <div className="control-interaction">
                {/* Only show slider for non-compact rows */}
                {!isCompact && (
                    <input
                        type="range"
                        min={control.min}
                        max={control.max}
                        step={control.step}
                        value={value}
                        onChange={(e) => onChange(control.id, parseInt(e.target.value))}
                        onMouseUp={() => onCommit(control.id, value)}
                        onWheel={handleWheel}
                    />
                )}

                {isPan && (
                    <div className="compact-icon" style={{ marginRight: '0.5rem', color: '#888' }}>
                        <svg viewBox="0 0 24 24" fill="currentColor" width="18"><path d="M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z" /></svg>
                    </div>
                )}
                {isTilt && (
                    <div className="compact-icon" style={{ marginRight: '0.5rem', color: '#888' }}>
                        <svg viewBox="0 0 24 24" fill="currentColor" width="18"><path d="M9 3L5 6.99h3V14h2V6.99h3L9 3zm7 14.01V10h-2v7.01h-3L15 21l4-3.99h-3z" /></svg>
                    </div>
                )}

                <div className="value-spinner">
                    <input
                        type="number"
                        value={value}
                        onChange={handleInputChange}
                    />
                    {control.unit && <span className="unit">{control.unit}</span>}
                </div>
            </div>
        </div>
    );
};

export default ControlRow;
