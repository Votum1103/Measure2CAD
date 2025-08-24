using System;
using System.Collections.Generic;
using Gssoft.Gscad.Geometry;

namespace Measure2cad.Services
{
    public class SurveyState
    {
        public static SurveyState Instance { get; } = new SurveyState();

        private SurveyState() { }
        public Point3d StationWcs { get; set; } = new Point3d(0, 0, 0);
        public double StationRotationDeg { get; set; } = 0.0;
        public List<Point3d> MeasuredPointsWcs { get; } = new List<Point3d>();
        public List<Point3d> ControlPoints { get; } = new List<Point3d>();
        public Point3d ComputePointWcs(double hzRad, double vRad, double slopeDist)
        {
            double hDist = slopeDist * Math.Sin(vRad);
            double dz = slopeDist * Math.Cos(vRad);

            double lx = hDist * Math.Cos(hzRad);
            double ly = hDist * Math.Sin(hzRad);

            double rot = StationRotationDeg * Math.PI / 180.0;
            double wx = lx * Math.Cos(rot) - ly * Math.Sin(rot);
            double wy = lx * Math.Sin(rot) + ly * Math.Cos(rot);

            return new Point3d(StationWcs.X + wx, StationWcs.Y + wy, StationWcs.Z + dz);
        }
    }
}
