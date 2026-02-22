import { useCallback } from 'react';

export function useScreenshot(onStatus: (msg: string) => void) {
    const handleScreenshot = useCallback(() => {
        const img = document.querySelector('.live-stream') as HTMLImageElement | null;
        if (!img) return;

        const canvas = document.createElement('canvas');
        canvas.width = img.naturalWidth || img.width;
        canvas.height = img.naturalHeight || img.height;
        const ctx = canvas.getContext('2d');
        if (ctx) {
            ctx.drawImage(img, 0, 0);
            const link = document.createElement('a');
            link.download = `Facecam_Capture_${new Date().toISOString()}.jpg`;
            link.href = canvas.toDataURL('image/jpeg', 0.95);
            link.click();
            onStatus('Screenshot captured and downloaded');
        }
    }, [onStatus]);

    const openScreenshotFolder = useCallback(() => {
        onStatus('Check your browser Downloads folder');
    }, [onStatus]);

    return { handleScreenshot, openScreenshotFolder };
}
