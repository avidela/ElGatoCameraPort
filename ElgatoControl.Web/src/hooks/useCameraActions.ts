import { useState } from 'react';
import type { ControlSectionData } from '../types';
import { API_BASE_URL } from '../config';

const API = `${API_BASE_URL}/api/camera`;

interface UseCameraActionsOptions {
    sections: ControlSectionData[];
    values: Record<string, number>;
    setValues: (updater: (prev: Record<string, number>) => Record<string, number>) => void;
}

export function useCameraActions({ sections, values, setValues }: UseCameraActionsOptions) {
    const [status, setStatus] = useState('Ready');
    const [activePreset, setActivePreset] = useState<string | null>(null);

    // ── Low-level: send a single control value to the API ──────────────────────
    const updateCamera = async (prop: string, val: number) => {
        try {
            const res = await fetch(`${API}/set`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ prop, val }),
            });
            const data = await res.json();
            if (!data.success) setStatus(`Error: ${data.message}`);
        } catch {
            setStatus('Failed to connect to API');
        }
    };

    // ── Save current values to hardware (and optionally active preset) ─────────
    const saveToCamera = async () => {
        try {
            setStatus('Saving to hardware...');

            if (activePreset) {
                await fetch(`${API}/preset/save/${activePreset}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ zoom: values.zoom, pan: values.pan, tilt: values.tilt }),
                });
            }

            const res = await fetch(`${API}/save`, { method: 'POST' });
            const data = await res.json();
            if (data.success) {
                setStatus(activePreset ? `Saved to Camera & Preset ${activePreset}` : 'Saved to Camera');
            }
        } catch {
            setStatus('Failed to save settings');
        }
    };

    // ── Reset all controls to hardware defaults ────────────────────────────────
    const resetDefaults = async () => {
        try {
            setStatus('Resetting...');
            const res = await fetch(`${API}/reset`, { method: 'POST' });
            const data = await res.json();
            if (data.success) {
                setValues(prev => {
                    const next = { ...prev };
                    sections.forEach(sec => sec.controls.forEach(c => { next[c.id] = c.defaultValue; }));
                    return next;
                });
                setStatus('Restored defaults');
            }
        } catch {
            setStatus('Failed to reset');
        }
    };

    // ── Reset a single section to defaults ─────────────────────────────────────
    const resetSection = (sectionId: string) => {
        const section = sections.find(s => s.id === sectionId);
        if (!section) return;

        setValues(prev => {
            const next = { ...prev };
            section.controls.forEach(c => { next[c.id] = c.defaultValue; });
            return next;
        });
        section.controls.forEach(c => updateCamera(c.id, c.defaultValue));
        setStatus(`Reset ${section.title} section`);
    };

    // ── Load a preset A/B/C/D ──────────────────────────────────────────────────
    const handlePreset = async (presetId: string) => {
        setActivePreset(presetId);
        setStatus(`Loading Preset ${presetId}...`);

        try {
            const res = await fetch(`${API}/preset/load/${presetId}`);
            const data = await res.json();

            if (data.success && data.state) {
                const { zoom, pan, tilt } = data.state;
                setValues(prev => ({ ...prev, zoom, pan, tilt }));
                setStatus(`Loaded Preset ${presetId}`);
            } else {
                const defaults = { zoom: 100, pan: 0, tilt: 0 };
                setValues(prev => ({ ...prev, ...defaults }));
                updateCamera('zoom', defaults.zoom);
                updateCamera('pan', defaults.pan);
                updateCamera('tilt', defaults.tilt);
                setStatus(`Loaded Default Preset ${presetId} (Unsaved)`);
            }
        } catch {
            setStatus(`Failed to load preset ${presetId}`);
        }
    };

    return {
        status,
        setStatus,
        activePreset,
        updateCamera,
        saveToCamera,
        resetDefaults,
        resetSection,
        handlePreset,
    };
}
