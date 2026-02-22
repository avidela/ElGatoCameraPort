import { useState, useEffect } from 'react';
import type { VideoFormat } from '../types';

export function useCameraPreview(isPreviewOn: boolean) {
    const [formats, setFormats] = useState<VideoFormat[]>([]);
    const [selectedFormat, setSelectedFormat] = useState<VideoFormat | null>(null);
    const [streamUrl, setStreamUrl] = useState('');
    const [showGrid, setShowGrid] = useState(true);

    // Fetch available formats on mount
    useEffect(() => {
        fetch('http://localhost:5000/api/camera/formats')
            .then(res => res.json())
            .then(data => {
                if (data.success && data.formats.length > 0) {
                    const sorted = data.formats.sort((a: VideoFormat, b: VideoFormat) =>
                        (b.width * b.height) - (a.width * a.height) || b.fps - a.fps
                    );
                    setFormats(sorted);
                    setSelectedFormat(sorted[0]);
                }
            })
            .catch(console.error);
    }, []);

    // Refresh stream URL when Format or Preview State changes
    useEffect(() => {
        let timer: number;
        if (isPreviewOn) {
            if (selectedFormat) {
                setStreamUrl(`http://localhost:5000/api/camera/stream?t=${Date.now()}&w=${selectedFormat.width}&h=${selectedFormat.height}&fps=${selectedFormat.fps}`);
            }
        } else {
            // Give the browser a moment to drop the img src before killing the backend process
            timer = window.setTimeout(() => {
                setStreamUrl('');
                fetch('http://localhost:5000/api/camera/stream/stop', { method: 'POST' }).catch(() => { });
            }, 500);
        }
        return () => {
            if (timer) clearTimeout(timer);
        };
    }, [isPreviewOn, selectedFormat]);

    return {
        formats,
        selectedFormat,
        setSelectedFormat,
        streamUrl,
        showGrid,
        setShowGrid
    };
}
