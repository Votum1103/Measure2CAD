using System;
using System.Collections.Generic;

namespace Measure2cad.Services
{
    public class StateService
    {
        public static StateService Instance { get; } = new StateService();

        public List<(double x, double y)> PendingPoints { get; } = new List<(double x, double y)>();

        public List<((double x, double y) a, (double x, double y) b)> PendingLines { get; }  = new List<((double x, double y) a, (double x, double y) b)>();

        public List<((double x, double y, double z) pt, int nr)> ControlPoints { get; } = new List<((double x, double y, double z) pt, int nr)>();

        public (double x, double y, double z) TachymeterPos { get; set; } = (0, 0, 0);
        public double TachymeterRotationDeg { get; set; } = 0;

        public (double x, double y, double z) CalcXY(double hzGrad, double sdist)
        {
            double hzRad = (hzGrad * 0.9) * Math.PI / 180.0;
            double x = sdist * Math.Cos(hzRad);
            double y = sdist * Math.Sin(hzRad);

            double ang = TachymeterRotationDeg * Math.PI / 180.0;
            double xr = x * Math.Cos(ang) - y * Math.Sin(ang);
            double yr = x * Math.Sin(ang) + y * Math.Cos(ang);

            return (xr + TachymeterPos.x, yr + TachymeterPos.y, 0);
        }
    }
}
