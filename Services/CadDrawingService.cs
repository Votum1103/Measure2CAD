using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.Geometry;

namespace Makro4._8.Services
{
    public class CadDrawingService
    {
        private readonly StateService _state;
        public CadDrawingService(StateService state) { _state = state; }

        public void SetPointStyle(double size = 0.02)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.Command("._SETVAR", "PDMODE", 34);
            doc.Editor.Command("._SETVAR", "PDSIZE", size);
        }

        public void DrawPoint((double x, double y, double z) pt, string label, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var p = new DBPoint(new Point3d(pt.x, pt.y, pt.z)) { ColorIndex = colorIndex };
                btr.AppendEntity(p); tr.AddNewlyCreatedDBObject(p, true);

                var mt = new MText
                {
                    Location = new Point3d(pt.x, pt.y, pt.z),
                    Contents = label,
                    TextHeight = 0.05,
                    ColorIndex = colorIndex
                };
                btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);

                tr.Commit();
            }
        }

        public void DrawLine((double x, double y, double z) a, (double x, double y, double z) b, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var ln = new Line(new Point3d(a.x, a.y, a.z), new Point3d(b.x, b.y, b.z)) { ColorIndex = colorIndex };
                btr.AppendEntity(ln); tr.AddNewlyCreatedDBObject(ln, true);
                tr.Commit();
            }
        }

        public void DrawQueuedLinesAndLastPoint() { /* iteracja jak w Twoim `draw_lines` */ }
        public void DrawAllPoints() { /* jak `draw_all_points` */ }
        public void CommitControlPoints() { /* jak `add_control_point` */ }
    }
}
