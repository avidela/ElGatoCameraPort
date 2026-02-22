import { useState, useEffect } from 'react';
import type { ControlSectionData } from '../types';

export function useCameraLayout() {
    const [sections, setSections] = useState<ControlSectionData[]>([]);
    const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});
    const [values, setValues] = useState<Record<string, number>>({});

    useEffect(() => {
        fetch('http://localhost:5000/api/camera/layout')
            .then(res => res.json())
            .then(layoutData => {
                if (layoutData.success && layoutData.layout) {
                    fetch('http://localhost:5000/api/camera/controls')
                        .then(res => res.json())
                        .then(controlsData => {
                            const newCollapsed: Record<string, boolean> = {};
                            const newValues: Record<string, number> = {};
                            const backendValues = (controlsData.success && controlsData.values) ? controlsData.values : {};

                            layoutData.layout.forEach((sec: ControlSectionData) => {
                                newCollapsed[sec.id] = false;
                                sec.controls.forEach(c => {
                                    newValues[c.id] = backendValues[c.id] !== undefined
                                        ? backendValues[c.id]
                                        : c.defaultValue;
                                });
                            });

                            setCollapsed(newCollapsed);
                            setValues(newValues);
                            setSections(layoutData.layout);
                        })
                        .catch(console.error);
                }
            })
            .catch(console.error);
    }, []);

    const toggleCollapse = (sectionId: string) => {
        setCollapsed(prev => ({ ...prev, [sectionId]: !prev[sectionId] }));
    };

    const handleChange = (id: string, value: number) => {
        setValues(prev => ({ ...prev, [id]: value }));
    };

    return { sections, collapsed, toggleCollapse, values, setValues, handleChange };
}
