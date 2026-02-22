import React from 'react';
import ControlRow from './ControlRow';
import type { CameraControl } from '../types';
import ZoomPreview from './ZoomPreview';

interface ControlSectionProps {
    title: string;
    id: string;
    controls: CameraControl[];
    values: Record<string, number>;
    collapsed: boolean;
    onToggle: (id: string) => void;
    onChange: (id: string, value: number) => void;
    onCommit: (id: string, value: number) => void;
    onResetSection: (sectionId: string) => void;
    onPresetClick?: (presetId: string) => void;
}

const ControlSection: React.FC<ControlSectionProps> = ({
    title, id, controls, values, collapsed, onToggle, onChange, onCommit, onResetSection, onPresetClick
}) => {
    return (
        <div className={`section ${collapsed ? 'collapsed' : ''} `}>
            <div className="section-header" onClick={() => onToggle(id)}>
                <span className="arrow">{collapsed ? '▶' : '▼'}</span>
                <span className="section-title">{title}</span>
                <button
                    className="reset-inline"
                    title="Reset Section"
                    onClick={(e) => {
                        e.stopPropagation();
                        onResetSection(id);
                    }}
                >
                    ↺
                </button>
            </div>
            <div className="section-body">
                {/* 1. Normal Controls (like Zoom) */}
                {controls.filter(c => c.id !== 'pan' && c.id !== 'tilt').map(control => (
                    <ControlRow
                        key={control.id}
                        control={control}
                        value={values[control.id]}
                        onChange={onChange}
                        onCommit={onCommit}
                    />
                ))}

                {/* 2. Pan/Tilt Side-by-Side row */}
                {id === 'frame' && controls.some(c => c.id === 'pan' || c.id === 'tilt') && (
                    <div className="flex-row gap-2">
                        {controls.filter(c => c.id === 'pan' || c.id === 'tilt').map(control => (
                            <ControlRow
                                key={control.id}
                                control={control}
                                value={values[control.id]}
                                onChange={onChange}
                                onCommit={onCommit}
                            />
                        ))}
                    </div>
                )}

                {/* 3. Preview Box and Presets */}
                {id === 'frame' && onPresetClick && (
                    <ZoomPreview
                        zoom={values['zoom'] || 100}
                        pan={values['pan'] || 0}
                        tilt={values['tilt'] || 0}
                        onPresetClick={onPresetClick}
                        onDragChange={(type, val) => {
                            onChange(type, val);
                            onCommit(type, val); // Trigger live API hit while dragging
                        }}
                    />
                )}
            </div>
        </div>
    );
};

export default ControlSection;
