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

        [Test]
        public void ImportSKP_InitialRotationX_IsPositive90Degrees()
        {
            Import.SelectImporter("test.skp");
            Assert.IsNotNull(Import.CurrentImport);
            Assert.AreEqual(90.0, Import.CurrentImport.InitialRotationX, 0.001, "SKP models must rotate +90 deg around X to align Y-up with Z-up.");
        }

        [Test]
        public void ImportOBJ_InitialRotationX_IsNegative90Degrees()
        {
            Import.SelectImporter("test.obj");
            Assert.IsNotNull(Import.CurrentImport);
            Assert.AreEqual(-90.0, Import.CurrentImport.InitialRotationX, 0.001, "OBJ models with -Y height must rotate -90 deg around X to align with Z-up.");
        }

        [Test]
        public void ImportSKP_ScalesMetersToMillimeters()
        {
            string testFile = @"C:\Desmond\Documents\3D-Print\autoramka\autoramka_test.skp";
            if (!File.Exists(testFile)) return;

            Import.SelectImporter(testFile);
            var importer = Import.CurrentImport;
            importer.Initialize();
            importer.Load(testFile);

            Assert.IsTrue(Import.Positions.Count > 0, "Loaded SKP should have vertices.");
            double minZ = double.MaxValue, maxZ = double.MinValue;
            foreach (var pos in Import.Positions)
            {
                if (pos.Z < minZ) minZ = pos.Z;
                if (pos.Z > maxZ) maxZ = pos.Z;
            }
            double sizeZ = maxZ - minZ;
            Assert.IsTrue(sizeZ > 10.0, $"SKP model dimensions must be in millimeters (got {sizeZ} mm, expected > 10 mm).");
        }
    }
}
