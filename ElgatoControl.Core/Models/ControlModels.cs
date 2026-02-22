using System.Collections.Generic;

namespace ElgatoControl.Core.Models;

public record CameraControl(
    string Id,
    string Label,
    int Min,
    int Max,
    int Step,
    int DefaultValue,
    string? Unit = null
);

public record ControlSectionData(
    string Title,
    string Id,
    IEnumerable<CameraControl> Controls
);
