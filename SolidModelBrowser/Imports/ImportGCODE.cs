using System;
using System.Collections.Generic;
using System.Globalization;
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
            Extensions = new List<string> { "gcode", "gco" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> {
                { "gcode", new SolidColorBrush(Color.FromRgb(240, 240, 240)) },
                { "gco", new SolidColorBrush(Color.FromRgb(240, 240, 240)) }
            };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> {
                { "gcode", new SolidColorBrush(Color.FromRgb(40, 40, 40)) },
                { "gco", new SolidColorBrush(Color.FromRgb(40, 40, 40)) }
            };
        }

        struct GPosition
        {
            public double X, Y, Z, E, F, I, J;
            public bool fx, fy, fz, fe, ff, fi, fj;
            public void Add(GPosition p)
            {
                X += p.X; Y += p.Y; Z += p.Z; E += p.E; F += p.F;
            }
        }

        static readonly char[] separator = { ' ', '\t' };

        double parseValue(string value)
        {
            if (value.Length > 1 && double.TryParse(value.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double res))
                return res;
            return 0;
        }

        GPosition parseCmd(string[] parts) => parseCmd(parts, new GPosition());
        GPosition parseCmd(string[] parts, GPosition pos)
        {
            int q = parts.Length;
            for (int c = 1; c < q; c++)
            {
                string p = parts[c];
                if (p.StartsWith(";")) break;
                int semi = p.IndexOf(';');
                if (semi >= 0) p = p.Substring(0, semi);
                if (p.Length < 2) continue;

                char code = p[0];
                if (code == 'X') { pos.X = parseValue(p); pos.fx = true; }
                else if (code == 'Y') { pos.Y = parseValue(p); pos.fy = true; }
                else if (code == 'Z') { pos.Z = parseValue(p); pos.fz = true; }
                else if (code == 'E') { pos.E = parseValue(p); pos.fe = true; }
                else if (code == 'F') { pos.F = parseValue(p); pos.ff = true; }
                else if (code == 'I') { pos.I = parseValue(p); pos.fi = true; }
                else if (code == 'J') { pos.J = parseValue(p); pos.fj = true; }
            }
            return pos;
        }

        void addLine(GPosition src, GPosition dst, Vector3D offset)
        {
            Vector3D rz = new Vector3D(0.0, 0.0, settings.GCODELayerHeight);
            Vector3D a = new Vector3D(src.X + offset.X, src.Y + offset.Y, src.Z + offset.Z);
            Vector3D b = new Vector3D(dst.X + offset.X, dst.Y + offset.Y, dst.Z + offset.Z);
            var r = b - a;
            // Guard against zero-length or pure vertical moves (in-place extrusion/wipe)
            if (Math.Abs(r.X) < 1e-6 && Math.Abs(r.Y) < 1e-6) return;

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
            if (p.LengthSquared < 1e-12) return;
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
        }

        void addArc(GPosition src, GPosition dst, bool clockwise, Vector3D offset)
        {
            if (!dst.fi && !dst.fj)
            {
                addLine(src, dst, offset);
                return;
            }

            double cx = src.X + (dst.fi ? dst.I : 0.0);
            double cy = src.Y + (dst.fj ? dst.J : 0.0);
            double dx0 = src.X - cx;
            double dy0 = src.Y - cy;
            double dx1 = dst.X - cx;
            double dy1 = dst.Y - cy;

            double radius = Math.Sqrt(dx0 * dx0 + dy0 * dy0);
            if (radius < 1e-4)
            {
                addLine(src, dst, offset);
                return;
            }

            double a0 = Math.Atan2(dy0, dx0);
            double a1 = Math.Atan2(dy1, dx1);
            double da = a1 - a0;

            if (clockwise)
            {
                if (da >= -1e-6) da -= 2.0 * Math.PI;
            }
            else
            {
                if (da <= 1e-6) da += 2.0 * Math.PI;
            }

            double absDa = Math.Abs(da);
            double arcLen = radius * absDa;

            // Keep tiny arcs as 1 segment; subdivide larger arcs coarsely (~1.5mm / 15 deg, max 32)
            int segments;
            if (arcLen <= 1.5 && absDa <= Math.PI / 9.0)
            {
                segments = 1;
            }
            else
            {
                int byLen = (int)Math.Ceiling(arcLen / 1.5);
                int byAngle = (int)Math.Ceiling(absDa / (Math.PI / 12.0));
                segments = Math.Max(2, Math.Min(32, Math.Max(byLen, byAngle)));
            }

            if (segments == 1)
            {
                addLine(src, dst, offset);
                return;
            }

            GPosition prev = src;
            for (int s = 1; s <= segments; s++)
            {
                double t = (double)s / segments;
                GPosition cur;
                if (s == segments)
                {
                    cur = dst;
                }
                else
                {
                    double ang = a0 + da * t;
                    cur = new GPosition
                    {
                        X = cx + radius * Math.Cos(ang),
                        Y = cy + radius * Math.Sin(ang),
                        Z = src.Z + (dst.Z - src.Z) * t
                    };
                }
                addLine(prev, cur, offset);
                prev = cur;
            }
        }

        public override void Load(string filename)
        {
            using (var stream = File.OpenRead(filename))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                LoadStream(reader, stream.Length);
            }
        }

        public void LoadStream(TextReader reader, long totalEstimatedLength = 0, Vector3D offset = default(Vector3D))
        {
            bool absolutePos = true;
            bool isRelativeE = false;
            GPosition pos = new GPosition();
            double maxAbsoluteE = 0;
            double sumRelativeE = 0;
            string printTime = null;
            string filamentGrams = null;
            string filamentLength = null;

            long bytesRead = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                bytesRead += line.Length + 2;
                if (totalEstimatedLength > 0)
                {
                    Progress = (int)Math.Min(100, bytesRead * 100 / totalEstimatedLength);
                }

                if (string.IsNullOrWhiteSpace(line)) continue;
                string raw = line.Trim();

                int commentIdx = raw.IndexOf(';');
                string comment = commentIdx >= 0 ? raw.Substring(commentIdx + 1).Trim() : null;
                string code = commentIdx >= 0 ? raw.Substring(0, commentIdx).Trim() : raw;

                if (!string.IsNullOrEmpty(comment))
                {
                    // 1. Time detection
                    if (printTime == null)
                    {
                        if (comment.StartsWith("TIME:", StringComparison.OrdinalIgnoreCase))
                        {
                            string secStr = comment.Substring(5).Trim();
                            if (double.TryParse(secStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double secs))
                            {
                                TimeSpan t = TimeSpan.FromSeconds(secs);
                                printTime = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
                            }
                        }
                        else if (comment.IndexOf("total estimated time", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int colon = comment.IndexOfAny(new[] { ':', '=' });
                            if (colon > 0)
                            {
                                string tStr = comment.Substring(colon + 1).Trim();
                                int semi = tStr.IndexOf(';');
                                if (semi > 0) tStr = tStr.Substring(0, semi).Trim();
                                printTime = tStr;
                            }
                        }
                        else if (comment.IndexOf("estimated printing time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 comment.IndexOf("model printing time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 comment.IndexOf("build time:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int colon = comment.IndexOfAny(new[] { ':', '=' });
                            if (colon > 0)
                            {
                                string tStr = comment.Substring(colon + 1).Trim();
                                int semi = tStr.IndexOf(';');
                                if (semi > 0) tStr = tStr.Substring(0, semi).Trim();
                                printTime = tStr;
                            }
                        }
                    }

                    // 2. Filament weight
                    if (filamentGrams == null)
                    {
                        if (comment.IndexOf("filament used [g]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("total filament used [g]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("total filament weight [g]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("plastic weight:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int sepIdx = comment.IndexOfAny(new[] { '=', ':' });
                            if (sepIdx > 0)
                            {
                                string val = comment.Substring(sepIdx + 1).Trim().Replace("g", "").Trim();
                                int semi = val.IndexOf(';');
                                if (semi > 0) val = val.Substring(0, semi).Trim();
                                if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double g))
                                    filamentGrams = $"{g:0.1} g";
                            }
                        }
                    }

                    // 3. Filament length
                    if (filamentLength == null)
                    {
                        if (comment.IndexOf("filament used [mm]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("total filament length [mm]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            comment.IndexOf("filament length:", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int sepIdx = comment.IndexOfAny(new[] { '=', ':' });
                            if (sepIdx > 0)
                            {
                                string val = comment.Substring(sepIdx + 1).Trim().Replace("mm", "").Trim();
                                int semi = val.IndexOf(';');
                                if (semi > 0) val = val.Substring(0, semi).Trim();
                                if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double mm))
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
                                int semi = val.IndexOf(';');
                                if (semi > 0) val = val.Substring(0, semi).Trim();
                                if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double m))
                                    filamentLength = $"{m:0.1} m";
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(code))
                {
                    string s = code.ToUpperInvariant();
                    var parts = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        string cmd = parts[0];
                        if (cmd == "G90") absolutePos = true;
                        else if (cmd == "G91") absolutePos = false;
                        else if (cmd == "M82") isRelativeE = false;
                        else if (cmd == "M83") isRelativeE = true;
                        else if (cmd == "G92")
                        {
                            pos = parseCmd(parts, pos);
                        }
                        else if (cmd == "G0" || cmd == "G1" || cmd == "G2" || cmd == "G3")
                        {
                            var newpos = parseCmd(parts);
                            if (absolutePos)
                            {
                                if (!newpos.fx) newpos.X = pos.X;
                                if (!newpos.fy) newpos.Y = pos.Y;
                                if (!newpos.fz) newpos.Z = pos.Z;
                            }
                            else
                            {
                                newpos.X += pos.X;
                                newpos.Y += pos.Y;
                                newpos.Z += pos.Z;
                            }

                            bool isTravelOnly = (cmd == "G0");
                            bool isArc = (cmd == "G2" || cmd == "G3");

                            if (isRelativeE)
                            {
                                bool isExtruding = !isTravelOnly && newpos.fe && newpos.E > 0;
                                if (isExtruding) sumRelativeE += newpos.E;
                                if ((newpos.fx || newpos.fy || isArc) && isExtruding)
                                {
                                    if (isArc) addArc(pos, newpos, cmd == "G2", offset);
                                    else addLine(pos, newpos, offset);
                                }
                                newpos.E = pos.E + (newpos.fe ? newpos.E : 0);
                            }
                            else
                            {
                                if (!newpos.fe) newpos.E = pos.E;
                                if (newpos.E > maxAbsoluteE) maxAbsoluteE = newpos.E;
                                bool isExtruding = !isTravelOnly && newpos.fe && newpos.E > pos.E;
                                if ((newpos.fx || newpos.fy || isArc) && isExtruding)
                                {
                                    if (isArc) addArc(pos, newpos, cmd == "G2", offset);
                                    else addLine(pos, newpos, offset);
                                }
                            }

                            if (!newpos.ff) newpos.F = pos.F;
                            pos = newpos;
                        }
                    }
                }

                if (StopFlag) return;
            }

            if (!string.IsNullOrEmpty(printTime) && !Metadata.ContainsKey("PrintTime"))
                Metadata["PrintTime"] = printTime;

            double totalExtrusionMm = isRelativeE ? sumRelativeE : maxAbsoluteE;
            if (!Metadata.ContainsKey("Filament"))
            {
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
}
