using Gssoft.Gscad.ApplicationServices.Core;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public sealed class MeasurementService
{
    private static readonly Lazy<MeasurementService> _lazy =
        new Lazy<MeasurementService>(() => new MeasurementService());
    public static MeasurementService Instance => _lazy.Value;

    private MeasurementService() { }

    public Point3d? LastPointWcs { get; private set; }
    public ObjectId? LastEntityId { get; private set; }
    public readonly List<Point3d> AllPointsWcs = new List<Point3d>();

    public event EventHandler<Point3d> MeasurementAdded;

    public bool StartMeasurement()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return false;

        var ed = doc.Editor;

        var ppo = new PromptPointOptions("\nWskaż punkt pomiaru:") { AllowNone = false };
        var ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return false;

        var ucs = ed.CurrentUserCoordinateSystem;
        var pickedUcs = ppr.Value;
        var pickedWcs = pickedUcs.TransformBy(ucs);

        var dwgPath = ResolvePluginPath("./DWGBlocks/TotalStation.dwg");
        var blockName = "Total";

        ObjectId blockDefId;
        try
        {
            blockDefId = ImportBlock(doc.Database, dwgPath, blockName);
        }
        catch (System.Exception ex)
        {
            ed.WriteMessage($"\nNie udało się wczytać bloku '{blockName}' z '{dwgPath}': {ex.Message}");
            return false;
        }

        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var br = new BlockReference(pickedWcs, blockDefId);

            br.ScaleFactors = new Scale3d(1.0);

            if (br.ScaleFactors.X < 0.2 || br.ScaleFactors.Y < 0.2 || br.ScaleFactors.Z < 0.2)
            {
                br.ScaleFactors = new Scale3d(0.02);
            }

            var entId = btr.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);

            tr.Commit();

            LastPointWcs = pickedWcs;
            LastEntityId = entId;
            AllPointsWcs.Add(pickedWcs);
        }

        MeasurementAdded?.Invoke(this, pickedWcs);
        ed.WriteMessage($"\nWstawiono blok '{blockName}' w {Format3d(pickedWcs)}");

        return true;
    }

    private static string Format3d(Point3d p) => $"Tachimetr został ustawiony na X={p.X:0.###}, Y={p.Y:0.###}, Z={p.Z:0.###}";


    private static ObjectId ImportBlock(Database destDb, string dwgPath, string blockName)
    {
        using (var sourceDb = new Database(false, true))
        {
            sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");

            var idMap = new IdMapping();
            var ids = new ObjectIdCollection();

            using (var tr = sourceDb.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                    throw new System.Exception($"Block '{blockName}' not found in {dwgPath}");

                ids.Add(bt[blockName]);
            }

            destDb.WblockCloneObjects(
                ids,
                destDb.BlockTableId,
                idMap,
                DuplicateRecordCloning.Ignore,
                false
            );
        }

        using (var tr = destDb.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(destDb.BlockTableId, OpenMode.ForRead);
            return bt[blockName];
        }
    }

    static string PluginDir()
    {
        var asmPath = Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(asmPath);
    }


    static string ResolvePluginPath(string tildePath)
    {
        var rel = tildePath.TrimStart('~', '/', '\\');
        var full = Path.Combine(PluginDir(), rel);
        return Path.GetFullPath(full);
    }

}
