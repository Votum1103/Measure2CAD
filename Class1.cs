using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;


namespace Makro_PP
{
    public class Methods
    {
        [CommandMethod("OpenMyUi")]
        public void OpenMyUi()
        {
            var window = new Window1();
            var helper = new WindowInteropHelper(window);
            helper.Owner = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
            window.ShowDialog();
        }
    }
}
