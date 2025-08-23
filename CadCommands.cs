using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.ApplicationServices;
using System.Windows.Interop;
using Measure2cad.Services;

namespace Measure2cad
{
    public class CadCommands
    {
        private static readonly StateService _state = StateService.Instance;
        private static readonly CadDrawingService _draw = new CadDrawingService(_state);
        private static Window1 _win;

        [CommandMethod("ll")]
        public void DrawLines() => _draw.DrawQueuedLinesAndLastPoint();

        [CommandMethod("pp")]
        public void DrawAllPoints() => _draw.DrawAllPoints();

        [CommandMethod("oo")]
        public void AddControlPoints() => _draw.CommitControlPoints();

        [CommandMethod("wnd")]
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
