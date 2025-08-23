using Gssoft.Gscad.ApplicationServices.Core;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Gssoft.Gscad.Runtime;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Rozpoczyna interakcję z użytkownikiem: wskazanie punktu i wstawienie DBPoint (na razie).
    /// Zwraca true, jeśli pomiar wykonano (nie anulowano).
    /// </summary>
    public bool StartMeasurement()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return false;

        var ed = doc.Editor;

        var ppo = new PromptPointOptions("\nWskaż punkt pomiaru:")
        {
            AllowNone = false
        };
        var ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK)
            return false;

        var ucs = ed.CurrentUserCoordinateSystem;
        var pickedUcs = ppr.Value;
        var pickedWcs = pickedUcs.TransformBy(ucs);

        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
            var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var dbp = new DBPoint(pickedWcs);
            var entId = btr.AppendEntity(dbp);
            tr.AddNewlyCreatedDBObject(dbp, true);
            tr.Commit();

            LastPointWcs = pickedWcs;
            LastEntityId = entId;
            AllPointsWcs.Add(pickedWcs);
        }

        MeasurementAdded?.Invoke(this, pickedWcs);

        ed.WriteMessage($"\nZapisano punkt (WCS): {Format3d(pickedWcs)}");

        return true;
    }

    private static string Format3d(Point3d p) => $"Tachimetr został ustawiony na X={p.X:0.###}, Y={p.Y:0.###}, Z={p.Z:0.###}";
}
