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
        private static Window1 _win;

        [CommandMethod("LL")]
        public void DrawLines() => _draw.DrawQueuedLinesAndLastPoint();

        [CommandMethod("PP")]
        public void DrawAllPoints() => _draw.DrawAllPoints();

        [CommandMethod("OO")]
        public void AddControlPoints() => _draw.CommitControlPoints();

        [CommandMethod("OpenMyUi")]
        public void OpenMyUi()
        {
            if (_win == null)
            {
                _win = new Window1();
                _win.Closed += (s, e) => _win = null;
                Application.ShowModelessWindow(_win);
            }
            else
            {
                _win.Activate();
            }
        }
    }
}
