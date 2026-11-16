using NUnit.Framework;
using SolidModelBrowser;
using System.IO;

namespace SolidModelBrowser.Tests
{
    [TestFixture]
    public class ImportSKPTests
    {
        [Test]
        public void ImportSKP_IsRegisteredInImportsList()
        {
            Assert.IsNotNull(Import.Imports);
            var skpImporter = Import.Imports.Find(i => i.Extensions.Contains("skp"));
            Assert.IsNotNull(skpImporter, "ImportSKP must be registered in Import.Imports list.");
        }

        [Test]
        public void ImportSKP_SelectImporterSelectsSKP()
        {
            bool selected = Import.SelectImporter("sample_model.skp");
            Assert.IsTrue(selected, "SelectImporter must identify and select .skp files.");
            Assert.IsNotNull(Import.CurrentImport);
            Assert.IsTrue(Import.CurrentImport.Extensions.Contains("skp"));
        }

        [Test]
        public void ImportSKP_HandlesMissingOrCorruptedFileGracefully()
        {
            Import.SelectImporter("dummy.skp");
            var importer = Import.CurrentImport;
            importer.Initialize();

            // Attempting to load non-existent file should not throw unhandled crash
            importer.Load("non_existent_file.skp");

            Assert.IsNotNull(Import.ExceptionMessage, "ExceptionMessage should capture the failure without crashing.");
        }
    }
}
