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
                    <div className="control-row pan-tilt-row">
                        <div className="control-label">Pan / Tilt</div>
                        <div className="control-interaction" style={{ flex: 1, display: 'flex', gap: '0.8rem', alignItems: 'center' }}>
                            {/* Pan Control */}
                            {controls.find(c => c.id === 'pan') && (() => {
                                const control = controls.find(c => c.id === 'pan')!;
                                return (
                                    <div className="compact-control" style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flex: 1 }}>
                                        <svg viewBox="0 0 24 24" fill="currentColor" width="16" style={{ color: '#888' }}><path d="M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z" /></svg>
                                        <div className="value-spinner" style={{ flex: 1 }}>
                                            <input
                                                type="number"
                                                min={control.min}
                                                max={control.max}
                                                step={control.step}
                                                value={values['pan'] || 0}
                                                onChange={(e) => {
                                                    const val = parseInt(e.target.value);
                                                    if (!isNaN(val)) onChange('pan', val);
                                                }}
                                                onBlur={(e) => {
                                                    const val = parseInt(e.target.value);
                                                    if (!isNaN(val)) onCommit('pan', val);
                                                }}
                                            />
                                            <span className="unit">%</span>
                                        </div>
                                    </div>
                                );
                            })()}

                            {/* Tilt Control */}
                            {controls.find(c => c.id === 'tilt') && (() => {
                                const control = controls.find(c => c.id === 'tilt')!;
                                return (
                                    <div className="compact-control" style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flex: 1 }}>
                                        <svg viewBox="0 0 24 24" fill="currentColor" width="16" style={{ color: '#888' }}><path d="M9 3L5 6.99h3V14h2V6.99h3L9 3zm7 14.01V10h-2v7.01h-3L15 21l4-3.99h-3z" /></svg>
                                        <div className="value-spinner" style={{ flex: 1 }}>
                                            <input
                                                type="number"
                                                min={control.min}
                                                max={control.max}
                                                step={control.step}
                                                value={values['tilt'] || 0}
                                                onChange={(e) => {
                                                    const val = parseInt(e.target.value);
                                                    if (!isNaN(val)) onChange('tilt', val);
                                                }}
                                                onBlur={(e) => {
                                                    const val = parseInt(e.target.value);
                                                    if (!isNaN(val)) onCommit('tilt', val);
                                                }}
                                            />
                                            <span className="unit">%</span>
                                        </div>
                                    </div>
                                );
                            })()}
                        </div>
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
                            onCommit(type, val);
                        }}
                    />
                )}
            </div>
        </div>
    );
};

export default ControlSection;
