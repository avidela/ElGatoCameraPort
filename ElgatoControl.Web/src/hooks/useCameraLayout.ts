import { useState, useEffect } from 'react';
import type { ControlSectionData } from '../types';

export function useCameraLayout(
    setValues: (values: Record<string, number>) => void
) {
    const [sections, setSections] = useState<ControlSectionData[]>([]);
    const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});

    useEffect(() => {
        // 1. Fetch Layout Schema
        fetch('http://localhost:5000/api/camera/layout')
            .then(res => res.json())
            .then(layoutData => {
                if (layoutData.success && layoutData.layout) {
                    setSections(layoutData.layout);

                    // 2. Fetch Active Hardware Controls
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
                        })
                        .catch(console.error);
                }
            })
            .catch(console.error);
    }, []);

    const toggleCollapse = (sectionId: string) => {
        setCollapsed(prev => ({ ...prev, [sectionId]: !prev[sectionId] }));
    };

    return { sections, collapsed, toggleCollapse };
}
