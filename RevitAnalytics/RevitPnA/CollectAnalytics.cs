using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using RevitAnalytics.Core.Sections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using Material = Autodesk.Revit.DB.Material;

namespace RevitAnalytics.RevitPnA
{
    // Enumeración para los tipos de elementos analíticos
    public enum AnalyticalElementType
    {
        Column,
        MainBeam,
        SecondaryBeam,
        TemporaryBeam, 
        Wall,
        Floor,
        Truss,
        Foundation
    }

    class CollectAnalytics
    {
        public static List<RevitAnalyticalElementInfo> CollectAnalyticalElements(Document doc)
        {
            DebugHandler.Log("Starting analytical element collection.", DebugHandler.LogLevel.INFO);

            // Buscar todas las columnas estructurales
            var columns = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();
            DebugHandler.Log($"Found {columns.Count} structural columns.", DebugHandler.LogLevel.INFO);

            // Buscar todas las vigas estructurales
            // Exclude temporary beams from the main beams list
            // Get all beams (unfiltered)
            var allBeams = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Where(fi => fi.StructuralType == StructuralType.Beam)
                .ToList();

            // Separate normal beams and temporary beams
            var beams = allBeams
                .Where(b => (b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty)
                    .IndexOf("T-B", StringComparison.OrdinalIgnoreCase) < 0)
                .ToList();

            var tempBeams = allBeams
                .Where(b => (b.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty)
                    .IndexOf("T-B", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            DebugHandler.Log($"Found {tempBeams.Count} temporary beams.", DebugHandler.LogLevel.INFO);

            // Buscar todos los muros
            var walls = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();
            DebugHandler.Log($"Found {walls.Count} walls.", DebugHandler.LogLevel.INFO);

            // Buscar todos los suelos
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .Cast<Floor>()
                .ToList();
            DebugHandler.Log($"Found {floors.Count} floors.", DebugHandler.LogLevel.INFO);

            // Buscar todas las cerchas
            var trusses = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralTruss)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();
            DebugHandler.Log($"Found {trusses.Count} trusses.", DebugHandler.LogLevel.INFO);

            // Buscar todas las fundaciones
            var foundations = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();
            DebugHandler.Log($"Found {foundations.Count} foundations.", DebugHandler.LogLevel.INFO);

            // Obtener infos ya en formato RevitAnalyticalElementInfo
            List<RevitAnalyticalElementInfo> columnInfos = GetColumnCurves(columns);
            DebugHandler.Log($"Processed {columnInfos.Count} analytical columns.", DebugHandler.LogLevel.INFO);

            List<RevitAnalyticalElementInfo> beamInfos = GetBeamCurves(beams);
            DebugHandler.Log($"Processed {beamInfos.Count} analytical beams.", DebugHandler.LogLevel.INFO);

            List<RevitAnalyticalElementInfo> tempBeamInfos = GetTemporaryBeamCurves(tempBeams);
            DebugHandler.Log($"Processed {tempBeamInfos.Count} analytical temporary beams.", DebugHandler.LogLevel.INFO);

            // (Opcional) Otros datos geométricos por si los necesitas en paralelo
            var wallFaces = GetWallFaces(walls);
            DebugHandler.Log($"Extracted {wallFaces.Count} wall faces.", DebugHandler.LogLevel.INFO);

            var floorFaces = GetFloorFaces(floors);
            DebugHandler.Log($"Extracted {floorFaces.Count} floor faces.", DebugHandler.LogLevel.INFO);

            var trussCurves = GetTrussCurves(trusses);
            DebugHandler.Log($"Extracted {trussCurves.Count} truss curves.", DebugHandler.LogLevel.INFO);

            var foundationFaces = GetFoundationFaces(foundations);
            DebugHandler.Log($"Extracted {foundationFaces.Count} foundation faces.", DebugHandler.LogLevel.INFO);

            // Unificar resultados en una sola lista para devolver
            var allInfos = new List<RevitAnalyticalElementInfo>(
                (columnInfos?.Count ?? 0) + (beamInfos?.Count ?? 0) + (tempBeamInfos?.Count ?? 0));

            if (columnInfos != null) allInfos.AddRange(columnInfos);
            if (beamInfos != null) allInfos.AddRange(beamInfos);
            if (tempBeamInfos != null) allInfos.AddRange(tempBeamInfos);

            DebugHandler.Log($"Total analytical elements collected: {allInfos.Count}", DebugHandler.LogLevel.INFO);

            // TODO: cuando implementes GetWallInfos/GetFloorInfos/GetTrussInfos/GetFoundationInfos,
            // agrégalos aquí con AddRange para devolverlos también.

            return allInfos;
        }

        private static List<RevitAnalyticalElementInfo> GetColumnCurves(List<FamilyInstance> columns)
        {
            var doc = columns.FirstOrDefault()?.Document;
            var result = new List<RevitAnalyticalElementInfo>();
            DebugHandler.Log($"Starting analytical column processing for {columns.Count} columns.", DebugHandler.LogLevel.INFO);

            foreach (var col in columns)
            {
                var location = col.Location as LocationCurve;
                Curve locationLine = location?.Curve;

                // Obtener el tipo de columna y sus parámetros
                ElementId typeId = col.GetTypeId();
                var colType = doc.GetElement(typeId) as FamilySymbol;

                // Obtener sección y material
                ElementId sectionTypeId = typeId;
                ElementId materialId = ElementId.InvalidElementId;
                Material material = null;
                string sectionName = colType?.Name ?? string.Empty;
                string materialName = string.Empty;

                try
                {
                    materialId = col.StructuralMaterialId;
                    if (materialId != null)
                    {
                        material = doc.GetElement(materialId) as Material;
                        materialName = material?.Name ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    DebugHandler.Log($"Error retrieving material for column {col.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                }

                // Crear el AnalyticalMember
                AnalyticalMember createdAnalytical = null;
                if (locationLine != null)
                {
                    createdAnalytical = AnalyticalMember.Create(doc, locationLine);

                    // Opcional: establecer propiedades si están disponibles
                    try
                    {
                        if (createdAnalytical != null)
                        {
                            if (sectionTypeId != ElementId.InvalidElementId)
                                createdAnalytical.SectionTypeId = sectionTypeId;

                            if (materialId != ElementId.InvalidElementId)
                                createdAnalytical.MaterialId = materialId;

                            createdAnalytical.StructuralRole = AnalyticalStructuralRole.StructuralRoleColumn;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugHandler.Log($"Error setting analytical properties for column {col.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                    }
                }
                else
                {
                    DebugHandler.Log($"Column {col.Id} has no valid location curve.", DebugHandler.LogLevel.WARNING);
                }

                // Obtener el parámetro Mark
                string mark = col.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(mark))
                {
                    DebugHandler.Log($"Column {col.Id} has no Mark parameter.", DebugHandler.LogLevel.WARNING);
                    continue; // Skip columns without a Mark
                }

                // Empaquetar resultados
                var info = new RevitAnalyticalElementInfo
                {
                    PhRevitId = col.Id,
                    Element = col,
                    AnalyticalMember = createdAnalytical,
                    AnRevitId = createdAnalytical?.Id,
                    AnalyticalElementType = AnalyticalElementType.Column,
                    Mark = mark,
                    FamilyType = colType,
                    Material = material,
                    SectionName = sectionName,
                    MaterialName = materialName,
                };

                result.Add(info);
            }

            DebugHandler.Log($"Finished analytical column processing. Created {result.Count} analytical column infos.", DebugHandler.LogLevel.INFO);
            return result;
        }

        private static List<RevitAnalyticalElementInfo> GetTemporaryBeamCurves(List<FamilyInstance> tempBeams)
        {
            var doc = tempBeams.FirstOrDefault()?.Document;
            var result = new List<RevitAnalyticalElementInfo>();
            DebugHandler.Log($"Starting analytical temporary beam processing for {tempBeams.Count} beams.", DebugHandler.LogLevel.INFO);

            foreach (var beam in tempBeams)
            {
                var location = beam.Location as LocationCurve;
                Curve locationLine = location?.Curve;

                // Get type and parameters
                ElementId typeId = beam.GetTypeId();
                var beamType = doc.GetElement(typeId) as FamilySymbol;

                // Material
                ElementId materialId = ElementId.InvalidElementId;
                Material material = null;
                string sectionName = beamType?.Name ?? string.Empty;
                string materialName = string.Empty;

                try
                {
                    materialId = beam.StructuralMaterialId;
                    if (materialId != null)
                    {
                        material = doc.GetElement(materialId) as Material;
                        materialName = material?.Name ?? string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    DebugHandler.Log($"Error retrieving material for temporary beam {beam.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                }

                // Get section height
                double sectionHeight = 0.0;
                var hParam = beamType?.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                if (hParam != null && hParam.StorageType == StorageType.Double)
                    sectionHeight = hParam.AsDouble();

                // Compute downward offset
                XYZ offsetVector = ComputeDownOffsetVector(locationLine, sectionHeight);

                // Translate the line
                Curve translatedLine = locationLine;
                if (locationLine != null && offsetVector != null)
                {
                    var t = Transform.CreateTranslation(offsetVector);
                    translatedLine = locationLine.CreateTransformed(t);
                }

                // Create the AnalyticalMember
                AnalyticalMember createdAnalytical = null;
                if (translatedLine != null)
                {
                    createdAnalytical = AnalyticalMember.Create(doc, translatedLine);

                    try
                    {
                        if (createdAnalytical != null)
                        {
                            if (typeId != ElementId.InvalidElementId)
                                createdAnalytical.SectionTypeId = typeId;

                            if (materialId != ElementId.InvalidElementId)
                                createdAnalytical.MaterialId = materialId;

                            createdAnalytical.StructuralRole = AnalyticalStructuralRole.StructuralRoleBeam;
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugHandler.Log($"Error setting analytical properties for temporary beam {beam.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                    }
                }
                else
                {
                    DebugHandler.Log($"Temporary beam {beam.Id} has no valid location curve.", DebugHandler.LogLevel.WARNING);
                }

                // Get the Mark parameter
                string mark = beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;

                var info = new RevitAnalyticalElementInfo
                {
                    PhRevitId = beam.Id,
                    Element = beam,
                    AnalyticalMember = createdAnalytical,
                    AnRevitId = createdAnalytical?.Id,
                    AnalyticalElementType = AnalyticalElementType.TemporaryBeam,
                    Mark = mark,
                    FamilyType = beamType,
                    Material = material,
                    SectionName = sectionName,
                    MaterialName = materialName,
                };

                result.Add(info);
            }

            DebugHandler.Log($"Finished analytical temporary beam processing. Created {result.Count} analytical temporary beam infos.", DebugHandler.LogLevel.INFO);
            return result;
        }



        private static XYZ ComputeDownOffsetVector(Curve axisCurve, double sectionHeight)
        {
            if (axisCurve == null || sectionHeight <= 0) return null;

            // Axis direction comes from the *axisCurve* you pass in
            XYZ dir = (axisCurve.GetEndPoint(1) - axisCurve.GetEndPoint(0)).Normalize();

            // Move DOWN in the vertical plane of the axis: v = (-Z) - proj_{dir}(-Z)
            XYZ down = -XYZ.BasisZ;
            XYZ v = down - (down.DotProduct(dir)) * dir;

            // Guard (e.g., nearly vertical axis)
            if (v.GetLength() < 1e-9) v = down;

            v = v.Normalize();
            return v.Multiply(sectionHeight / 2.0);
        }

        private static List<RevitAnalyticalElementInfo> GetBeamCurves(List<FamilyInstance> beams)
        {
            var doc = beams.FirstOrDefault()?.Document;
            var result = new List<RevitAnalyticalElementInfo>();
            DebugHandler.Log($"Starting analytical beam processing for {beams.Count} beams.", DebugHandler.LogLevel.INFO);

            // Identify main beams by Mark
            var mainBeams = beams
                .Where(b => ((b.LookupParameter("Mark")?.AsString()) ?? string.Empty)
                    .IndexOf("M-B", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            DebugHandler.Log($"Identified {mainBeams.Count} main beams by Mark.", DebugHandler.LogLevel.INFO);

            var mainByMark = mainBeams
                .GroupBy(b => b.LookupParameter("Mark")?.AsString() ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.First());

            try
            {
                foreach (var beam in beams)
                {
                    string mark = beam.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(mark))
                    {
                        DebugHandler.Log($"Beam {beam.Id} has no Mark parameter.", DebugHandler.LogLevel.WARNING);
                        continue; // Skip beams without a Mark
                    }

                    AnalyticalElementType type = AnalyticalElementType.SecondaryBeam;
                    if (mark.IndexOf("M-B", StringComparison.OrdinalIgnoreCase) >= 0)
                        type = AnalyticalElementType.MainBeam;
                    else if (mark.IndexOf("S-B", StringComparison.OrdinalIgnoreCase) >= 0)
                        type = AnalyticalElementType.SecondaryBeam;

                    // Secondary beam's OWN line (this is the one we'll translate)
                    var locationCurve = beam.Location as LocationCurve;
                    Curve locationLine = locationCurve?.Curve;

                    // Resolve reference beam for SECONDARY beams
                    FamilyInstance referenceBeam = beam;
                    if (type == AnalyticalElementType.SecondaryBeam)
                    {
                        string supportName = beam.LookupParameter("ITS_First Support Name")?.AsString();
                        if (string.IsNullOrWhiteSpace(supportName))
                            supportName = beam.LookupParameter("ITS_Second Support Name")?.AsString();

                        if (!string.IsNullOrWhiteSpace(supportName) && mainByMark.TryGetValue(supportName, out var foundMain))
                            referenceBeam = foundMain;
                    }

                    // Height comes from the *reference* beam
                    double sectionHeight = 0.0;
                    ElementId refTypeId = referenceBeam.GetTypeId();
                    var refType = doc.GetElement(refTypeId);
                    var hParam = refType?.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT);
                    if (hParam != null && hParam.StorageType == StorageType.Double)
                        sectionHeight = hParam.AsDouble();

                    // Material from reference (optional)
                    ElementId materialId = ElementId.InvalidElementId;
                    try
                    {
                        var matParam = refType?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM);
                        if (matParam != null && matParam.StorageType == StorageType.ElementId)
                            materialId = matParam.AsElementId();
                    }
                    catch (Exception ex)
                    {
                        DebugHandler.Log($"Error retrieving material for beam {beam.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                    }

                    // --- KEY CHANGE: choose the axis for the offset vector ---
                    // For MAIN beams → use its own axis (locationLine).
                    // For SECONDARY beams → use the REFERENCE beam's axis.
                    Curve axisForVector = locationLine;
                    if (type == AnalyticalElementType.SecondaryBeam)
                    {
                        var refLoc = referenceBeam.Location as LocationCurve;
                        if (refLoc != null && refLoc.Curve != null)
                        {
                            axisForVector = refLoc.Curve;
                        }
                    }

                    // Compute downward offset in vertical plane of the chosen axis
                    XYZ offsetVector = ComputeDownOffsetVector(axisForVector, sectionHeight);

                    // Translate the *secondary beam's* line by that vector (or main beam’s own line if main)
                    Curve translatedLine = null;
                    if (locationLine != null && offsetVector != null)
                    {
                        var t = Transform.CreateTranslation(offsetVector);
                        translatedLine = locationLine.CreateTransformed(t);
                    }
                    else
                    {
                        translatedLine = locationLine;
                    }

                    // Create analytical member from translated curve
                    AnalyticalMember createdAnalytical = null;
                    if (translatedLine != null)
                    {
                        createdAnalytical = AnalyticalMember.Create(doc, translatedLine);
                        try
                        {
                            if (createdAnalytical != null)
                            {
                                // Keep your current choice here (from the physical beam):
                                createdAnalytical.SectionTypeId = beam.GetTypeId();
                                createdAnalytical.MaterialId = beam.StructuralMaterialId;
                                createdAnalytical.StructuralRole = AnalyticalStructuralRole.StructuralRoleBeam;
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugHandler.Log($"Error setting analytical properties for beam {beam.Id}: {ex.Message}", DebugHandler.LogLevel.WARNING);
                        }
                    }
                    else
                    {
                        DebugHandler.Log($"Beam {beam.Id} has no valid location curve.", DebugHandler.LogLevel.WARNING);
                    }

                    var info = new RevitAnalyticalElementInfo
                    {
                        PhRevitId = beam.Id,
                        Element = beam,
                        AnalyticalMember = createdAnalytical,
                        AnRevitId = createdAnalytical?.Id,
                        AnalyticalElementType = type,
                        Mark = mark,
                        FamilyType = doc.GetElement(beam.GetTypeId()) as FamilySymbol,
                    };

                    result.Add(info);
                }
            }
            catch (Exception ex)
            {
                DebugHandler.Log($"Error processing beams: {ex.Message}", DebugHandler.LogLevel.ERROR);
            }

            DebugHandler.Log($"Finished analytical beam processing. Created {result.Count} analytical beam infos.", DebugHandler.LogLevel.INFO);
            return result;
        }



        private static List<Face> GetWallFaces(List<Wall> walls)
        {
            var faces = new List<Face>();
            DebugHandler.Log($"Extracting faces from {walls.Count} walls.", DebugHandler.LogLevel.INFO);
            foreach (var wall in walls)
            {
                var options = new Options();
                var geometry = wall.get_Geometry(options);
                foreach (var geomObj in geometry)
                {
                    var solid = geomObj as Solid;
                    if (solid != null)
                    {
                        foreach (Face face in solid.Faces)
                            faces.Add(face);
                    }
                }
            }
            DebugHandler.Log($"Extracted {faces.Count} faces from walls.", DebugHandler.LogLevel.INFO);
            return faces;
        }

        private static List<Face> GetFloorFaces(List<Floor> floors)
        {
            var faces = new List<Face>();
            DebugHandler.Log($"Extracting faces from {floors.Count} floors.", DebugHandler.LogLevel.INFO);
            foreach (var floor in floors)
            {
                var options = new Options();
                var geometry = floor.get_Geometry(options);
                foreach (var geomObj in geometry)
                {
                    var solid = geomObj as Solid;
                    if (solid != null)
                    {
                        foreach (Face face in solid.Faces)
                            faces.Add(face);
                    }
                }
            }
            DebugHandler.Log($"Extracted {faces.Count} faces from floors.", DebugHandler.LogLevel.INFO);
            return faces;
        }

        private static List<Curve> GetTrussCurves(List<FamilyInstance> trusses)
        {
            var curves = new List<Curve>();
            DebugHandler.Log($"Extracting curves from {trusses.Count} trusses.", DebugHandler.LogLevel.INFO);
            foreach (var truss in trusses)
            {
                var location = truss.Location as LocationCurve;
                if (location != null)
                    curves.Add(location.Curve);
            }
            DebugHandler.Log($"Extracted {curves.Count} curves from trusses.", DebugHandler.LogLevel.INFO);
            return curves;
        }

        private static List<Face> GetFoundationFaces(List<FamilyInstance> foundations)
        {
            var faces = new List<Face>();
            DebugHandler.Log($"Extracting faces from {foundations.Count} foundations.", DebugHandler.LogLevel.INFO);
            foreach (var foundation in foundations)
            {
                var options = new Options();
                var geometry = foundation.get_Geometry(options);
                foreach (var geomObj in geometry)
                {
                    var solid = geomObj as Solid;
                    if (solid != null)
                    {
                        foreach (Face face in solid.Faces)
                            faces.Add(face);
                    }
                }
            }
            DebugHandler.Log($"Extracted {faces.Count} faces from foundations.", DebugHandler.LogLevel.INFO);
            return faces;
        }
    }
}
