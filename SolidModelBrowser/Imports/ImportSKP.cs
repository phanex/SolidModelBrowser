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
        static ImportSKP()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                var an = new System.Reflection.AssemblyName(e.Name);
                if (an.Name.Equals("OpenSkp", StringComparison.OrdinalIgnoreCase))
                {
                    var asm = typeof(ImportSKP).Assembly;
                    foreach (var resName in asm.GetManifestResourceNames())
                    {
                        if (resName.EndsWith("OpenSkp.dll", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var stream = asm.GetManifestResourceStream(resName))
                            {
                                if (stream == null) return null;
                                byte[] bytes = new byte[stream.Length];
                                stream.Read(bytes, 0, bytes.Length);
                                return System.Reflection.Assembly.Load(bytes);
                            }
                        }
                    }
                }
                return null;
            };
        }

        public ImportSKP()
        {
            Extensions = new List<string> { "skp" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "skp", new SolidColorBrush(Color.FromRgb(235, 95, 75)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "skp", new SolidColorBrush(Color.FromRgb(180, 45, 30)) } };
            InitialRotationX = 0.0;
        }

        public override void Load(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    ExceptionMessage = $"SKP file not found: {filename}";
                    return;
                }

                var scene = SkpFile.BuildScene(filename);
                if (scene == null || scene.GlbPrimitives == null)
                {
                    ExceptionMessage = "Failed to extract scene from SKP file.";
                    return;
                }

                int totalPrimitives = scene.GlbPrimitives.Count;
                if (totalPrimitives == 0) return;

                for (int p = 0; p < totalPrimitives; p++)
                {
                    if (StopFlag) return;

                    var prim = scene.GlbPrimitives[p];
                    int baseIndex = Positions.Count;

                    // Unpack flat float array (OpenSkp glTF meters -> native SketchUp CAD millimeters)
                    // glTF coordinate frame (X right, Y up, -Z forward) -> SketchUp & SolidModelBrowser (X right, Y forward, Z up)
                    if (prim.Positions != null)
                    {
                        for (int i = 0; i < prim.Positions.Length; i += 3)
                        {
                            float gx = prim.Positions[i] * 1000.0f;
                            float gy = prim.Positions[i + 1] * 1000.0f;
                            float gz = prim.Positions[i + 2] * 1000.0f;

                            double x = gx;
                            double y = -gz;
                            double z = gy;
                            Positions.Add(new Point3D(x, y, z));
                        }
                    }

                    // Unpack flat normal array (NX, NY, NZ triplets transformed matching position mapping)
                    if (prim.Normals != null)
                    {
                        for (int i = 0; i < prim.Normals.Length; i += 3)
                        {
                            float gnx = prim.Normals[i];
                            float gny = prim.Normals[i + 1];
                            float gnz = prim.Normals[i + 2];

                            double nx = gnx;
                            double ny = -gnz;
                            double nz = gny;
                            Normals.Add(new Vector3D(nx, ny, nz));
                        }
                    }

                    // Unpack triangle vertex indices
                    if (prim.Indices != null)
                    {
                        for (int i = 0; i < prim.Indices.Length; i++)
                        {
                            Indices.Add(baseIndex + (int)prim.Indices[i]);
                        }
                    }

                    Progress = (p + 1) * 100 / totalPrimitives;
                }
            }
            catch (Exception ex)
            {
                ExceptionMessage = $"SKP load error: {ex.Message}";
            }
        }
    }
}
