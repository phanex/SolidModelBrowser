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

        struct RibbonChain
        {
            public bool Active;
            public Vector3D LastEnd;
            public Vector3D LastDirXY;
            public Vector3D LastNormal;
            public int TopIdx, RightIdx, LeftIdx;
            public double LastZ;
        }

        struct PendingSegment
        {
            public bool Active;
            public GPosition Start;
            public GPosition End;
        }

        static bool TryParseFast(string s, int start, int len, out double result)
        {
            result = 0.0;
            if (len <= 0) return false;

            int i = start;
            int end = start + len;
            bool neg = false;
            char first = s[i];
            if (first == '-') { neg = true; i++; }
            else if (first == '+') { i++; }

            if (i >= end) return false;

            long intPart = 0;
            long fracPart = 0;
            double fracDiv = 1.0;
            bool seenDigit = false;
            bool seenDot = false;

            for (; i < end; i++)
            {
                char ch = s[i];
                if (ch >= '0' && ch <= '9')
                {
                    seenDigit = true;
                    int d = ch - '0';
                    if (!seenDot)
                    {
                        if (intPart > 1000000000000L)
                            return double.TryParse(s.Substring(start, len), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
                        intPart = intPart * 10 + d;
                    }
                    else
                    {
                        if (fracDiv < 1e12)
                        {
                            fracPart = fracPart * 10 + d;
                            fracDiv *= 10.0;
                        }
                    }
                }
                else if (ch == '.' && !seenDot)
                {
                    seenDot = true;
                }
                else
                {
                    // Fallback for scientific notation (e.g. 1e-5) or unexpected characters
                    return double.TryParse(s.Substring(start, len), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
                }
            }

            if (!seenDigit) return false;
            result = intPart + (fracPart / fracDiv);
            if (neg) result = -result;
            return true;
        }

        static GPosition parseCmdRange(string line, int start, int end, GPosition pos)
        {
            int i = start;
            while (i < end)
            {
                while (i < end && (line[i] == ' ' || line[i] == '\t')) i++;
                if (i >= end) break;

                int tokStart = i;
                while (i < end && line[i] != ' ' && line[i] != '\t') i++;
                int tokLen = i - tokStart;
                if (tokLen < 2) continue;

                char code = line[tokStart];
                if (code >= 'a' && code <= 'z') code = (char)(code - 32);

                double val;
                if (code == 'X') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.X = val; pos.fx = true; } }
                else if (code == 'Y') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.Y = val; pos.fy = true; } }
                else if (code == 'Z') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.Z = val; pos.fz = true; } }
                else if (code == 'E') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.E = val; pos.fe = true; } }
                else if (code == 'F') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.F = val; pos.ff = true; } }
                else if (code == 'I') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.I = val; pos.fi = true; } }
                else if (code == 'J') { if (TryParseFast(line, tokStart + 1, tokLen - 1, out val)) { pos.J = val; pos.fj = true; } }
            }
            return pos;
        }

        static void RecordLayerProgress(double maxZ, int indexCountBefore, ref bool hasLayerZ, ref double currentLayerZ)
        {
            if (!IsGCodeLayersMonotonic) return;

            if (!hasLayerZ)
            {
                hasLayerZ = true;
                currentLayerZ = maxZ;
            }
            else if (maxZ > currentLayerZ + 1e-4)
            {
                if (indexCountBefore > 0)
                    GCodeLayers.Add(new LayerSlice { MaxZ = currentLayerZ, IndexEnd = indexCountBefore });
                currentLayerZ = maxZ;
            }
            else if (maxZ < currentLayerZ - 1e-4)
            {
                // Non-monotonic Z (e.g. sequential by-object print) -> fall back to standard SliceMesh
                IsGCodeLayersMonotonic = false;
                GCodeLayers.Clear();
            }
        }

        void EmitSegment(GPosition src, GPosition dst, Vector3D offset, ref RibbonChain chain, ref bool hasLayerZ, ref double currentLayerZ)
        {
            double layerH = settings.GCODELayerHeight;
            Vector3D rz = new Vector3D(0.0, 0.0, layerH);
            Vector3D a = new Vector3D(src.X + offset.X, src.Y + offset.Y, src.Z + offset.Z);
            Vector3D b = new Vector3D(dst.X + offset.X, dst.Y + offset.Y, dst.Z + offset.Z);
            var r = b - a;
            // Guard against zero-length or pure vertical moves (in-place extrusion/wipe)
            if (Math.Abs(r.X) < 1e-6 && Math.Abs(r.Y) < 1e-6) return;

            double lenXY = Math.Sqrt(r.X * r.X + r.Y * r.Y);
            if (lenXY < 1e-6) return;

            Vector3D dirXY = new Vector3D(r.X / lenXY, r.Y / lenXY, 0.0);
            Vector3D n = new Vector3D(dirXY.Y, -dirXY.X, 0.0); // matches CrossProduct(r, rz).Normalized()
            double halfWidth = settings.GCODELineWidth * 0.5;
            Vector3D p = n * halfWidth;

            int indexCountBefore = Indices.Count;
            double segMaxZ = Math.Max(a.Z, b.Z) + layerH;
            RecordLayerProgress(segMaxZ, indexCountBefore, ref hasLayerZ, ref currentLayerZ);

            bool canJoin = chain.Active
                           && Math.Abs(a.Z - chain.LastZ) < 1e-4
                           && (a - chain.LastEnd).LengthSquared < 1e-6
                           && Vector3D.DotProduct(chain.LastDirXY, dirXY) >= -0.5;

            if (canJoin)
            {
                Vector3D m = chain.LastNormal + n;
                if (m.LengthSquared > 1e-12)
                {
                    m.Normalize();
                    double cosHalf = Vector3D.DotProduct(m, n);
                    double scale = cosHalf > 0.5 ? (halfWidth / cosHalf) : (halfWidth * 2.0);
                    Vector3D miter = m * scale;

                    Positions[chain.RightIdx] = (Point3D)(a + miter);
                    Positions[chain.LeftIdx] = (Point3D)(a - miter);
                }

                int newTop = Positions.Count;
                Positions.Add((Point3D)(b + rz));
                int newRight = Positions.Count;
                Positions.Add((Point3D)(b + p));
                int newLeft = Positions.Count;
                Positions.Add((Point3D)(b - p));

                Indices.Add(chain.TopIdx);
                Indices.Add(chain.RightIdx);
                Indices.Add(newRight);

                Indices.Add(newRight);
                Indices.Add(newTop);
                Indices.Add(chain.TopIdx);

                Indices.Add(chain.TopIdx);
                Indices.Add(newTop);
                Indices.Add(newLeft);

                Indices.Add(newLeft);
                Indices.Add(chain.LeftIdx);
                Indices.Add(chain.TopIdx);

                chain.LastEnd = b;
                chain.LastDirXY = dirXY;
                chain.LastNormal = n;
                chain.LastZ = b.Z;
                chain.TopIdx = newTop;
                chain.RightIdx = newRight;
                chain.LeftIdx = newLeft;
                return;
            }

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

            chain.Active = true;
            chain.LastEnd = b;
            chain.LastDirXY = dirXY;
            chain.LastNormal = n;
            chain.LastZ = b.Z;
            chain.TopIdx = i + 3;
            chain.RightIdx = i + 2;
            chain.LeftIdx = i + 4;
        }

        void FlushPending(ref PendingSegment pending, Vector3D offset, ref RibbonChain chain, ref bool hasLayerZ, ref double currentLayerZ)
        {
            if (!pending.Active) return;
            EmitSegment(pending.Start, pending.End, offset, ref chain, ref hasLayerZ, ref currentLayerZ);
            pending.Active = false;
        }

        void QueueMove(GPosition src, GPosition dst, Vector3D offset, double decimateTol, double maxMergeLenSq, ref PendingSegment pending, ref RibbonChain chain, ref bool hasLayerZ, ref double currentLayerZ)
        {
            if (!pending.Active)
            {
                pending.Active = true;
                pending.Start = src;
                pending.End = dst;
                return;
            }

            // Can only merge if continuing from pending.End at the same Z
            if (Math.Abs(src.X - pending.End.X) < 1e-5 &&
                Math.Abs(src.Y - pending.End.Y) < 1e-5 &&
                Math.Abs(src.Z - pending.End.Z) < 1e-5 &&
                Math.Abs(dst.Z - pending.Start.Z) < 1e-5)
            {
                double vx = dst.X - pending.Start.X;
                double vy = dst.Y - pending.Start.Y;
                double totalLenSq = vx * vx + vy * vy;

                if (totalLenSq > 1e-12 && totalLenSq < maxMergeLenSq)
                {
                    double px = pending.End.X - pending.Start.X;
                    double py = pending.End.Y - pending.Start.Y;
                    double dot = px * vx + py * vy;
                    if (dot > 0.0)
                    {
                        double cross = Math.Abs(px * vy - py * vx);
                        double dev = cross / Math.Sqrt(totalLenSq);
                        if (dev < decimateTol)
                        {
                            pending.End = dst;
                            return;
                        }
                    }
                }
            }

            EmitSegment(pending.Start, pending.End, offset, ref chain, ref hasLayerZ, ref currentLayerZ);
            pending.Start = src;
            pending.End = dst;
        }

        void QueueArc(GPosition src, GPosition dst, bool clockwise, Vector3D offset, double decimateTol, double maxMergeLenSq, double arcChordStep, double arcAngleStep, int arcMaxSegments, ref PendingSegment pending, ref RibbonChain chain, ref bool hasLayerZ, ref double currentLayerZ)
        {
            if (!dst.fi && !dst.fj)
            {
                QueueMove(src, dst, offset, decimateTol, maxMergeLenSq, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
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
                QueueMove(src, dst, offset, decimateTol, maxMergeLenSq, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
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

            int segments;
            if (arcLen <= arcChordStep && absDa <= arcAngleStep * 1.35)
            {
                segments = 1;
            }
            else
            {
                int byLen = (int)Math.Ceiling(arcLen / arcChordStep);
                int byAngle = (int)Math.Ceiling(absDa / arcAngleStep);
                segments = Math.Max(2, Math.Min(arcMaxSegments, Math.Max(byLen, byAngle)));
            }

            if (segments == 1)
            {
                QueueMove(src, dst, offset, decimateTol, maxMergeLenSq, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
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
                QueueMove(prev, cur, offset, decimateTol, maxMergeLenSq, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
                prev = cur;
            }
        }

        static bool MatchCmd(string line, int start, int len, string upperCmd)
        {
            if (len != upperCmd.Length) return false;
            for (int i = 0; i < len; i++)
            {
                char c = line[start + i];
                if (c >= 'a' && c <= 'z') c = (char)(c - 32);
                if (c != upperCmd[i]) return false;
            }
            return true;
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

            // Enable fast monotonic Z-layer indexing only for single-pass imports (multi-plate calls LoadStream >1 time)
            if (Positions.Count == 0 && GCodeLayers.Count == 0)
                IsGCodeLayersMonotonic = true;
            else
            {
                IsGCodeLayersMonotonic = false;
                GCodeLayers.Clear();
            }

            bool hasLayerZ = false;
            double currentLayerZ = 0.0;
            RibbonChain chain = new RibbonChain();
            PendingSegment pending = new PendingSegment();

            // Adaptive simplification for large G-code files (> 10 MB) to keep WPF 3D responsive
            bool isLarge = totalEstimatedLength > 10_000_000;
            double decimateTol = isLarge ? 0.08 : 0.03;
            double maxMergeLenSq = isLarge ? 16.0 : 4.0;
            double arcChordStep = isLarge ? 2.5 : 1.5;
            double arcAngleStep = isLarge ? (Math.PI / 8.0) : (Math.PI / 12.0);
            int arcMaxSegments = isLarge ? 16 : 32;

            long bytesRead = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                bytesRead += line.Length + 2;
                if (totalEstimatedLength > 0)
                {
                    Progress = (int)Math.Min(100, bytesRead * 100 / totalEstimatedLength);
                }

                int len = line.Length;
                if (len == 0) continue;

                int commentIdx = line.IndexOf(';');
                int codeEnd = commentIdx >= 0 ? commentIdx : len;

                // Parse metadata comments only until all fields are found
                if (commentIdx >= 0 && (printTime == null || filamentGrams == null || filamentLength == null))
                {
                    string comment = line.Substring(commentIdx + 1).Trim();
                    if (comment.Length > 0)
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
                            else if (comment.IndexOf("total estimated time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     comment.IndexOf("estimated printing time", StringComparison.OrdinalIgnoreCase) >= 0 ||
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
                }

                // Zero-allocation command & parameter scan
                int i = 0;
                while (i < codeEnd && (line[i] == ' ' || line[i] == '\t')) i++;
                if (i >= codeEnd)
                {
                    if (StopFlag) return;
                    continue;
                }

                int cmdStart = i;
                while (i < codeEnd && line[i] != ' ' && line[i] != '\t') i++;
                int cmdLen = i - cmdStart;

                if (cmdLen == 2 || cmdLen == 3)
                {
                    char c0 = line[cmdStart];
                    if (c0 >= 'a' && c0 <= 'z') c0 = (char)(c0 - 32);

                    if (c0 == 'G' && cmdLen == 2)
                    {
                        char c1 = line[cmdStart + 1];
                        if (c1 == '0' || c1 == '1' || c1 == '2' || c1 == '3')
                        {
                            var newpos = parseCmdRange(line, i, codeEnd, new GPosition());
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

                            bool isTravelOnly = (c1 == '0');
                            bool isArc = (c1 == '2' || c1 == '3');
                            bool isExtruding;

                            if (isRelativeE)
                            {
                                isExtruding = !isTravelOnly && newpos.fe && newpos.E > 0;
                                if (isExtruding) sumRelativeE += newpos.E;
                                newpos.E = pos.E + (newpos.fe ? newpos.E : 0);
                            }
                            else
                            {
                                if (!newpos.fe) newpos.E = pos.E;
                                if (newpos.E > maxAbsoluteE) maxAbsoluteE = newpos.E;
                                isExtruding = !isTravelOnly && newpos.fe && newpos.E > pos.E;
                            }

                            if (isExtruding && (newpos.fx || newpos.fy || isArc))
                            {
                                if (isArc) QueueArc(pos, newpos, c1 == '2', offset, decimateTol, maxMergeLenSq, arcChordStep, arcAngleStep, arcMaxSegments, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
                                else QueueMove(pos, newpos, offset, decimateTol, maxMergeLenSq, ref pending, ref chain, ref hasLayerZ, ref currentLayerZ);
                            }
                            else if (newpos.fx || newpos.fy || newpos.fz || isTravelOnly || (newpos.fe && !isExtruding))
                            {
                                // Travel, Z-hop, or retract breaks continuous extrusion chain
                                FlushPending(ref pending, offset, ref chain, ref hasLayerZ, ref currentLayerZ);
                                chain.Active = false;
                            }

                            if (!newpos.ff) newpos.F = pos.F;
                            pos = newpos;
                        }
                    }
                    else if (c0 == 'G' && cmdLen == 3)
                    {
                        if (MatchCmd(line, cmdStart, cmdLen, "G90")) absolutePos = true;
                        else if (MatchCmd(line, cmdStart, cmdLen, "G91")) absolutePos = false;
                        else if (MatchCmd(line, cmdStart, cmdLen, "G92"))
                        {
                            FlushPending(ref pending, offset, ref chain, ref hasLayerZ, ref currentLayerZ);
                            chain.Active = false;
                            pos = parseCmdRange(line, i, codeEnd, pos);
                        }
                    }
                    else if (c0 == 'M' && cmdLen == 3)
                    {
                        if (MatchCmd(line, cmdStart, cmdLen, "M82")) isRelativeE = false;
                        else if (MatchCmd(line, cmdStart, cmdLen, "M83")) isRelativeE = true;
                    }
                }

                if (StopFlag) return;
            }

            FlushPending(ref pending, offset, ref chain, ref hasLayerZ, ref currentLayerZ);

            if (IsGCodeLayersMonotonic && hasLayerZ && Indices.Count > 0)
            {
                GCodeLayers.Add(new LayerSlice { MaxZ = currentLayerZ, IndexEnd = Indices.Count });
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
