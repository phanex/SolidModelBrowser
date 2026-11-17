using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class ImportGCODE : Import
    {
        public ImportGCODE()
        {
            Extensions = new List<string> { "gcode" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "gcode", new SolidColorBrush(Color.FromRgb(240, 240, 240)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "gcode", new SolidColorBrush(Color.FromRgb(40, 40, 40)) } };
        }

        struct GPosition
        {
            public double X, Y, Z, E, F;
            public bool fx, fy, fz, fe, ff;
            public void Add(GPosition p)
            {
                X += p.X; Y += p.Y; Z += p.Z; E += p.E; F += p.F;
            }
        }

        char[] separator = { ' ' };

        double parseValue(string value) => double.Parse(value.Substring(1));

        GPosition parseCmd(string[] parts) => parseCmd(parts, new GPosition());
        GPosition parseCmd(string[] parts, GPosition pos)
        {
            //GPosition pos = new GPosition();
            int q = parts.Length;
            for (int c = 1; c < q; c++)
            {
                if (parts[c].StartsWith(";")) c = q;
                else if (parts[c].StartsWith("X")) { pos.X = parseValue(parts[c]); pos.fx = true; }
                else if (parts[c].StartsWith("Y")) { pos.Y = parseValue(parts[c]); pos.fy = true; }
                else if (parts[c].StartsWith("Z")) { pos.Z = parseValue(parts[c]); pos.fz = true; }
                else if (parts[c].StartsWith("E")) { pos.E = parseValue(parts[c]); pos.fe = true; }
                else if (parts[c].StartsWith("F")) { pos.F = parseValue(parts[c]); pos.ff = true; }
            }
            return pos;
        }


        void addLine(GPosition src, GPosition dst)
        {
            Vector3D rz = new Vector3D(0.0, 0.0, settings.GCODELayerHeight);
            Vector3D a = new Vector3D(src.X, src.Y, src.Z);
            Vector3D b = new Vector3D(dst.X, dst.Y, dst.Z);
            var r = b - a;
            if (settings.GCODELineExtensions != 0.0)
            {
                var ext = r;
                ext.Normalize();
                ext *= settings.GCODELineExtensions;
                if ((ext * 2).Length < r.Length)
                {
                    b += ext;
                    a -= ext;
                }
            }
            var p = Vector3D.CrossProduct(r, rz);
            p.Normalize();
            p /= 2;
            p *= settings.GCODELineWidth; // line width

            int i = Positions.Count;

            Positions.Add((Point3D)(a + rz));
            Positions.Add((Point3D)(a + p));
            Positions.Add((Point3D)(b + p));
            Positions.Add((Point3D)(b + rz));
            Positions.Add((Point3D)(b - p));
            Positions.Add((Point3D)(a - p));

            Indices.Add(i);
            Indices.Add(i + 1);
            Indices.Add(i + 2);

            Indices.Add(i + 2);
            Indices.Add(i + 3);
            Indices.Add(i);

            Indices.Add(i);
            Indices.Add(i + 3);
            Indices.Add(i + 4);

            Indices.Add(i + 4);
            Indices.Add(i + 5);
            Indices.Add(i);

            //Indices.Add(i + 1);
            //Indices.Add(i + 5);
            //Indices.Add(i + 4);

            //Indices.Add(i + 4);
            //Indices.Add(i + 2);
            //Indices.Add(i + 1);
        }

        public override void Load(string filename)
        {
            bool absolutePos = true;
            GPosition pos = new GPosition();
            double maxAbsoluteE = 0;
            double sumRelativeE = 0;
            string printTime = null;
            string filamentGrams = null;
            string filamentLength = null;

            var strs = File.ReadAllLines(filename);
            int q = strs.Length;
            for (int c = 0; c < q; c++)
            {
                string raw = strs[c].Trim();
                if (raw.StartsWith(";"))
                {
                    string comment = raw.Substring(1).Trim();

                    // 1. Time detection
                    if (printTime == null)
                    {
                        if (comment.StartsWith("TIME:", StringComparison.OrdinalIgnoreCase))
                        {
                            string secStr = comment.Substring(5).Trim();
                            if (double.TryParse(secStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double secs))
                            {
                                TimeSpan t = TimeSpan.FromSeconds(secs);
                                printTime = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
                            }
                        }
                        else if (comment.IndexOf("estimated printing time", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int eq = comment.IndexOf('=');
                            if (eq > 0) printTime = comment.Substring(eq + 1).Trim();
                        }
                        else if (comment.IndexOf("build time:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int colon = comment.IndexOf(':');
                            if (colon > 0) printTime = comment.Substring(colon + 1).Trim();
                        }
                    }

                    // 2. Filament weight
                    if (filamentGrams == null)
                    {
                        if (comment.IndexOf("filament used [g]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("total filament used [g]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("plastic weight:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int sepIdx = comment.IndexOfAny(new[] { '=', ':' });
                            if (sepIdx > 0)
                            {
                                string val = comment.Substring(sepIdx + 1).Trim().Replace("g", "").Trim();
                                if (double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double g))
                                    filamentGrams = $"{g:0.1} g";
                            }
                        }
                    }

                    // 3. Filament length
                    if (filamentLength == null)
                    {
                        if (comment.IndexOf("filament used [mm]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("filament length:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int sepIdx = comment.IndexOfAny(new[] { '=', ':' });
                            if (sepIdx > 0)
                            {
                                string val = comment.Substring(sepIdx + 1).Trim().Replace("mm", "").Trim();
                                if (double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double mm))
                                    filamentLength = $"{mm / 1000.0:0.1} m";
                            }
                        }
                        else if (comment.IndexOf("filament used [m]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 comment.StartsWith("Filament used:", StringComparison.OrdinalIgnoreCase))
                        {
                            int sepIdx = comment.IndexOfAny(new[] { '=', ':' });
                            if (sepIdx > 0)
                            {
                                string val = comment.Substring(sepIdx + 1).Trim().Replace("m", "").Trim();
                                if (double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double m))
                                    filamentLength = $"{m:0.1} m";
                            }
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(raw))
                {
                    string s = raw.ToUpper();
                    var parts = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        string cmd = parts[0];
                        if (cmd == "G90") absolutePos = true;
                        if (cmd == "G91") absolutePos = false;
                        if (cmd == "G92") pos = parseCmd(parts, pos);
                        if (cmd == "G0" || cmd == "G1")
                        {
                            var newpos = parseCmd(parts);
                            if (absolutePos)
                            {
                                if (!newpos.fx) newpos.X = pos.X;
                                if (!newpos.fy) newpos.Y = pos.Y;
                                if (!newpos.fz) newpos.Z = pos.Z;
                                if (!newpos.fe) newpos.E = pos.E;
                                if (!newpos.ff) newpos.F = pos.F;
                                if (newpos.E > maxAbsoluteE) maxAbsoluteE = newpos.E;
                            }
                            else
                            {
                                if (newpos.fe && newpos.E > 0) sumRelativeE += newpos.E;
                                newpos.Add(pos);
                            }

                            if ((newpos.fx || newpos.fy) && newpos.E > pos.E) addLine(pos, newpos);
                            pos = newpos;
                        }
                    }
                }

                Progress = c * 100 / q;
                if (StopFlag) return;
            }

            if (!string.IsNullOrEmpty(printTime))
                Metadata["PrintTime"] = printTime;

            double totalExtrusionMm = absolutePos ? maxAbsoluteE : sumRelativeE;
            if (filamentGrams != null && filamentLength != null)
                Metadata["Filament"] = $"{filamentGrams} ({filamentLength})";
            else if (filamentGrams != null)
                Metadata["Filament"] = filamentGrams;
            else if (filamentLength != null)
                Metadata["Filament"] = filamentLength;
            else if (totalExtrusionMm > 0)
            {
                double lenM = totalExtrusionMm / 1000.0;
                double volCm3 = Math.PI * 0.875 * 0.875 * totalExtrusionMm / 1000.0;
                double weightG = volCm3 * 1.24;
                Metadata["Filament"] = $"~{weightG:0.1} g ({lenM:0.1} m)";
            }
        }
    }
}
