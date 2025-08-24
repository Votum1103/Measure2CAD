using System;
using Gssoft.Gscad.ApplicationServices.Core;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using Measure2cad.Services;

public sealed class SurveyService
{
    private static readonly Lazy<SurveyService> _lazy = new Lazy<SurveyService>(() => new SurveyService());
    public static SurveyService Instance => _lazy.Value;

    private readonly SurveyState _state = SurveyState.Instance;
    private readonly CadService _cad = new CadService();

    public string TachyDwgPath { get; set; } = ResolvePluginPath("./DWGBlocks/TotalStation.dwg");
    public string TachyBlockName { get; set; } = "Total";
    public double TachyBlockScale { get; set; } = 1.0;

    public event EventHandler<Point3d> StationSet;
    public event EventHandler<Point3d> PointAdded;

    private SurveyService() { }

    public bool StartStationSetup()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc == null) return false;
        var ed = doc.Editor;

        var ppo = new PromptPointOptions("\nWskaż pozycję tachimetru:") { AllowNone = false };
        var ppr = ed.GetPoint(ppo);
        if (ppr.Status != PromptStatus.OK) return false;

        var ucs = ed.CurrentUserCoordinateSystem;
        var pickedUcs = ppr.Value;
        var pickedWcs = pickedUcs.TransformBy(ucs);

        try
        {
            _cad.InsertBlock(doc.Database, TachyDwgPath, TachyBlockName, pickedWcs, TachyBlockScale < 0.2 ? 0.02 : TachyBlockScale);
        }
        catch (Exception ex)
        {
            ed.WriteMessage($"\nNie udało się wczytać/wstawić bloku '{TachyBlockName}': {ex.Message}");
            return false;
        }

        _state.StationWcs = pickedWcs;

        StationSet?.Invoke(this, pickedWcs);
        ed.WriteMessage($"\nTachimetr ustawiono w: X={pickedWcs.X:0.###}, Y={pickedWcs.Y:0.###}, Z={pickedWcs.Z:0.###}");
        return true;
    }

    public Point3d AddMeasuredObservation(double hzRad, double vRad, double slopeDist)
    {
        var wcs = _state.ComputePointWcs(hzRad, vRad, slopeDist);
        _state.MeasuredPointsWcs.Add(wcs);

        PointAdded?.Invoke(this, wcs);
        return wcs;
    }

    public void DrawAllPoints(short colorIndex = 2)
    {
        int n = _state.PendingPointsCount;
        if (n <= 0) return;

        _cad.SetPointStyle(0.02);
        foreach (var p in _state.MeasuredPointsWcs)
            _cad.DrawPoint(p, colorIndex);

        _state.PendingPointsCount = 0;
    }

    public void   DrawQueuedLines(short colorIndex = 3)
    {
        foreach (var ln in _state.PendingLinesWcs)
            _cad.DrawLine(ln.a, ln.b, colorIndex);
    }

    public void AddControlPoint(Point3d pt, int nr, bool drawNow = false)
    {
        _state.ControlPoints.Add((pt, nr));
        if (drawNow)
        {
            _cad.DrawPoint(pt, colorIndex: 1);
            _cad.DrawText(pt, $"#{nr}", 0.05, colorIndex: 1);
        }
    }

    private static string ResolvePluginPath(string tildePath)
    {
        var asmPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var dir = System.IO.Path.GetDirectoryName(asmPath);
        var rel = tildePath.TrimStart('~', '/', '\\');
        var full = System.IO.Path.Combine(dir, rel);
        return System.IO.Path.GetFullPath(full);
    }
}
