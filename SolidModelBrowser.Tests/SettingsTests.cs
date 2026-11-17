using NUnit.Framework;
using SolidModelBrowser;

namespace SolidModelBrowser.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [Test]
        public void DefaultSettings_FilePanelIsShownByDefaultWhenLaunchedStandalone()
        {
            var settings = new Settings();
            Assert.IsTrue(settings.ShowFilePanel, "File navigation panel should be shown by default when launched standalone.");
        }

        [Test]
        public void DefaultSettings_SelectedPathPointsToValidUserDirectory()
        {
            var settings = new Settings();
            Assert.IsFalse(string.IsNullOrEmpty(settings.SelectedPath));
            Assert.IsTrue(System.IO.Directory.Exists(settings.SelectedPath), "Default SelectedPath should resolve to a valid directory (e.g. 3D Objects or Documents).");
        }

        [Test]
        public void DefaultSettings_FilePanelIsOpaque()
        {
            var settings = new Settings();
            Assert.AreEqual(1.0, settings.FilePanelIdleOpacity, 0.001, "File navigation panel idle opacity must be solid (1.0).");
        }

        [Test]
        public void DefaultSettings_FilePanelWidthIsReasonable()
        {
            var settings = new Settings();
            Assert.IsTrue(settings.FilePanelWidth >= 150, "Default file panel width must be at least 150px.");
        }

        [Test]
        public void DefaultSettings_UnsmoothAfterLoadingIsTrue()
        {
            var settings = new Settings();
            Assert.IsTrue(settings.UnsmoothAfterLoading, "CAD Flat Shading (UnsmoothAfterLoading) must be enabled by default.");
        }

        [Test]
        public void DefaultSettings_CalibratedLightingAndMaterial_AvoidOverexposure()
        {
            var settings = new Settings();
            Assert.AreEqual("#FF9AA2AA", settings.DiffuseColor.ToString(), "DiffuseColor must be studio neutral matte gray to avoid blown-out highlights.");
            Assert.AreEqual("#FF303030", settings.SpecularColor.ToString(), "SpecularColor must be controlled to prevent high-glare surfaces.");
            Assert.AreEqual(40.0, settings.SpecularPower, 0.001);
            Assert.AreEqual("#FF303236", settings.AmbientLightColor.ToString());
            Assert.AreEqual("#FFB8BCC4", settings.DirectionalLight1Color.ToString());
            Assert.AreEqual("#FF68707C", settings.DirectionalLight2Color.ToString());
            Assert.AreEqual("#FF40444C", settings.DirectionalLight3Color.ToString());
        }

        [TestCase("model.stl", true, ExpectedResult = true)]
        [TestCase("model.3mf", true, ExpectedResult = true)]
        [TestCase("model.skp", true, ExpectedResult = true)]
        [TestCase("model.obj", true, ExpectedResult = true)]
        [TestCase("model.ply", true, ExpectedResult = true)]
        [TestCase("model.gcode", true, ExpectedResult = false)]
        [TestCase("model.gco", true, ExpectedResult = false)]
        [TestCase("model.stl", false, ExpectedResult = false)]
        [TestCase("model.gcode", false, ExpectedResult = false)]
        public bool ShouldUnsmoothMesh_GCodeStaysSmoothByDefault_CADIsFlat(string filename, bool unsmoothSetting)
        {
            return Utils.ShouldUnsmoothMesh(filename, unsmoothSetting);
        }
    }
}
