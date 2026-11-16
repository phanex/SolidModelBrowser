using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using OpenSkp;

namespace SolidModelBrowser
{
    internal class ImportSKP : Import
    {
        public ImportSKP()
        {
            Extensions = new List<string> { "skp" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "skp", new SolidColorBrush(Color.FromRgb(235, 95, 75)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "skp", new SolidColorBrush(Color.FromRgb(180, 45, 30)) } };
            InitialXRotationNeeded = false;
        }

        public override void Load(string filename)
        {
            try
            {
                using (var skp = SkpFile.Open(filename))
                {
                    var scene = skp.BuildScene();
                    if (scene == null || scene.Meshes == null) return;

                    int totalMeshes = scene.Meshes.Count;
                    int currentMesh = 0;
                    int baseIndex = 0;

                    foreach (var mesh in scene.Meshes)
                    {
                        if (StopFlag) return;

                        if (mesh.Vertices != null)
                        {
                            for (int i = 0; i < mesh.Vertices.Count; i++)
                            {
                                var v = mesh.Vertices[i];
                                Positions.Add(new Point3D(v.X, v.Y, v.Z));

                                if (mesh.Normals != null && i < mesh.Normals.Count)
                                {
                                    var n = mesh.Normals[i];
                                    Normals.Add(new Vector3D(n.X, n.Y, n.Z));
                                }
                            }
                        }

                        if (mesh.Indices != null)
                        {
                            for (int i = 0; i < mesh.Indices.Count; i++)
                            {
                                Indices.Add(baseIndex + mesh.Indices[i]);
                            }
                        }

                        baseIndex = Positions.Count;
                        currentMesh++;
                        Progress = totalMeshes > 0 ? (currentMesh * 100 / totalMeshes) : 100;
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionMessage = $"SKP load error: {ex.Message}";
            }
        }
    }
}
