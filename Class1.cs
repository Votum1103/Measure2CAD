using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.Geometry;
using System.Windows.Interop;

namespace Makro4._8
{
    public class Methods
    {
        [CommandMethod("OpenMyUi")]
        public void OpenMyUi()
        {
            var window = new Window1();
            var helper = new WindowInteropHelper(window);

            // W GstarCAD odpowiednik Application.MainWindow zwykle istnieje;
            // jeśli nie, użyj uchwytu bieżącego procesu (fallback).
            var owner = (Application.MainWindow != null)
                        ? Application.MainWindow.Handle
                        : System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            helper.Owner = owner;
            window.ShowDialog();
        }

        [CommandMethod("DrawLine")]
        public static void DrawLine()
        {
            var startPoint = new Point3d(0, 0, 0);
            var endPoint = new Point3d(10, 10, 0);

            var doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage($"\nDrawing line from {startPoint} to {endPoint}");
            ed.Command("_.LINE", startPoint, endPoint, "");
        }
    }
}
