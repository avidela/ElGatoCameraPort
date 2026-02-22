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

    return (
        <div className="control-row">
            <div className="control-label">{control.label}</div>
            <div className="control-interaction">
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
