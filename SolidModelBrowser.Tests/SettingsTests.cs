using NUnit.Framework;
using SolidModelBrowser;

namespace SolidModelBrowser.Tests
{
    [TestFixture]
    public class SettingsTests
    {
        [Test]
        public void DefaultSettings_FilePanelIsHiddenByDefault()
        {
            var settings = new Settings();
            Assert.IsFalse(settings.ShowFilePanel, "File navigation panel must be hidden by default for file association workflow.");
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
    }
}
