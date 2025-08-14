using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAnalytics.RevitPnA
{
    class TrimExtend
    {
        public static void TrimAndExtendAnalyticalElements(Document doc, List<RevitAnalyticalElementInfo> analyticalRevitInfos)
        {
            if (doc == null || analyticalRevitInfos == null || analyticalRevitInfos.Count == 0)
            {
                DebugHandler.Log("No document or analytical elements provided for trim/extend.", DebugHandler.LogLevel.WARNING);
                return;
            }

            // Agrupar por Frame Name
            List<Frame> frames = analyticalRevitInfos
                .Where(info => info.AnalyticalElementType == AnalyticalElementType.Column
                            || info.AnalyticalElementType == AnalyticalElementType.MainBeam
                            || info.AnalyticalElementType == AnalyticalElementType.SecondaryBeam)
                .Where(info => !string.IsNullOrEmpty(info.FrameName))
                .GroupBy(info => info.FrameName)
                .Select(g => new Frame { FrameName = g.Key, Elements = g.ToList() })
                .ToList();

            DebugHandler.Log($"Found {frames.Count} frames for trim/extend.", DebugHandler.LogLevel.INFO);

            foreach (Frame frame in frames)
            {
                List<RevitAnalyticalElementInfo> beams = frame.Elements
                    .Where(e => e.AnalyticalElementType == AnalyticalElementType.MainBeam
                             || e.AnalyticalElementType == AnalyticalElementType.SecondaryBeam)
                    .ToList();

                List<RevitAnalyticalElementInfo> columns = frame.Elements
                    .Where(e => e.AnalyticalElementType == AnalyticalElementType.Column)
                    .ToList();

                DebugHandler.Log($"Processing Frame '{frame.FrameName}': {beams.Count} beams, {columns.Count} columns.", DebugHandler.LogLevel.INFO);

                // 1) Beam ↔ Beam trim/extend
                for (int i = 0; i < beams.Count; i++)
                {
                    for (int j = i + 1; j < beams.Count; j++)
                    {
                        DebugHandler.Log(
                            $"Attempting trim/extend between beams AnalyticalId={beams[i].AnRevitId} and AnalyticalId={beams[j].AnRevitId}.",
                            DebugHandler.LogLevel.DEBUG);
                        TrimOrExtendPair(beams[i], beams[j], doc);
                    }
                }

                // 2) Beam ↔ Column trim/extend (NEW)
                foreach (RevitAnalyticalElementInfo beam in beams)
                {
                    RevitAnalyticalElementInfo closestColumn = FindClosestColumn(beam, columns);
                    if (closestColumn != null)
                    {
                        DebugHandler.Log(
                            $"Attempting beam/column trim: Beam AnalyticalId={beam.AnRevitId}, Column AnalyticalId={closestColumn.AnRevitId}.",
                            DebugHandler.LogLevel.DEBUG);

                        TrimOrExtendBeamWithColumn(beam, closestColumn, doc);
                    }
                    else
                    {
                        DebugHandler.Log($"No column found to trim/extend for beam AnalyticalId={beam.AnRevitId}.", DebugHandler.LogLevel.INFO);
                    }
                }
            }

            // 3) Secondary beams with "_01": re-wire by support mains


            // Recopila todos los beams (main y secondary) de toda la lista
            List<RevitAnalyticalElementInfo> allBeams = analyticalRevitInfos
                .Where(e => e.AnalyticalElementType == AnalyticalElementType.MainBeam
                         || e.AnalyticalElementType == AnalyticalElementType.SecondaryBeam)
                .ToList();

            int secondaryCount = allBeams.Count(e => e.AnalyticalElementType == AnalyticalElementType.SecondaryBeam);
            DebugHandler.Log($"ApplySecondaryBeamSupportRule: About to process {allBeams.Count} beams, {secondaryCount} secondary beams.", DebugHandler.LogLevel.INFO);


            DebugHandler.Log($"Calling ApplySecondaryBeamSupportRule for all beams: {allBeams.Count} total.", DebugHandler.LogLevel.INFO);

            ApplySecondaryBeamSupportRule(doc, allBeams);
        }

        // ---------- Selection helpers ----------

        private static RevitAnalyticalElementInfo FindClosestColumn(RevitAnalyticalElementInfo beam, List<RevitAnalyticalElementInfo> columns)
        {
            if (beam == null || beam.AnalyticalMember == null || columns == null || columns.Count == 0)
            {
                DebugHandler.Log("FindClosestColumn: Invalid beam or empty columns list.", DebugHandler.LogLevel.WARNING);
                return null;
            }

            Line beamLine = beam.AnalyticalMember.GetCurve() as Line;
            if (beamLine == null) return null;

            XYZ beamStart = beamLine.GetEndPoint(0);
            XYZ beamEnd = beamLine.GetEndPoint(1);

            double minDist = double.MaxValue;
            RevitAnalyticalElementInfo closest = null;

            foreach (RevitAnalyticalElementInfo col in columns)
            {
                if (col.AnalyticalMember == null) continue;
                Line colLine = col.AnalyticalMember.GetCurve() as Line;
                if (colLine == null) continue;

                // Distance from the COLUMN LINE to each BEAM endpoint
                double d0 = DistancePointToLine(colLine, beamStart);
                double d1 = DistancePointToLine(colLine, beamEnd);
                double d = Math.Min(d0, d1);

                if (d < minDist)
                {
                    minDist = d;
                    closest = col;
                }
            }

            if (closest != null)
            {
                DebugHandler.Log(
                    $"Closest column to beam AnalyticalId={beam.AnRevitId} is AnalyticalId={closest.AnRevitId} (distance={minDist}).",
                    DebugHandler.LogLevel.DEBUG);
            }
            return closest;
        }

        private static double DistancePointToLine(Line line, XYZ point)
        {
            IntersectionResult ir = line.Project(point);
            if (ir == null) return double.MaxValue;
            return ir.XYZPoint.DistanceTo(point);
        }

        // ---------- Geometry primitives ----------

        private const double TOL = 1e-6;

        /// Ensure the line’s start is the lower-Z endpoint.
        private static Line EnsureStartIsLower(Line line)
        {
            XYZ a0 = line.GetEndPoint(0);
            XYZ a1 = line.GetEndPoint(1);
            if (a0.Z <= a1.Z + TOL) return line;
            return Line.CreateBound(a1, a0);
        }

        /// Symmetrically extend a line to length *factor* (factor >= 1).
        private static Line ExtendLine(Line line, double factor)
        {
            if (factor <= 1.0) return line;
            XYZ p0 = line.GetEndPoint(0);
            XYZ p1 = line.GetEndPoint(1);
            XYZ v = p1 - p0;
            XYZ half = v.Multiply((factor - 1.0) * 0.5);
            return Line.CreateBound(p0 - half, p1 + half);
        }

        /// Extend a line to 'factor' * length while **keeping the end at fixedEndIndex** fixed.
        private static Line ExtendFromFixedEnd(Line line, int fixedEndIndex, double factor)
        {
            if (factor <= 1.0) return line;

            XYZ pFixed = line.GetEndPoint(fixedEndIndex);
            int otherIndex = fixedEndIndex == 0 ? 1 : 0;
            XYZ pOther = line.GetEndPoint(otherIndex);

            XYZ dir = (pOther - pFixed).Normalize();
            double L = pFixed.DistanceTo(pOther);

            XYZ pOtherNew = pFixed + dir.Multiply(factor * L);

            return fixedEndIndex == 0
                ? Line.CreateBound(pFixed, pOtherNew)
                : Line.CreateBound(pOtherNew, pFixed);
        }

        /// Try segment/segment intersection in 3D (coplanar case typical for frames).
        /// Returns true iff intersection lies on both finite segments.
        private static bool TryIntersectSegments(Line l1, Line l2, out XYZ p)
        {
            p = null;

            XYZ p1 = l1.GetEndPoint(0);
            XYZ q1 = l1.GetEndPoint(1);
            XYZ p2 = l2.GetEndPoint(0);
            XYZ q2 = l2.GetEndPoint(1);

            XYZ d1n = (q1 - p1).Normalize();
            XYZ d2n = (q2 - p2).Normalize();
            double L1 = p1.DistanceTo(q1);
            double L2 = p2.DistanceTo(q2);

            XYZ cross = d1n.CrossProduct(d2n);
            double denom = cross.DotProduct(cross);
            if (denom < 1e-12) return false; // parallel

            XYZ p21 = p2 - p1;

            double t1 = (p21.CrossProduct(d2n)).DotProduct(cross) / denom;
            double t2 = (p21.CrossProduct(d1n)).DotProduct(cross) / denom;

            XYZ a = p1 + d1n.Multiply(t1);
            XYZ b = p2 + d2n.Multiply(t2);

            if (a.DistanceTo(b) > 1e-5) return false; // skew

            if (t1 < -TOL || t1 > L1 + TOL) return false;
            if (t2 < -TOL || t2 > L2 + TOL) return false;

            p = (a + b).Multiply(0.5);
            return true;
        }

        /// Try intersection; if segments don’t meet, extend both to 2× and try again.
        private static bool TryIntersectWithOptionalExtension(Line la, Line lb, out XYZ p)
        {
            if (TryIntersectSegments(la, lb, out p)) return true;

            Line la2 = ExtendLine(la, 2.0);
            Line lb2 = ExtendLine(lb, 2.0);
            return TryIntersectSegments(la2, lb2, out p);
        }

        // ---------- Trim routines ----------

        /// Beam ↔ Beam: keep lower end fixed on both, trim/extend upper ends to their intersection.
        private static void TrimOrExtendPair(RevitAnalyticalElementInfo a, RevitAnalyticalElementInfo b, Document doc)
        {
            if (a == null || b == null || a.AnalyticalMember == null || b.AnalyticalMember == null)
            {
                DebugHandler.Log("TrimOrExtendPair: One or both elements are null or missing AnalyticalMember.", DebugHandler.LogLevel.WARNING);
                return;
            }

            Line curveA = a.AnalyticalMember.GetCurve() as Line;
            Line curveB = b.AnalyticalMember.GetCurve() as Line;
            if (curveA == null || curveB == null)
            {
                DebugHandler.Log("TrimOrExtendPair: One or both curves are not straight lines.", DebugHandler.LogLevel.WARNING);
                return;
            }

            curveA = EnsureStartIsLower(curveA);
            curveB = EnsureStartIsLower(curveB);

            XYZ ip;
            if (!TryIntersectWithOptionalExtension(curveA, curveB, out ip))
            {
                DebugHandler.Log($"No intersection (even after 2× extension) for A:{a.AnRevitId} and B:{b.AnRevitId}.", DebugHandler.LogLevel.INFO);
                return;
            }

            Line newA = Line.CreateBound(curveA.GetEndPoint(0), ip);
            Line newB = Line.CreateBound(curveB.GetEndPoint(0), ip);

            a.AnalyticalMember.SetCurve(newA);
            b.AnalyticalMember.SetCurve(newB);

            DebugHandler.Log($"Trimmed/extended A:{a.AnRevitId} and B:{b.AnRevitId} → end at {ip}.", DebugHandler.LogLevel.INFO);
        }

        /// Beam ↔ Column per the specified rules:
        /// - choose beam end nearest to column line; extend beam toward that end to 2× (keep far end fixed),
        /// - if no intersection, extend column upward (keep lower start fixed) to 2×, retry,
        /// - trim: column start→IP, beam far end→IP.
        private static void TrimOrExtendBeamWithColumn(RevitAnalyticalElementInfo beamInfo, RevitAnalyticalElementInfo columnInfo, Document doc)
        {
            if (beamInfo == null || columnInfo == null ||
                beamInfo.AnalyticalMember == null || columnInfo.AnalyticalMember == null)
            {
                DebugHandler.Log("Beam/Column trim: missing analytical members.", DebugHandler.LogLevel.WARNING);
                return;
            }

            Line beamLine = beamInfo.AnalyticalMember.GetCurve() as Line;
            Line colLine = columnInfo.AnalyticalMember.GetCurve() as Line;
            if (beamLine == null || colLine == null)
            {
                DebugHandler.Log("Beam/Column trim: one of the curves is not a straight line.", DebugHandler.LogLevel.WARNING);
                return;
            }

            // Column orientation: start = lower, end = upper
            colLine = EnsureStartIsLower(colLine);

            // Decide which beam endpoint is nearer to the COLUMN LINE
            double d0 = DistancePointToLine(colLine, beamLine.GetEndPoint(0));
            double d1 = DistancePointToLine(colLine, beamLine.GetEndPoint(1));
            int nearIdx = d0 <= d1 ? 0 : 1;
            int farIdx = nearIdx == 0 ? 1 : 0;

            // Extend beam to 2× length toward the NEAR end, keeping FAR end fixed
            Line beamExtended = ExtendFromFixedEnd(beamLine, farIdx, 2.0);

            // Try intersection with current column
            XYZ ip;
            if (!TryIntersectSegments(beamExtended, colLine, out ip))
            {
                // Extend column upward (keep start/lower fixed) to 2×
                Line colExtended = ExtendFromFixedEnd(colLine, 0, 2.0);

                if (!TryIntersectSegments(beamExtended, colExtended, out ip))
                {
                    DebugHandler.Log($"Beam/Column trim: no intersection even after extension. Beam:{beamInfo.AnRevitId}, Column:{columnInfo.AnRevitId}.",
                                     DebugHandler.LogLevel.INFO);
                    return;
                }
            }

            // Apply trims:
            Line newColumn = Line.CreateBound(columnInfo.AnalyticalMember.GetCurve().GetEndPoint(0), ip); // keep lower start
            Line newBeam = Line.CreateBound(beamLine.GetEndPoint(farIdx), ip);                          // keep far end

            columnInfo.AnalyticalMember.SetCurve(newColumn);
            beamInfo.AnalyticalMember.SetCurve(newBeam);

            DebugHandler.Log($"Beam/Column trimmed at {ip}. Beam kept end {farIdx}, column kept lower start.",
                             DebugHandler.LogLevel.INFO);
        }


        /// For secondary beams whose Mark ends with "_01":
        /// - read First/Second support names,
        /// - find the two MAIN beams with those Marks,
        /// - build a new analytical line from the endpoint of main#1 closest to the secondary’s start
        ///   to the endpoint of main#2 closest to the secondary’s end,
        /// - set that as the secondary beam’s analytical curve.
        private static void ApplySecondaryBeamSupportRule(Document doc, List<RevitAnalyticalElementInfo> beamsInFrame)
        {
            if (beamsInFrame == null)
            {
                DebugHandler.Log("ApplySecondaryBeamSupportRule: beamsInFrame is null.", DebugHandler.LogLevel.WARNING);
                return;
            }
            if (beamsInFrame.Count == 0)
            {
                DebugHandler.Log("ApplySecondaryBeamSupportRule: beamsInFrame is empty.", DebugHandler.LogLevel.INFO);
                return;
            }

           

            // Index MAIN beams by Mark (case-insensitive)
            Dictionary<string, RevitAnalyticalElementInfo> mainByMark =
                beamsInFrame
                .Where(b => b != null
                         && b.AnalyticalElementType == AnalyticalElementType.MainBeam
                         && !string.IsNullOrEmpty(b.Mark))
                .GroupBy(b => b.Mark, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);


            int processed = 0;
            int skipped = 0;

            foreach (RevitAnalyticalElementInfo sec in beamsInFrame)
            {
                if (sec == null)
                {
                    skipped++;
                    continue;
                }
                if (sec.AnalyticalMember == null)
                {
                    skipped++;
                    continue;
                }
                if (sec.AnalyticalElementType != AnalyticalElementType.SecondaryBeam)
                {
                    skipped++;
                    continue;
                }

                // Only those with _01 suffix
                if (string.IsNullOrEmpty(sec.Mark) || !sec.Mark.EndsWith("_01", StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    continue;
                }

                Line secLine = sec.AnalyticalMember.GetCurve() as Line;
                if (secLine == null)
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Skipped secondary beam {sec.AnRevitId} (curve is not a line).", DebugHandler.LogLevel.WARNING);
                    skipped++;
                    continue;
                }

                // Read support names from info (preferred), else from parameters
                string firstSupport = GetSupportNameFromInfoOrParams(sec, primary: true);
                string secondSupport = GetSupportNameFromInfoOrParams(sec, primary: false);

                DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId} Mark='{sec.Mark}', FirstSupport='{firstSupport}', SecondSupport='{secondSupport}'.", DebugHandler.LogLevel.DEBUG);

                if (string.IsNullOrWhiteSpace(firstSupport) || string.IsNullOrWhiteSpace(secondSupport))
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: missing support names ('{firstSupport}' / '{secondSupport}').", DebugHandler.LogLevel.INFO);
                    skipped++;
                    continue;
                }

                if (!mainByMark.TryGetValue(firstSupport, out RevitAnalyticalElementInfo main1))
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: main beam '{firstSupport}' not found.", DebugHandler.LogLevel.INFO);
                    skipped++;
                    continue;
                }
                if (!mainByMark.TryGetValue(secondSupport, out RevitAnalyticalElementInfo main2))
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: main beam '{secondSupport}' not found.", DebugHandler.LogLevel.INFO);
                    skipped++;
                    continue;
                }

                if (main1.AnalyticalMember == null || main2.AnalyticalMember == null)
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: one of the mains has no analytical member.", DebugHandler.LogLevel.INFO);
                    skipped++;
                    continue;
                }

                Line m1 = main1.AnalyticalMember.GetCurve() as Line;
                Line m2 = main2.AnalyticalMember.GetCurve() as Line;
                if (m1 == null || m2 == null)
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: one of the main beam curves is not a line.", DebugHandler.LogLevel.WARNING);
                    skipped++;
                    continue;
                }

                // Map: FirstSupport ↔ secondary start (end 0), SecondSupport ↔ secondary end (end 1)
                XYZ secStart = secLine.GetEndPoint(0);
                XYZ secEnd = secLine.GetEndPoint(1);

                XYZ p1 = GetClosestEndpointToPoint(m1, secStart);
                XYZ p2 = GetClosestEndpointToPoint(m2, secEnd);

                if (p1 == null || p2 == null)
                {
                    DebugHandler.Log($"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId}: could not find closest endpoints on main beams.", DebugHandler.LogLevel.WARNING);
                    skipped++;
                    continue;
                }

                Line newLine = Line.CreateBound(p1, p2);
                sec.AnalyticalMember.SetCurve(newLine);

                DebugHandler.Log(
                    $"ApplySecondaryBeamSupportRule: Secondary {sec.AnRevitId} re-wired by supports '{firstSupport}' → '{secondSupport}'.",
                    DebugHandler.LogLevel.INFO);

                processed++;
            }

            DebugHandler.Log($"ApplySecondaryBeamSupportRule: Finished. Processed: {processed}, Skipped: {skipped}.", DebugHandler.LogLevel.INFO);
        }



        private static string GetSupportNameFromInfoOrParams(RevitAnalyticalElementInfo info, bool primary)
        {
            // Prefer strongly-typed properties if your class defines them
            // (Assumes properties FirstSupportName / SecondSupportName exist)
            string name = string.Empty;

            try
            {
                if (primary)
                {
                    name = info.FirstSupportName;
                }
                else
                {
                    name = info.SecondSupportName;
                }
            }
            catch {
                DebugHandler.Log($"Error reading support name from info for {info.AnRevitId}.", DebugHandler.LogLevel.ERROR);
            }

            if (!string.IsNullOrWhiteSpace(name)) return name;

            // Fallback: read from instance parameters on the PHYSICAL element
            Element e = info.Element;
            if (e != null)
            {
                string paramName = primary ? "ITS_First Support Name" : "ITS_Second Support Name";
                Parameter p = e.LookupParameter(paramName);
                if (p != null && p.StorageType == StorageType.String)
                {
                    string v = p.AsString();
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }

            // Last fallback: try analytical member parameter
            Element an = info.AnalyticalMember;
            if (an != null)
            {
                string paramName = primary ? "ITS_First Support Name" : "ITS_Second Support Name";
                Parameter p = an.LookupParameter(paramName);
                if (p != null && p.StorageType == StorageType.String)
                {
                    string v = p.AsString();
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }

            return string.Empty;
        }

        private static XYZ GetClosestEndpointToPoint(Line line, XYZ point)
        {
            if (line == null || point == null) return null;
            XYZ a = line.GetEndPoint(0);
            XYZ b = line.GetEndPoint(1);
            double da = a.DistanceTo(point);
            double db = b.DistanceTo(point);
            return da <= db ? a : b;
        }

    }
}
