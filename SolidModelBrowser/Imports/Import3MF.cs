using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolidModelBrowser
{
    internal class Import3MF : Import
    {
        public Import3MF()
        {
            Extensions = new List<string> { "3mf" };
            ExtensionsColors = new Dictionary<string, SolidColorBrush> { { "3mf", new SolidColorBrush(Color.FromRgb(255, 150, 150)) } };
            ExtensionsColorsLight = new Dictionary<string, SolidColorBrush> { { "3mf", new SolidColorBrush(Color.FromRgb(200, 50, 50)) } };
            InitialRotationX = 0.0;
        }

        string readZipEntry(ZipArchive zfile, string entryName)
        {
            var zentry = zfile.Entries.First(e => e.FullName.ToLower() == entryName);
            using (var sr = new StreamReader(zentry.Open(), Encoding.UTF8))
                return sr.ReadToEnd();
        }

        string getTag(string s, string tag, bool shorttail, ref int pos)
        {
            string tail = shorttail ? "/>" : $"</{tag}>";
            int ps = s.IndexOf("<" + tag, pos, StringComparison.Ordinal);
            if (ps < 0) return null;
            int pe = s.IndexOf(tail, ps, StringComparison.Ordinal);
            if (pe < 0) return null;
            pos = pe + tail.Length;
            return s.Substring(ps, pe - ps + tail.Length);
        }

        string getTag(string s, string tag, bool shorttail)
        {
            int pos = 0;
            return getTag(s, tag, shorttail, ref pos);
        }

        List<string> getTags(string s, string tag, bool shorttail)
        {
            string tail = shorttail ? "/>" : $"</{tag}>";
            int pos = 0;
            var tags = new List<string>();
            string t;
            while((t = getTag(s, tag, shorttail, ref pos)) != null) tags.Add(t);
            return tags;
        }

        string getParam(string s, string param)
        {
            return Regex.Match(s, $"{param}=\"(.*?)\"", RegexOptions.IgnoreCase).Groups[1].Value;
        }

        Matrix3D buildMatrix(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new Matrix3D();
            var p = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 12) return new Matrix3D();
            var m = new Matrix3D();
            m.M11 = double.Parse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M12 = double.Parse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M13 = double.Parse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M21 = double.Parse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M22 = double.Parse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M23 = double.Parse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M31 = double.Parse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M32 = double.Parse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M33 = double.Parse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.OffsetX = double.Parse(p[9], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.OffsetY = double.Parse(p[10], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.OffsetZ = double.Parse(p[11], NumberStyles.Float, CultureInfo.InvariantCulture);
            m.M44 = 1D;
            return m;
        }

        Vector3D? getVertex(string s, ref int pos)
        {
            Vector3D v = new Vector3D();
            int p1 = s.IndexOf("<vertex", pos, StringComparison.Ordinal);
            if (p1 < 0) return null;
            int p2 = s.IndexOf('"', p1 + 7);
            if (p2 < 0) return null;
            int p3 = s.IndexOf('"', p2 + 1);
            if (p3 < 0) return null;
            v.X = double.Parse(s.Substring(p2 + 1, p3 - p2 - 1), NumberStyles.Float, CultureInfo.InvariantCulture);
            int p4 = s.IndexOf('"', p3 + 1);
            if (p4 < 0) return null;
            int p5 = s.IndexOf('"', p4 + 1);
            if (p5 < 0) return null;
            v.Y = double.Parse(s.Substring(p4 + 1, p5 - p4 - 1), NumberStyles.Float, CultureInfo.InvariantCulture);
            int p6 = s.IndexOf('"', p5 + 1);
            if (p6 < 0) return null;
            int p7 = s.IndexOf('"', p6 + 1);
            if (p7 < 0) return null;
            v.Z = double.Parse(s.Substring(p6 + 1, p7 - p6 - 1), NumberStyles.Float, CultureInfo.InvariantCulture);
            pos = p7 + 1;
            return v;
        }

        struct Triangle { public int v1, v2, v3; }
        Triangle? getTriangle(string s, ref int pos)
        {
            Triangle t = new Triangle();
            int p1 = s.IndexOf("<triangle", pos, StringComparison.Ordinal);
            if (p1 < 0) return null;
            int p2 = s.IndexOf('"', p1 + 7);
            if (p2 < 0) return null;
            int p3 = s.IndexOf('"', p2 + 1);
            if (p3 < 0) return null;
            t.v1 = int.Parse(s.Substring(p2 + 1, p3 - p2 - 1));
            int p4 = s.IndexOf('"', p3 + 1);
            if (p4 < 0) return null;
            int p5 = s.IndexOf('"', p4 + 1);
            if (p5 < 0) return null;
            t.v2 = int.Parse(s.Substring(p4 + 1, p5 - p4 - 1));
            int p6 = s.IndexOf('"', p5 + 1);
            if (p6 < 0) return null;
            int p7 = s.IndexOf('"', p6 + 1);
            if (p7 < 0) return null;
            t.v3 = int.Parse(s.Substring(p6 + 1, p7 - p6 - 1));
            pos = p7 + 1;
            return t;
        }

        int countLines(string s)
        {
            int r = 0;
            //for(int c = 0; c < s.Length; c++) if (s[c] == '\n') ++r;
            foreach(char c in s) if (c == '\n') ++r;
            return r;
        }

        int basepos = 0;
        long pr_len, pr_avglinelen, pr_pos;
        void initProgress(string data)
        {
            if (string.IsNullOrEmpty(data)) { pr_len = 1; pr_avglinelen = 1; pr_pos = 0; return; }
            pr_len = Math.Max(1, data.Length);
            pr_avglinelen = Math.Max(1, data.Length / Math.Max(1, countLines(data)));
            pr_pos = 0;
        }

        void appendProgress(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            pr_len += data.Length;
            pr_avglinelen = Math.Max(1, data.Length / Math.Max(1, countLines(data)));
        }

        void stepProgress()
        {
            pr_pos += pr_avglinelen;
            if (pr_len > 0) Progress = (int)Math.Min(100, pr_pos * 100 / pr_len);
        }

        void parseMesh(string obj, Matrix3D mtr)
        {
            if (string.IsNullOrEmpty(obj)) return;
            int pos = 0;
            Vector3D? v0;
            while ((v0 = getVertex(obj, ref pos)) != null)
            {
                Vector3D v = v0.Value * mtr;
                Positions.Add(new Point3D(v.X + mtr.OffsetX, v.Y + mtr.OffsetY, v.Z + mtr.OffsetZ));

                stepProgress();
                if (StopFlag) return;
            }

            pos = 0;
            Triangle? t;
            while ((t = getTriangle(obj, ref pos)) != null)
            {
                Indices.Add(t.Value.v1 + basepos);
                Indices.Add(t.Value.v2 + basepos);
                Indices.Add(t.Value.v3 + basepos);

                stepProgress();
                if (StopFlag) return;
            }
            basepos = Positions.Count;
        }


        Dictionary<string, List<string>> componentCache = new Dictionary<string, List<string>>();

        void loadComponent(string comp, string model, ZipArchive zfile, Matrix3D basemtr)
        {
            string id = getParam(comp, "objectid");
            string path = getParam(comp, "p:path").TrimStart('/').ToLower();
            string tr = getParam(comp, "transform");
            Matrix3D mtr = buildMatrix(tr) * basemtr;

            string resources;
            List<string> objects;
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (componentCache.ContainsKey(path)) objects = componentCache[path];
                else
                {
                    model = readZipEntry(zfile, path);
                    resources = getTag(model, "resources", false);
                    objects = getTags(resources, "object", false);
                    componentCache[path] = objects;
                }
            }
            else // if no path specified use local model
            {
                resources = getTag(model, "resources", false);
                objects = getTags(resources, "object", false);
            }

            string obj = objects.Where(o => o.Contains($" id=\"{id}\"")).FirstOrDefault();

            appendProgress(obj);

            parseMesh(obj, mtr);
        }

        void ExtractModelMetadata(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return;
            var matches = Regex.Matches(xml, @"<metadata\s+name=[""']([^""']+)[""']>([\s\S]*?)<\/metadata>", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                string name = m.Groups[1].Value;
                string val = WebUtility.HtmlDecode(m.Groups[2].Value.Trim());
                if (string.IsNullOrWhiteSpace(val) || val == "[]") continue;

                if (name.Equals("Title", StringComparison.OrdinalIgnoreCase))
                    Metadata["Title"] = val;
                else if (name.Equals("Designer", StringComparison.OrdinalIgnoreCase) || name.Equals("ProfileUserName", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Metadata.ContainsKey("Designer")) Metadata["Designer"] = val;
                }
                else if (name.Equals("License", StringComparison.OrdinalIgnoreCase))
                    Metadata["License"] = val;
            }
        }

        void ExtractSliceInfo(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return;

            double totalSecs = 0;
            foreach (Match m in Regex.Matches(xml, @"key=[""']prediction[""']\s+value=[""'](\d+)[""']", RegexOptions.IgnoreCase))
            {
                if (double.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double secs))
                    totalSecs += secs;
            }
            if (totalSecs > 0)
            {
                TimeSpan t = TimeSpan.FromSeconds(totalSecs);
                Metadata["PrintTime"] = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
            }

            double totalWeight = 0;
            double totalLength = 0;
            string filamentType = null;
            foreach (Match fm in Regex.Matches(xml, @"<filament\b([^>]*)>", RegexOptions.IgnoreCase))
            {
                string attrs = fm.Groups[1].Value;
                var wMatch = Regex.Match(attrs, @"used_g=[""']([\d\.]+)[""']", RegexOptions.IgnoreCase);
                var lMatch = Regex.Match(attrs, @"used_m=[""']([\d\.]+)[""']", RegexOptions.IgnoreCase);
                var tMatch = Regex.Match(attrs, @"type=[""']([^""']+)[""']", RegexOptions.IgnoreCase);

                if (wMatch.Success && double.TryParse(wMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double wg))
                    totalWeight += wg;
                if (lMatch.Success && double.TryParse(lMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double lm))
                    totalLength += lm;
                if (filamentType == null && tMatch.Success)
                    filamentType = tMatch.Groups[1].Value;
            }

            if (totalWeight > 0 && totalLength > 0)
            {
                string typeStr = !string.IsNullOrEmpty(filamentType) ? $"{filamentType} " : "";
                Metadata["Filament"] = $"{typeStr}{totalWeight:0.##} g ({totalLength:0.##} m)";
            }
        }

        Dictionary<string, Queue<int>> ExtractPlateMapping(string xml, out int plateCount)
        {
            var map = new Dictionary<string, Queue<int>>(StringComparer.OrdinalIgnoreCase);
            plateCount = 0;
            if (string.IsNullOrEmpty(xml)) return map;

            var plateMatches = Regex.Matches(xml, @"<plate>([\s\S]*?)<\/plate>", RegexOptions.IgnoreCase);
            foreach (Match pm in plateMatches)
            {
                string block = pm.Groups[1].Value;
                int plateIdx = plateCount;
                plateCount++;

                var instMatches = Regex.Matches(block, @"<model_instance>([\s\S]*?)<\/model_instance>", RegexOptions.IgnoreCase);
                foreach (Match im in instMatches)
                {
                    var objIdMatch = Regex.Match(im.Groups[1].Value, @"key=[""']object_id[""']\s+value=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
                    if (objIdMatch.Success)
                    {
                        string objId = objIdMatch.Groups[1].Value;
                        if (!map.ContainsKey(objId))
                            map[objId] = new Queue<int>();
                        map[objId].Enqueue(plateIdx);
                    }
                }
            }
            return map;
        }

        static void GetPositionsXRange(int startIdx, int endIdx, out double minX, out double maxX)
        {
            minX = double.MaxValue;
            maxX = double.MinValue;
            for (int i = startIdx; i < endIdx; i++)
            {
                double x = Positions[i].X;
                if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
            }
        }

        static void ShiftPositionsX(int startIdx, int endIdx, double dx)
        {
            if (Math.Abs(dx) < 1e-6) return;
            for (int i = startIdx; i < endIdx; i++)
            {
                var p = Positions[i];
                Positions[i] = new Point3D(p.X + dx, p.Y, p.Z);
            }
        }

        public override void Load(string filename)
        {
            componentCache.Clear();
            basepos = 0;
            string model = null;
            using (var zfile = ZipFile.OpenRead(filename))
            {
                // Collect all G-code entries (plate_1.gcode, plate_2.gcode, etc. ordered numerically)
                var gcodeEntries = zfile.Entries
                    .Where(e => e.FullName.EndsWith(".gcode", StringComparison.OrdinalIgnoreCase))
                    .Select(e =>
                    {
                        var m = Regex.Match(e.FullName, @"plate_(\d+)\.gcode$", RegexOptions.IgnoreCase);
                        int idx = m.Success && int.TryParse(m.Groups[1].Value, out int n) ? n : int.MaxValue;
                        return new { Entry = e, Index = idx };
                    })
                    .OrderBy(x => x.Index)
                    .ThenBy(x => x.Entry.FullName, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Entry)
                    .ToList();

                // Read 3dmodel.model if present for metadata
                var modelEntry = zfile.Entries.FirstOrDefault(e => e.FullName.Equals("3D/3dmodel.model", StringComparison.OrdinalIgnoreCase));
                if (modelEntry != null)
                {
                    using (var sr = new StreamReader(modelEntry.Open(), Encoding.UTF8))
                        model = sr.ReadToEnd();
                    ExtractModelMetadata(model);
                }

                // Also check Metadata/slice_info.config if present
                var sliceInfoEntry = zfile.Entries.FirstOrDefault(e => e.FullName.Equals("Metadata/slice_info.config", StringComparison.OrdinalIgnoreCase));
                if (sliceInfoEntry != null)
                {
                    using (var sr = new StreamReader(sliceInfoEntry.Open(), Encoding.UTF8))
                        ExtractSliceInfo(sr.ReadToEnd());
                }

                string resources = model != null ? getTag(model, "resources", false) : null;
                var objects = resources != null ? getTags(resources, "object", false) : new List<string>();

                // If G-code is present and (no 3D mesh objects in model or explicitly .gcode.3mf), load all G-code plates side-by-side
                if (gcodeEntries.Count > 0 && (objects.Count == 0 || filename.EndsWith(".gcode.3mf", StringComparison.OrdinalIgnoreCase)))
                {
                    if (gcodeEntries.Count > 1)
                        Metadata["Plates"] = $"{gcodeEntries.Count}";

                    double nextMinX = 0;
                    bool hasPrevPlate = false;

                    foreach (var entry in gcodeEntries)
                    {
                        int startPos = Positions.Count;
                        var gcodeImporter = new ImportGCODE();
                        using (var stream = entry.Open())
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            gcodeImporter.LoadStream(reader, entry.Length);
                        }
                        if (StopFlag) return;

                        int endPos = Positions.Count;
                        if (endPos > startPos)
                        {
                            GetPositionsXRange(startPos, endPos, out double minX, out double maxX);
                            if (minX <= maxX)
                            {
                                if (hasPrevPlate)
                                {
                                    double dx = nextMinX - minX;
                                    ShiftPositionsX(startPos, endPos, dx);
                                    maxX += dx;
                                }
                                nextMinX = maxX + 40.0;
                                hasPrevPlate = true;
                            }
                        }
                    }
                    return;
                }

                if (model != null)
                {
                    // Check Metadata/model_settings.config for multi-plate layout
                    int plateCount = 0;
                    Dictionary<string, Queue<int>> plateMap = null;
                    var modelSettingsEntry = zfile.Entries.FirstOrDefault(e => e.FullName.Equals("Metadata/model_settings.config", StringComparison.OrdinalIgnoreCase));
                    if (modelSettingsEntry != null)
                    {
                        using (var sr = new StreamReader(modelSettingsEntry.Open(), Encoding.UTF8))
                            plateMap = ExtractPlateMapping(sr.ReadToEnd(), out plateCount);
                    }

                    string build = getTag(model, "build", false);
                    var items = build != null ? getTags(build, "item", true) : new List<string>();

                    initProgress(model);

                    var plateVertexRanges = new Dictionary<int, List<Tuple<int, int>>>();
                    var fallbackPlateByObj = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    foreach (string item in items)
                    {
                        string id = getParam(item, "objectid");
                        string tr = getParam(item, "transform");
                        Matrix3D mtr = buildMatrix(tr);

                        int plateIdx = 0;
                        if (plateMap != null && plateMap.TryGetValue(id, out var q) && q.Count > 0)
                        {
                            plateIdx = q.Dequeue();
                            fallbackPlateByObj[id] = plateIdx;
                        }
                        else if (fallbackPlateByObj.TryGetValue(id, out int lastPlate))
                        {
                            plateIdx = lastPlate;
                        }

                        string obj = objects.Where(o => o.Contains($" id=\"{id}\"")).FirstOrDefault();
                        if (string.IsNullOrEmpty(obj)) continue;

                        int startPos = Positions.Count;

                        string components = getTag(obj, "components", false);
                        if (components != null)
                        {
                            var comps = getTags(components, "component", true);
                            foreach (var comp in comps) loadComponent(comp, model, zfile, mtr);
                        }

                        parseMesh(obj, mtr);
                        if (StopFlag) return;

                        int endPos = Positions.Count;
                        if (endPos > startPos)
                        {
                            if (!plateVertexRanges.ContainsKey(plateIdx))
                                plateVertexRanges[plateIdx] = new List<Tuple<int, int>>();
                            plateVertexRanges[plateIdx].Add(Tuple.Create(startPos, endPos));
                        }
                    }

                    // Shift subsequent plates along +X so multi-plate 3MFs render side-by-side instead of overlapping
                    if (plateVertexRanges.Count > 1)
                    {
                        Metadata["Plates"] = $"{plateVertexRanges.Count}";
                        double nextMinX = 0;
                        bool hasPrevPlate = false;

                        foreach (int pIdx in plateVertexRanges.Keys.OrderBy(k => k))
                        {
                            double plateMinX = double.MaxValue;
                            double plateMaxX = double.MinValue;
                            foreach (var range in plateVertexRanges[pIdx])
                            {
                                GetPositionsXRange(range.Item1, range.Item2, out double rMinX, out double rMaxX);
                                if (rMinX < plateMinX) plateMinX = rMinX;
                                if (rMaxX > plateMaxX) plateMaxX = rMaxX;
                            }

                            if (plateMinX <= plateMaxX)
                            {
                                if (hasPrevPlate)
                                {
                                    double dx = nextMinX - plateMinX;
                                    foreach (var range in plateVertexRanges[pIdx])
                                        ShiftPositionsX(range.Item1, range.Item2, dx);
                                    plateMaxX += dx;
                                }
                                nextMinX = plateMaxX + 40.0;
                                hasPrevPlate = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
