export interface CameraControl {
    id: string;
    label: string;
    min: number;
    max: number;
    step: number;
    defaultValue: number;
    unit?: string;
}

export interface ControlSectionData {
    title: string;
    id: string;
    controls: CameraControl[];
}

export interface VideoFormat {
    codec: string;
    width: number;
    height: number;
    fps: number;
}
