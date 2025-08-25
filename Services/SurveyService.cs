using System;
using System.Collections.Generic;
using System.Windows.Documents;
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
    public event EventHandler<int> MeasuredCountChanged;

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
            _cad.InsertBlock(doc.Database, TachyDwgPath, TachyBlockName, pickedWcs, TachyBlockScale);
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

    private void RaiseMeasuredCountChanged()
    => MeasuredCountChanged?.Invoke(this, _state.MeasuredPointsWcs.Count);

    public Point3d AddMeasuredObservation(double hzRad, double vRad, double slopeDist)
    {
        var wcs = _state.ComputePointWcs(hzRad, vRad, slopeDist);
        _state.MeasuredPointsWcs.Add(wcs);

        RaiseMeasuredCountChanged();
        return wcs;
    }

    public void DrawAllPoints(short colorIndex = 2)
    {
        var pts = _state.MeasuredPointsWcs;
        if (pts.Count <= 0) return;

        foreach (var p in _state.MeasuredPointsWcs)
            _cad.DrawPoint(p, colorIndex);

        pts.Clear();
        RaiseMeasuredCountChanged();
    }

    private static bool TryIntersectInfiniteLines(Point3d A1, Point3d A2, Point3d B1, Point3d B2, out Point3d X, double eps = 1e-12)
    {
        // P + t*r, Q + u*s
        double rX = A2.X - A1.X, rY = A2.Y - A1.Y;
        double sX = B2.X - B1.X, sY = B2.Y - B1.Y;

        // zdegenerowane
        if (Math.Abs(rX) < eps && Math.Abs(rY) < eps) { X = default; return false; }
        if (Math.Abs(sX) < eps && Math.Abs(sY) < eps) { X = default; return false; }

        double rxs = rX * sY - rY * sX;
        double qpx = B1.X - A1.X, qpy = B1.Y - A1.Y;

        if (Math.Abs(rxs) < eps) { X = default; return false; } // równoległe/współliniowe

        double t = (qpx * sY - qpy * sX) / rxs;
        X = new Point3d(A1.X + t * rX, A1.Y + t * rY, 0.0);
        return true;
    }

    public void DrawQueuedLines(short lineColorIndex = 3, short oddPointColorIndex = 2)
    {
        var pts = _state.MeasuredPointsWcs;
        int n = pts.Count;
        if (n == 0) return;

        // nieparzysty – pokaż ostatni punkt i pomiń
        if ((n & 1) == 1)
        {
            _cad.DrawPoint(pts[n - 1], oddPointColorIndex);
            pts.RemoveAt(n - 1);
            n--;
        }
        if (n < 2) return;

        int m = n / 2; // liczba linii: L0=(p0,p1), L1=(p2,p3), ...

        // 1) policz przecięcia sąsiednich linii: X[k] = Lk ∩ Lk+1
        var X = new Point3d[Math.Max(0, m - 1)];
        var hasX = new bool[Math.Max(0, m - 1)];

        for (int k = 0; k + 1 < m; k++)
        {
            int a0 = 2 * k, a1 = a0 + 1;
            int b0 = 2 * (k + 1), b1 = b0 + 1;

            hasX[k] = TryIntersectInfiniteLines(pts[a0], pts[a1], pts[b0], pts[b1], out X[k]);
        }

        // 2) narysuj po JEDNYM odcinku na każdą linię
        for (int k = 0; k < m; k++)
        {
            int p0 = 2 * k;
            int p1 = p0 + 1;

            _cad.DrawPoint(pts[p0], oddPointColorIndex);
            _cad.DrawPoint(pts[p1], oddPointColorIndex);

            if (m == 1)
            {
                _cad.DrawLine(pts[p0], pts[p1], lineColorIndex);
                break;
            }

            if (k == 0)
            {
                // L0: p0 -> X0 (jeśli brak X0, rysuj jak jest)
                if (hasX[0]) _cad.DrawLine(pts[p0], X[0], lineColorIndex);
                else _cad.DrawLine(pts[p0], pts[p1], lineColorIndex);
            }
            else if (k == m - 1)
            {
                // L_{m-1}: X_{m-2} -> p1 (jeśli brak X_{m-2}, rysuj jak jest)
                if (hasX[m - 2]) _cad.DrawLine(X[m - 2], pts[p1], lineColorIndex);
                else _cad.DrawLine(pts[p0], pts[p1], lineColorIndex);
            }
            else
            {
                // Lk: X_{k-1} -> X_k; jeśli któryś brak, rysuj jak jest
                if (hasX[k - 1] && hasX[k])
                    _cad.DrawLine(X[k - 1], X[k], lineColorIndex);
                else
                    _cad.DrawLine(pts[p0], pts[p1], lineColorIndex);
            }

            // (opcjonalnie) zaznacz pierwszy punkt każdej linii
            _cad.DrawPoint(pts[p0], oddPointColorIndex);
        }

        pts.Clear();
        RaiseMeasuredCountChanged();
    }


    public void AddControlPoint(Point3d pt, bool drawNow = false)
    {
        _state.ControlPoints.Add((pt));
        if (drawNow)
        {
            _cad.DrawPoint(pt, colorIndex: 1);
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
