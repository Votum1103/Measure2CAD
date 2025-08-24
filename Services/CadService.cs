using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.Geometry;
using System.IO;

namespace Measure2cad.Services
{
    public class CadService
    {
        public void SetPointStyle(double size = 0.02)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.Command("._SETVAR", "PDMODE", 34);
            doc.Editor.Command("._SETVAR", "PDSIZE", size);
        }

        public ObjectId DrawPoint(Point3d pt, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var p = new DBPoint(pt) { ColorIndex = colorIndex };
                var id = ms.AppendEntity(p); tr.AddNewlyCreatedDBObject(p, true);
                tr.Commit();
                return id;
            }
        }

        public ObjectId DrawLine(Point3d a, Point3d b, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var ln = new Line(a, b) { ColorIndex = colorIndex };
                var id = ms.AppendEntity(ln); tr.AddNewlyCreatedDBObject(ln, true);
                tr.Commit();
                return id;
            }
        }

        public ObjectId DrawText(Point3d at, string text, double height = 0.05, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var mt = new MText { Location = at, Contents = text, TextHeight = height, ColorIndex = colorIndex };
                var id = ms.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
                tr.Commit();
                return id;
            }
        }

        public ObjectId InsertBlock(Database db, string dwgPath, string blockName, Point3d position, double scale = 1.0)
        {
            if (!File.Exists(dwgPath))
                throw new FileNotFoundException($"DWG block file not found: {dwgPath}");

            ObjectId defId;
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(dwgPath, FileOpenMode.OpenForReadAndAllShare, true, "");
                var ids = new ObjectIdCollection();
                using (var tr = sourceDb.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    if (!bt.Has(blockName))
                        throw new System.Exception($"Block '{blockName}' not found in {dwgPath}");
                    ids.Add(bt[blockName]);
                }
                var idMap = new IdMapping();
                db.WblockCloneObjects(ids, db.BlockTableId, idMap, DuplicateRecordCloning.Ignore, false);
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                defId = bt[blockName];
                tr.Commit();
            }

            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var br = new BlockReference(position, defId) { ScaleFactors = new Scale3d(scale) };
                var id = ms.AppendEntity(br); tr.AddNewlyCreatedDBObject(br, true);
                tr.Commit();
                return id;
            }
        }
    }
}
