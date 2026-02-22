import React from 'react';
import ControlRow from './ControlRow';
import type { CameraControl } from '../types';

interface ControlSectionProps {
    title: string;
    id: string;
    controls: CameraControl[];
    values: Record<string, number>;
    collapsed: boolean;
    onToggle: (id: string) => void;
    onChange: (id: string, value: number) => void;
    onCommit: (id: string, value: number) => void;
}

const ControlSection: React.FC<ControlSectionProps> = ({
    title, id, controls, values, collapsed, onToggle, onChange, onCommit
}) => {
    return (
        <div className={`section ${collapsed ? 'collapsed' : ''}`}>
            <div className="section-header" onClick={() => onToggle(id)}>
                <span className="arrow">{collapsed ? '▶' : '▼'}</span>
                <span className="section-title">{title}</span>
                <button
                    className="reset-inline"
                    title="Reset Section"
                    onClick={(e) => {
                        e.stopPropagation();
                        // Reset logic would go here if needed per section
                    }}
                >
                    ↺
                </button>
            </div>
            <div className="section-body">
                {controls.map(control => (
                    <ControlRow
                        key={control.id}
                        control={control}
                        value={values[control.id]}
                        onChange={onChange}
                        onCommit={onCommit}
                    />
                ))}
            </div>
        </div>
    );
};

export default ControlSection;
