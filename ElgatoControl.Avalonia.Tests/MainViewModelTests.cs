using Xunit;
using Moq;
using ElgatoControl.Avalonia.ViewModels;
using ElgatoControl.Core.Services;
using ElgatoControl.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ElgatoControl.Avalonia.Tests;

public class MainViewModelTests
{
    [Fact]
    public void LoadLayout_GroupsPanTiltCorrectly()
    {
        // Arrange
        var mockCamera = new Mock<ICameraDevice>();
        var mockStream = new Mock<IStreamService>();

        var layout = new List<ControlSectionData>
        {
            new ControlSectionData("Frame", "frame", new List<CameraControl>
            {
                new CameraControl("zoom", "Zoom", 0, 100, 1, 50),
                new CameraControl("pan", "Pan", -100, 100, 1, 0),
                new CameraControl("tilt", "Tilt", -100, 100, 1, 0),
                new CameraControl("other", "Other", 0, 10, 1, 0)
            })
        };

        mockCamera.Setup(c => c.GetLayout()).Returns(layout);
        mockCamera.Setup(c => c.GetControlValues()).Returns(new Dictionary<string, int>());

        // Act
        var vm = new MainViewModel(mockCamera.Object, mockStream.Object);

        // Assert
        Assert.Single(vm.Sections);
        var section = vm.Sections[0];
        Assert.Equal(3, section.Items.Count); // Zoom, Pair(Pan, Tilt), Other

        Assert.IsType<ControlViewModel>(section.Items[0]); // Zoom
        Assert.IsType<ControlPairViewModel>(section.Items[1]); // Pan/Tilt Pair
        Assert.IsType<ControlViewModel>(section.Items[2]); // Other

        var pair = (ControlPairViewModel)section.Items[1];
        Assert.Equal("pan", pair.Left.Id);
        Assert.Equal("tilt", pair.Right.Id);
    }

    [Fact]
    public void LoadLayout_HandlesMissingTilt()
    {
        // Arrange
        var mockCamera = new Mock<ICameraDevice>();
        var mockStream = new Mock<IStreamService>();

        var layout = new List<ControlSectionData>
        {
            new ControlSectionData("Frame", "frame", new List<CameraControl>
            {
                new CameraControl("pan", "Pan", -100, 100, 1, 0)
                // Tilt is missing
            })
        };

        mockCamera.Setup(c => c.GetLayout()).Returns(layout);
        mockCamera.Setup(c => c.GetControlValues()).Returns(new Dictionary<string, int>());

        // Act
        var vm = new MainViewModel(mockCamera.Object, mockStream.Object);

        // Assert
        Assert.Single(vm.Sections);
        var section = vm.Sections[0];
        Assert.Single(section.Items);
        Assert.IsType<ControlViewModel>(section.Items[0]); // Pan alone
    }
}
