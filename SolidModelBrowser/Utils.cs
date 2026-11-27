using SolidModelBrowser.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shell;

namespace SolidModelBrowser
{
    internal static class Utils
    {
        public static MeshGeometry3D FillMeshFromImport()
        {
            var mesh = new MeshGeometry3D();
            // don't try to optimize; mesh and 3Dcollections are single thread; also dont fill mesh objects directly in mesh cos it is too slow
            var normals = new Vector3DCollection(Import.Normals.Count);
            var positions = new Point3DCollection(Import.Positions.Count);
            var indices = new Int32Collection(Import.Indices.Count);
            foreach (var n in Import.Normals) normals.Add(n);
            foreach (var p in Import.Positions) positions.Add(p);
            foreach (var i in Import.Indices) indices.Add(i);
            normals.Freeze();
            positions.Freeze();
            indices.Freeze();
            mesh.Normals = normals;
            mesh.Positions = positions;
            mesh.TriangleIndices = indices;
            return mesh;
        }

        public static void FillMesh(MeshGeometry3D src, MeshGeometry3D dest)
        {
            dest.Positions = src.Positions;
            dest.Normals = src.Normals;
            dest.TriangleIndices = src.TriangleIndices;
        }

        public static void GetModelCenterAndSize(MeshGeometry3D mesh, out Point3D center, out Point3D massCenter, out Vector3D size)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            double massX = 0.0, massY = 0.0, massZ = 0.0;
            int validCount = 0;
            foreach (var p in mesh.Positions)
            {
                double x = p.X, y = p.Y, z = p.Z;
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) ||
                    double.IsInfinity(x) || double.IsInfinity(y) || double.IsInfinity(z))
                    continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
                massX += x; massY += y; massZ += z;
                validCount++;
            }
            if (minX > maxX) minX = maxX = 0;
            if (minY > maxY) minY = maxY = 0;
            if (minZ > maxZ) minZ = maxZ = 0;

            size = new Vector3D(maxX - minX, maxY - minY, maxZ - minZ);
            center = new Point3D((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
            massCenter = validCount > 0 ? new Point3D(massX / validCount, massY / validCount, massZ / validCount) : center;
        }

        public static Vector3D GetShiftVectorInNormalSurface(Vector3D v, double dx, double dy)
        {
            var ap = Math.Atan2(v.Y, v.X) + Math.PI / 2;
            var v1 = new Vector3D(Math.Cos(ap), Math.Sin(ap), 0);

            var v2 = Vector3D.CrossProduct(v, v1);
            if (v2.Length > 0) v2.Normalize();

            var r = v.Length;
            return (v1 * dx + v2 * dy) * r / 1000;
        }

        public static Vector3D RotateVectorInParallel(Vector3D v, double angle)
        {
            var a = Math.Atan2(v.Y, v.X) + angle;
            var r = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            return new Vector3D(r * Math.Cos(a), r * Math.Sin(a), v.Z);
        }

        public static Vector3D RotateVectorInMeridian(Vector3D v, double angle)
        {
            var qx = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            var am = (Math.Atan2(v.Z, qx) + angle).MinMax(-Math.PI / 2 + 0.01, Math.PI / 2 - 0.01);
            var r = v.Length;
            var ap = Math.Atan2(v.Y, v.X);
            var rp = r * Math.Cos(am);
            return new Vector3D(rp * Math.Cos(ap), rp * Math.Sin(ap), r * Math.Sin(am));
        }

        //public static Transform3D GetRotation(Vector3D r, Point3D center)
        //{
        //    if (r.Length == 0) return null;
        //    var t = new Transform3DGroup();
        //    var tx = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), r.X), center);
        //    var ty = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), r.Y), center);
        //    var tz = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), r.Z), center);
        //    t.Children.Add(tx);
        //    t.Children.Add(ty);
        //    t.Children.Add(tz);
        //    return t;
        //}

        //public static void RotateMesh(MeshGeometry3D mesh)
        //{
        //    var t = new Point3DCollection();
        //    var mp = mesh.Positions;
        //    mesh.Positions = null;
        //    for (int c = 0; c < mp.Count; c++)
        //    {
        //        var p = mp[c];
        //        t.Add(new Point3D(-p.Y, p.X, p.Z));
        //    }
        //    mesh.Positions = t;
        //}

        public static void InvertNormals(MeshGeometry3D mesh)
        {
            if (mesh == null || mesh.Normals == null) return;
            var normals = new Vector3DCollection(mesh.Normals.Count);
            foreach (var normal in mesh.Normals) normals.Add(-normal);
            mesh.Normals = normals;
        }

        public static void InvertVertexOrder(MeshGeometry3D mesh)
        {
            if (mesh == null || mesh.TriangleIndices == null || mesh.TriangleIndices.Count % 3 != 0) return;
            var v = new Int32Collection(mesh.TriangleIndices.Count);
            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                v.Add(mesh.TriangleIndices[i + 1]);
                v.Add(mesh.TriangleIndices[i]);
                v.Add(mesh.TriangleIndices[i + 2]);
            }
            mesh.TriangleIndices = v;
        }

        public static MeshGeometry3D UnsmoothMesh(MeshGeometry3D mesh)
        {
            if (mesh == null || mesh.Positions == null || mesh.TriangleIndices == null) return mesh;
            var r = new MeshGeometry3D();
            var p = new Point3DCollection(mesh.Positions.Count);
            var i = new Int32Collection(mesh.TriangleIndices.Count);
            for (int c = 0; c < mesh.TriangleIndices.Count; c++)
            {
                p.Add(mesh.Positions[mesh.TriangleIndices[c]]);
                i.Add(c);
            }
            r.Positions = p;
            r.TriangleIndices = i;
            //r.Normals.Clear();
            return r;
        }



        public static Vector3D Norm(this Vector3D v) { v.Normalize(); return v; }

        public static MeshGeometry3D ConvertToWireframe(MeshGeometry3D src, double edgeScale)
        {
            if (src == null || src.Positions == null || src.Positions.Count < 3)
                return new MeshGeometry3D();

            var positions = src.Positions;
            var indices = src.TriangleIndices;

            // Calculate model bounding box for adaptive depth bias and line width
            Point3D min = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            Point3D max = new Point3D(double.MinValue, double.MinValue, double.MinValue);
            for (int k = 0; k < positions.Count; k++)
            {
                var pt = positions[k];
                if (pt.X < min.X) min.X = pt.X; if (pt.Y < min.Y) min.Y = pt.Y; if (pt.Z < min.Z) min.Z = pt.Z;
                if (pt.X > max.X) max.X = pt.X; if (pt.Y > max.Y) max.Y = pt.Y; if (pt.Z > max.Z) max.Z = pt.Z;
            }
            double diag = Math.Sqrt((max.X - min.X) * (max.X - min.X) +
                                    (max.Y - min.Y) * (max.Y - min.Y) +
                                    (max.Z - min.Z) * (max.Z - min.Z));
            if (diag < 1e-4) diag = 1.0;

            // Depth bias and half-width for CAD crease edge rendering without Z-fighting
            double biasDist = Math.Max(diag * 0.002, 0.05);
            double halfWidth = Math.Max(diag * 0.0008 * Math.Max(0.5, edgeScale * 50.0), 0.02);

            // Quantize points to robustly detect shared vertices across disconnected face buffers
            var pointMap = new Dictionary<Tuple<long, long, long>, int>();
            var uniquePoints = new List<Point3D>();

            int GetPointId(Point3D p)
            {
                var key = Tuple.Create((long)Math.Round(p.X * 1000.0), (long)Math.Round(p.Y * 1000.0), (long)Math.Round(p.Z * 1000.0));
                if (pointMap.TryGetValue(key, out int id)) return id;
                id = uniquePoints.Count;
                pointMap[key] = id;
                uniquePoints.Add(p);
                return id;
            }

            int numTris;
            bool hasIndices = indices != null && indices.Count >= 3;
            if (hasIndices)
                numTris = indices.Count / 3;
            else
                numTris = positions.Count / 3;

            // Map edges: (v1, v2) -> EdgeRecord
            var edges = new Dictionary<long, EdgeRecord>();

            for (int t = 0; t < numTris; t++)
            {
                Point3D p1, p2, p3;
                if (hasIndices)
                {
                    p1 = positions[indices[t * 3]];
                    p2 = positions[indices[t * 3 + 1]];
                    p3 = positions[indices[t * 3 + 2]];
                }
                else
                {
                    p1 = positions[t * 3];
                    p2 = positions[t * 3 + 1];
                    p3 = positions[t * 3 + 2];
                }

                Vector3D fn = Vector3D.CrossProduct(p2 - p1, p3 - p1);
                if (fn.LengthSquared < 1e-12) continue;
                fn.Normalize();

                int id1 = GetPointId(p1);
                int id2 = GetPointId(p2);
                int id3 = GetPointId(p3);
                if (id1 == id2 || id2 == id3 || id3 == id1) continue;

                AddEdge(edges, id1, id2, fn);
                AddEdge(edges, id2, id3, fn);
                AddEdge(edges, id3, id1, fn);
            }

            var dest = new MeshGeometry3D();
            var outPositions = new Point3DCollection();
            var outIndices = new Int32Collection();
            var outNormals = new Vector3DCollection();
            int curIdx = 0;

            foreach (var kvp in edges)
            {
                var rec = kvp.Value;
                bool isFeatureEdge = false;

                if (rec.Count == 1)
                {
                    // Boundary / silhouette edge: always a feature edge
                    isFeatureEdge = true;
                }
                else if (rec.Count == 2)
                {
                    // Dihedral angle check: cos(15 deg) ~ 0.966
                    double dot = Vector3D.DotProduct(rec.N1, rec.N2);
                    if (dot < 0.966) // Angle > 15 degrees (crease edge)
                    {
                        isFeatureEdge = true;
                    }
                }
                else // Non-manifold
                {
                    isFeatureEdge = true;
                }

                if (!isFeatureEdge) continue;

                Point3D pA = uniquePoints[rec.V1];
                Point3D pB = uniquePoints[rec.V2];

                Vector3D avgN = rec.Count == 2 ? (rec.N1 + rec.N2) : rec.N1;
                if (avgN.LengthSquared > 1e-6) avgN.Normalize();
                else avgN = rec.N1;

                // Push outward along surface normal (depth bias) to eliminate Z-fighting
                Point3D aBiased = pA + avgN * biasDist;
                Point3D bBiased = pB + avgN * biasDist;

                Vector3D edgeVec = bBiased - aBiased;
                double edgeLen = edgeVec.Length;
                if (edgeLen < 1e-6) continue;
                Vector3D dir = edgeVec / edgeLen;

                // Two orthogonal perpendicular vectors to form a 3D tube
                Vector3D u = avgN - Vector3D.DotProduct(avgN, dir) * dir;
                if (u.LengthSquared > 1e-6) u.Normalize();
                else
                {
                    u = Math.Abs(dir.Z) < 0.9 ? Vector3D.CrossProduct(dir, new Vector3D(0, 0, 1)) : Vector3D.CrossProduct(dir, new Vector3D(0, 1, 0));
                    u.Normalize();
                }
                Vector3D v = Vector3D.CrossProduct(dir, u);
                v.Normalize();

                double radius = halfWidth * 0.7071;
                Vector3D o0 = (u + v) * radius;
                Vector3D o1 = (-u + v) * radius;
                Vector3D o2 = (-u - v) * radius;
                Vector3D o3 = (u - v) * radius;

                Point3D a0 = aBiased + o0;
                Point3D a1 = aBiased + o1;
                Point3D a2 = aBiased + o2;
                Point3D a3 = aBiased + o3;

                Point3D b0 = bBiased + o0;
                Point3D b1 = bBiased + o1;
                Point3D b2 = bBiased + o2;
                Point3D b3 = bBiased + o3;

                // 4 rectangular faces forming an opaque 3D tube with volume from all angles
                Point3D[] tubeVerts = { a0, a1, b1, b0, a1, a2, b2, b1, a2, a3, b3, b2, a3, a0, b0, b3 };
                Vector3D[] tubeNorms = { v, v, v, v, -u, -u, -u, -u, -v, -v, -v, -v, u, u, u, u };

                for (int i = 0; i < 16; i++)
                {
                    outPositions.Add(tubeVerts[i]);
                    outNormals.Add(tubeNorms[i]);
                }

                for (int f = 0; f < 4; f++)
                {
                    int baseIdx = curIdx + f * 4;
                    outIndices.Add(baseIdx);
                    outIndices.Add(baseIdx + 1);
                    outIndices.Add(baseIdx + 2);

                    outIndices.Add(baseIdx);
                    outIndices.Add(baseIdx + 2);
                    outIndices.Add(baseIdx + 3);
                }

                curIdx += 16;
            }

            dest.Positions = outPositions;
            dest.TriangleIndices = outIndices;
            dest.Normals = outNormals;
            return dest;
        }

        private class EdgeRecord
        {
            public int V1, V2;
            public Vector3D N1, N2;
            public int Count;
        }

        private static void AddEdge(Dictionary<long, EdgeRecord> edges, int idA, int idB, Vector3D fn)
        {
            int minId = Math.Min(idA, idB);
            int maxId = Math.Max(idA, idB);
            long key = ((long)minId << 32) | (uint)maxId;

            if (!edges.TryGetValue(key, out var rec))
            {
                rec = new EdgeRecord { V1 = minId, V2 = maxId, N1 = fn, Count = 1 };
                edges[key] = rec;
            }
            else
            {
                if (rec.Count == 1) rec.N2 = fn;
                rec.Count++;
            }
        }

        //public static void ConvertToVertexframe(MeshGeometry3D mesh, double vertexScale)
        //{
        //    RecreateUnsmoothed(mesh);
        //    var p = new Point3DCollection(mesh.Positions.Count * 3);
        //    var i = new Int32Collection(mesh.TriangleIndices.Count * 3);
        //    int iq = 0;
        //    for (int c = 0; c < mesh.Positions.Count; c += 3)
        //    {
        //        Point3D p1 = mesh.Positions[c], p2 = mesh.Positions[c + 1], p3 = mesh.Positions[c + 2];
        //        p.Add(p1);
        //        p.Add((Point3D)(p1 + (p2 - p1).Norm() * vertexScale));
        //        p.Add((Point3D)(p1 + (p3 - p1).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);

        //        p.Add(p2);
        //        p.Add((Point3D)(p2 + (p1 - p2).Norm() * vertexScale));
        //        p.Add((Point3D)(p2 + (p3 - p2).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);

        //        p.Add(p3);
        //        p.Add((Point3D)(p3 + (p1 - p3).Norm() * vertexScale));
        //        p.Add((Point3D)(p3 + (p2 - p3).Norm() * vertexScale));
        //        i.Add(iq++);
        //        i.Add(iq++);
        //        i.Add(iq++);
        //    }
        //    mesh.Positions = p;
        //    mesh.TriangleIndices = i;
        //}

        //public static Stopwatch sw;
        public static void SliceMesh(MeshGeometry3D src, MeshGeometry3D dest, double z)
        {
            if (src == null || src.TriangleIndices == null || src.Positions == null) return;
            int q = src.TriangleIndices.Count;
            if (q < 3 || src.Positions.Count == 0)
            {
                dest.TriangleIndices = new Int32Collection();
                return;
            }

            int[] ti = new int[src.TriangleIndices.Count];
            src.TriangleIndices.CopyTo(ti, 0);

            Int32Collection indices;

            // Fast O(log L) slice path for monotonic G-code layer index
            if (Import.IsGCodeLayersMonotonic && Import.GCodeLayers.Count > 0 && q == Import.Indices.Count)
            {
                var layers = Import.GCodeLayers;
                int lo = 0, hi = layers.Count - 1, found = -1;
                while (lo <= hi)
                {
                    int mid = lo + ((hi - lo) >> 1);
                    if (layers[mid].MaxZ <= z)
                    {
                        found = mid;
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }

                int indexEnd = found >= 0 ? Math.Min(q, layers[found].IndexEnd) : 0;
                indices = new Int32Collection(indexEnd);
                for (int c = 0; c < indexEnd; c++)
                    indices.Add(ti[c]);
            }
            else
            {
                Point3D[] pos = new Point3D[src.Positions.Count];
                src.Positions.CopyTo(pos, 0);

                indices = new Int32Collection(q);

                for (int c = 0; c + 2 < q; c += 3)
                {
                    int i1 = ti[c], i2 = ti[c + 1], i3 = ti[c + 2];
                    if (i1 >= 0 && i1 < pos.Length && i2 >= 0 && i2 < pos.Length && i3 >= 0 && i3 < pos.Length)
                    {
                        if (pos[i1].Z <= z && pos[i2].Z <= z && pos[i3].Z <= z)
                        {
                            indices.Add(i1);
                            indices.Add(i2);
                            indices.Add(i3);
                        }
                    }
                }
            }
            
            if (dest.Positions.Count != src.Positions.Count)
                dest.Positions = src.Positions;
            dest.Normals = src.Normals;
            indices.Freeze();
            dest.TriangleIndices = indices;

            //sw.Stop();
        }

        //static Point3DCollection storedMeshPositions;
        //static Int32Collection storedMeshTriangles;
        //static Vector3DCollection storedMeshNormals;
        //public static void StoreMesh(MeshGeometry3D mesh)
        //{
        //    storedMeshPositions = mesh.Positions;
        //    storedMeshTriangles = mesh.TriangleIndices;
        //    storedMeshNormals = mesh.Normals;
        //}

        //public static void RestoreMesh(MeshGeometry3D mesh)
        //{
        //    mesh.Positions = storedMeshPositions;
        //    mesh.TriangleIndices = storedMeshTriangles;
        //    mesh.Normals = storedMeshNormals;
        //}

        public static bool ShouldUnsmoothMesh(string filename, bool unsmoothSetting)
        {
            if (!unsmoothSetting) return false;
            if (string.IsNullOrEmpty(filename)) return true;
            if (filename.EndsWith(".gcode.3mf", StringComparison.OrdinalIgnoreCase)) return false;
            string ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            return ext != "gcode" && ext != "gco";
        }

        public static double MinMax(this double value, double min, double max)
        {
            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }

        public static double SelectInRange(this double value, double[] options, double defvalue)
        {
            if (options.Contains(value)) return value;
            return defvalue;
        }

        public static void RenderElementToPNG(FrameworkElement e, string filename, int dpi)
        {
            double scale = (double)dpi / 96; // scale DPI
            var img = new RenderTargetBitmap((int)(scale * (e.ActualWidth + 1)), (int)(scale * (e.ActualHeight + 1)), dpi, dpi, PixelFormats.Default);
            img.Render(e);

            var encoder = new PngBitmapEncoder();
            BitmapFrame frame = BitmapFrame.Create(img);
            encoder.Frames.Add(frame);
            using (var stream = File.Create(filename)) encoder.Save(stream);
        }

        public static void SaveImagePNG(Viewport3D vp, int dpi)
        {
            var sd = new Microsoft.Win32.SaveFileDialog() { Filter = "PNG Image|*.png" };
            if (sd.ShowDialog().Value) RenderElementToPNG(vp, sd.FileName, dpi);
        }

        public static void RunExternalApp(string path, string args, string fname)
        {
            if (!File.Exists(fname)) { MessageWindow("Select file first"); return; }
            if (string.IsNullOrWhiteSpace(path)) { MessageWindow("No app specified in settings"); return; }
            string formattedArgs = args;
            if (string.IsNullOrWhiteSpace(formattedArgs))
            {
                formattedArgs = $"\"{fname}\"";
            }
            else if (formattedArgs.Contains("%1"))
            {
                formattedArgs = formattedArgs.Replace("%1", fname);
            }
            else if (formattedArgs.Contains("$file$"))
            {
                formattedArgs = formattedArgs.Replace("$file$", fname);
            }
            else
            {
                formattedArgs = $"{formattedArgs} \"{fname}\"";
            }
            try
            {
                System.Diagnostics.Process.Start(path, formattedArgs);
            }
            catch (Exception ex)
            {
                MessageWindow($"Failed to start {path}:\r\n{ex.Message}");
            }
        }

        //public static GeometryModel3D GetAxesModel()
        //{
        //    var model = new GeometryModel3D();
        //    model.Geometry = new MeshGeometry3D() { Positions = new Point3DCollection() { 0, 0, 0,  100 0 0  0 0 1  100 0 1 } }
        //}

        //public static void saveTest(Point3DCollection positions, Vector3DCollection normals, Int32Collection indices)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    foreach (var p in positions) sb.AppendLine($"p x={p.X} y={p.Y} z={p.Z}");
        //    sb.AppendLine();
        //    foreach (var n in normals) sb.AppendLine($"n x={n.X} y={n.Y} z={n.Z}");
        //    sb.AppendLine();
        //    for(int c = 0; c < indices.Count; c += 3) sb.AppendLine($"f {indices[c]} {indices[c + 1]} {indices[c + 2]}");
        //    File.WriteAllText(@"c:\downloads\text.txt", sb.ToString());
        //}

        public static Size GetCurrentScreenSize(System.Windows.Window w)
        {
            var scr = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(w).Handle);
            if (scr.Primary) return new Size(SystemParameters.MaximizedPrimaryScreenWidth, SystemParameters.MaximizedPrimaryScreenHeight);
            return new Size(scr.WorkingArea.Width + 16, scr.WorkingArea.Height + 16);
        }

        public static string GetKeyboardModifiers()
        {
            string CAS = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? "C" : "";
            CAS += Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ? "A" : "";
            CAS += Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? "S" : "";
            return CAS;
        }

        public static void Toggle(this ToggleButton b) => b.IsChecked = !b.IsChecked;

        public static string MessageWindow(string message) => MessageWindow(message, Application.Current.MainWindow, "OK", Orientation.Horizontal);
        public static string MessageWindow(string message, Window owner, string buttons, Orientation buttonsOrientation)
        {
            string result = "";
            Window mb = new Window() { SizeToContent = SizeToContent.WidthAndHeight, WindowStyle = WindowStyle.None, MinWidth = 200, MinHeight = 60, MaxWidth = 800, MaxHeight = 1000, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            if (owner != null && owner.Visibility == Visibility.Visible)
            {
                mb.Owner = owner;
                mb.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                owner.Effect = new BlurEffect() { Radius = 4.0 };
            }
            WindowChrome.SetWindowChrome(mb, new WindowChrome() { CaptionHeight = 0.0 });
            mb.SetResourceReference(Window.BackgroundProperty, "WindowBack");
            mb.SetResourceReference(Window.ForegroundProperty, "WindowFore");
            var spv = new StackPanel() { Margin = new Thickness(16) };
            var lab = new TextBlock() { Text = message, Margin = new Thickness(0, 0, 0, 16) };
            spv.Children.Add(lab);

            var spbtns = new StackPanel() { Orientation = buttonsOrientation, HorizontalAlignment = HorizontalAlignment.Center };
            var btns = buttons.Split(',');
            foreach (var b in btns)
            {
                var btn = new Button() { Content = b, HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(30, 2, 30, 2) };
                btn.Click += (s, e) => { result = b; mb.Close(); };
                spbtns.Children.Add(btn);
            }
            spv.Children.Add(spbtns);
            mb.Content = spv;
            mb.ContentRendered += (s, e) => mb.InvalidateVisual();
            mb.KeyDown += (s, e) => { if (e.Key == Key.Escape) mb.Close(); };
            mb.ShowDialog();
            if (owner != null) owner.Effect = null;
            return result;
        }

        public static void SetWindowStyle(Window w, bool systemStyle, FrameworkElement elementToHide)
        {
            if (systemStyle)
            {
                w.Style = (Style)Application.Current.Resources["SimpleWindowStyle"];
                if (elementToHide != null) elementToHide.Visibility = Visibility.Collapsed;
            }
            else
            {
                w.Style = (Style)Application.Current.Resources["CustomWindowStyle"];
                if (elementToHide != null) elementToHide.Visibility = Visibility.Visible;
            }
        }

        public static Vector3D RotateVector(Vector3D v, double ax, double ay, double az)
        {
            // Convert angles from degrees to radians
            double radX = ax * (Math.PI / 180.0);
            double radY = ay * (Math.PI / 180.0);
            double radZ = az * (Math.PI / 180.0);

            // Rotation around X axis
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double y1 = v.Y * cosX - v.Z * sinX;
            double z1 = v.Y * sinX + v.Z * cosX;

            // Rotation around Y axis
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);
            double x2 = v.X * cosY + z1 * sinY;
            double z2 = -v.X * sinY + z1 * cosY;

            // Rotation around Z axis
            double cosZ = Math.Cos(radZ);
            double sinZ = Math.Sin(radZ);
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;

            return new Vector3D(x3, y3, z2);
        }

        public static Point3D RotatePoint(Point3D p, Point3D center, double ax, double ay, double az)
        {
            double vx = p.X - center.X;
            double vy = p.Y - center.Y;
            double vz = p.Z - center.Z;

            // Convert angles from degrees to radians
            double radX = ax * (Math.PI / 180.0);
            double radY = ay * (Math.PI / 180.0);
            double radZ = az * (Math.PI / 180.0);

            // Rotation around X axis
            double cosX = Math.Cos(radX);
            double sinX = Math.Sin(radX);
            double y1 = vy * cosX - vz * sinX;
            double z1 = vy * sinX + vz * cosX;

            // Rotation around Y axis
            double cosY = Math.Cos(radY);
            double sinY = Math.Sin(radY);
            double x2 = vx * cosY + z1 * sinY;
            double z2 = -vx * sinY + z1 * cosY;

            // Rotation around Z axis
            double cosZ = Math.Cos(radZ);
            double sinZ = Math.Sin(radZ);
            double x3 = x2 * cosZ - y1 * sinZ;
            double y3 = x2 * sinZ + y1 * cosZ;

            return new Point3D(center.X + x3, center.Y + y3, center.Z + z2);
        }

        public static void RotateMesh(MeshGeometry3D mesh, Point3D center, double ax, double ay, double az)
        {
            if (mesh == null || mesh.Positions == null) return;
            var positions = new Point3DCollection(mesh.Positions.Count);
            var normals = new Vector3DCollection(mesh.Normals.Count);
            foreach (var p in mesh.Positions) positions.Add(RotatePoint(p, center, ax, ay, az));
            foreach (var n in mesh.Normals) normals.Add(RotateVector(n, ax, ay, az));
            mesh.Positions = positions;
            mesh.Normals = normals;
        }


        public static Vector3D GenerateNormal(Point3D p1, Point3D p2, Point3D p3)
        {
            return Vector3D.CrossProduct(p2 - p1, p3 - p1).Norm();
        }

        public static void CalculateMeshStats(MeshGeometry3D mesh, out double volumeCm3, out double surfaceAreaCm2)
        {
            volumeCm3 = 0.0;
            surfaceAreaCm2 = 0.0;
            if (mesh == null || mesh.Positions == null || mesh.TriangleIndices == null || mesh.TriangleIndices.Count % 3 != 0)
                return;

            double totalVolMm3 = 0.0;
            double totalAreaMm2 = 0.0;
            var positions = mesh.Positions;
            var indices = mesh.TriangleIndices;
            int count = indices.Count;

            for (int i = 0; i < count; i += 3)
            {
                Point3D p1 = positions[indices[i]];
                Point3D p2 = positions[indices[i + 1]];
                Point3D p3 = positions[indices[i + 2]];

                Vector3D v1 = p2 - p1;
                Vector3D v2 = p3 - p1;
                Vector3D cross = Vector3D.CrossProduct(v1, v2);
                totalAreaMm2 += 0.5 * cross.Length;

                Vector3D p2CrossP3 = Vector3D.CrossProduct((Vector3D)p2, (Vector3D)p3);
                totalVolMm3 += Vector3D.DotProduct((Vector3D)p1, p2CrossP3);
            }

            totalVolMm3 = Math.Abs(totalVolMm3) / 6.0;
            volumeCm3 = totalVolMm3 / 1000.0;
            surfaceAreaCm2 = totalAreaMm2 / 100.0;
        }
    }
}
