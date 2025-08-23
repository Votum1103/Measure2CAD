using System;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Measure2cad
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPorts();

            cmbParity.ItemsSource = Enum.GetValues(typeof(Parity));
            cmbStopBits.ItemsSource = new[] { StopBits.One, StopBits.OnePointFive, StopBits.Two };
            cmbHandshake.ItemsSource = Enum.GetValues(typeof(Handshake));

            if (cmbParity.SelectedItem == null) cmbParity.SelectedItem = Parity.None;
            if (cmbStopBits.SelectedItem == null) cmbStopBits.SelectedItem = StopBits.One;
            if (cmbHandshake.SelectedItem == null) cmbHandshake.SelectedItem = Handshake.None;

            if (string.IsNullOrWhiteSpace(txtBaud.Text)) txtBaud.Text = "9600";
            if (string.IsNullOrWhiteSpace(txtDataBits.Text)) txtDataBits.Text = "8";
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            TryClosePort();
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                TryOpenSelectedPort();
            else
                TryClosePort();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void BtnStartMeasurement_Click(object sender, RoutedEventArgs e)
        {
            try { Gssoft.Gscad.Internal.Utils.SetFocusToDwgView(); } catch { }

            var ok = MeasurementService.Instance.StartMeasurement();

            var last = MeasurementService.Instance.LastPointWcs;
            if (ok && last.HasValue)
            {
                var p = last.Value;
                logTextBox.Text = $"Wstawiono tachimetr w punkcie: X={p.X:0.###}; Y={p.Y:0.###}; Z={p.Z:0.###}\n";
            }
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
        }

        private void InsertLine_Click(object sender, RoutedEventArgs e) { }
        private void InsertPoint_Click(object sender, RoutedEventArgs e) { }
        private void InsertArc_Click(object sender, RoutedEventArgs e) { }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }


        private void RefreshPorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames().OrderBy(n => n).ToArray();
                comboPorts.ItemsSource = ports;
                if (ports.Length > 0 && comboPorts.SelectedItem == null)
                    comboPorts.SelectedIndex = 0;

                Log($"Dostępne porty: {string.Join(", ", ports)}");
            }
            catch (Exception ex)
            {
                Log("Błąd przy odświeżaniu portów: " + ex.Message);
            }
        }

        private static int ParseIntOrDefault(string text, int @default)
    => int.TryParse(text, out var v) ? v : @default;

        private static T ParseEnumFromCombo<T>(ComboBox combo, T @default) where T : struct, Enum
        {
            if (combo?.SelectedItem is T t) return t;

            // jeśli Items są stringami/ComboBoxItem, spróbuj zparsować po tekście
            var s = (combo?.SelectedItem as ComboBoxItem)?.Content as string
                    ?? combo?.SelectedItem?.ToString();

            return Enum.TryParse<T>(s, ignoreCase: true, out var val) ? val : @default;
        }
        private void TryOpenSelectedPort()
        {
            if (comboPorts.SelectedItem == null)
            {
                MessageBox.Show("Nie wybrano portu COM");
                return;
            }

            var portName = comboPorts.SelectedItem.ToString();

            int baud = ParseIntOrDefault(txtBaud.Text, 9600);
            int dataBits = ParseIntOrDefault(txtDataBits.Text, 8);
            Parity parity = ParseEnumFromCombo(cmbParity, Parity.None);
            StopBits stopBits = ParseEnumFromCombo(cmbStopBits, StopBits.One);
            Handshake handshake = ParseEnumFromCombo(cmbHandshake, Handshake.None);

            try
            {
                _serialPort = new SerialPort(
                    portName,
                    baud,
                    parity,
                    dataBits,
                    stopBits
                );

                _serialPort.Handshake = handshake;

                _serialPort.NewLine = GetTerminator() ?? string.Empty;

                _serialPort.Encoding = Encoding.ASCII;

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();

                Log($"Połączono z {portName} (baud={baud}, dataBits={dataBits}, parity={parity}, stopBits={stopBits}, handshake={handshake})");
                SetStartButtonConnected(true);
            }
            catch (Exception ex)
            {
                Log("Błąd otwarcia portu: " + ex.Message);
                TryClosePort();
            }
        }


        private void TryClosePort()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= SerialPort_DataReceived;
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                    Log("Rozłączono.");
                }
            }
            catch (Exception ex)
            {
                Log("Błąd zamykania portu: " + ex.Message);
            }
            finally
            {
                SetStartButtonConnected(false);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = !string.IsNullOrEmpty(_serialPort.NewLine)
                              ? _serialPort.ReadLine()
                              : _serialPort.ReadExisting();

                if (TryParseGeoComMeasurement(line, out double hz, out double v, out double dist))
                {
                    Dispatcher.Invoke(() =>
                    {
                        Log($"WYNIK: Hz={hz:F6} rad, V={v:F6} rad, D={dist:F3} m");
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log("Błąd RX: " + ex.Message));
            }
        }
        private bool TryParseGeoComMeasurement(string line, out double hz, out double v, out double dist)
        {
            hz = 0; dist = 0; v = 0;

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("%R1P,0,0:"))
                return false;

            var payload = line.Substring("%R1P,0,0:".Length);

            if (payload == "0")
                return false;

            var parts = payload.Split(',');
            if (parts.Length < 4)
                return false;

            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out hz))
                return false;
            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                return false;
            if (!double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out dist))
                return false;

            return true;
        }

        private string GetTerminator()
        {
            foreach (var rb in FindVisualChildren<RadioButton>(this))
            {
                if (rb.IsChecked == true)
                {
                    switch ((rb.Content as string) ?? "")
                    {
                        case "Append CR": return "\r";
                        case "Append LF": return "\n";
                        case "Append CR/LF": return "\r\n";
                        case "Append nothing": return null;
                    }
                }
            }
            return "\r\n";
        }

        private void SetStartButtonConnected(bool connected)
        {
            var startBtn = FindDescendant<Button>(this, b => (b.Content as string) == "Start" || (b.Content as string) == "Stop");
            if (startBtn != null)
                startBtn.Content = connected ? "Stop" : "Start";
        }

        private void Log(string msg)
        {
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            logTextBox.ScrollToEnd();
        }

        private static T FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
        {
            foreach (var d in FindVisualChildren<T>(root))
                if (predicate(d)) return d;
            return null;
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;

                foreach (var c in FindVisualChildren<T>(child))
                    yield return c;
            }
        }
        private void MeasurePoint_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                Log("Błąd: brak aktywnego połączenia szeregowego!");
                return;
            }

            try
            {
                _serialPort.DiscardInBuffer();

                SendGeoCom("%R1Q,2008:1,1");
                SendGeoCom("%R1Q,2108:2000,1");
                SendGeoCom("%R1Q,11003:");
                SendGeoCom("%R1Q,2008:0,0");

                Log("Trwa pomiar punktu");
            }
            catch (Exception ex)
            {
                Log("Błąd TX: " + ex.Message);
            }
        }
        private void SendGeoCom(string ascii)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    Log("Port nie jest otwarty.");
                    return;
                }

                string suffix = GetTerminator();
                string payload = suffix != null ? ascii + suffix : ascii;

                _serialPort.Write(payload);
            }
            catch (Exception ex)
            {
                Log("Błąd TX: " + ex.Message);
            }
        }
    }
}
  