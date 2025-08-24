using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.ApplicationServices;
using System.Windows.Interop;
using Measure2cad.Services;

namespace Measure2cad
{
    public class CadCommands
    {
        private static readonly SurveyState _state = SurveyState.Instance;
        private static readonly CadService _draw = new CadService();
        private static MainWindow _win;

        [CommandMethod("PP")]
        public void DrawAllMeasuredPoints() => SurveyService.Instance.DrawAllPoints();

        [CommandMethod("LL")]
        public void DrawQueuedLines() => SurveyService.Instance.DrawQueuedLines();

        [CommandMethod("ST")]
        public void StartStation() => SurveyService.Instance.StartStationSetup();

        [CommandMethod("wnd")]
        public void OpenMyUi()
        {
            if (_win == null)
            {
                _win = new MainWindow();
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
