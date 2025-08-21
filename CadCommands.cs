using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.ApplicationServices;
using System.Windows.Interop;
using Makro4._8.Services;

namespace Makro4._8
{
    public class CadCommands
    {
        private static readonly StateService _state = StateService.Instance;
        private static readonly CadDrawingService _draw = new CadDrawingService(_state);

        [CommandMethod("LL")]
        public void DrawLines() => _draw.DrawQueuedLinesAndLastPoint();

        [CommandMethod("PP")]
        public void DrawAllPoints() => _draw.DrawAllPoints();

        [CommandMethod("OO")]
        public void AddControlPoints() => _draw.CommitControlPoints();

        [CommandMethod("OpenMyUi")]
        public void OpenMyUi()
        {
            var window = new Window1();
            var helper = new WindowInteropHelper(window);

            var owner = (Application.MainWindow != null)
                        ? Application.MainWindow.Handle
                        : System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

            helper.Owner = owner;
            window.ShowDialog();
        }
    }
}
