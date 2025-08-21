using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Makro4._8
{
    public partial class Window1 : Window
    {
        private SerialPort _serialPort;

        public Window1()
        {
            InitializeComponent();
            Loaded += Window1_Loaded;
            Closed += Window1_Closed;
        }

        private void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshPorts();
            // jeśli chcesz: wybierz ostatnio używany port itp.
        }

        private void Window1_Closed(object sender, EventArgs e)
        {
            TryClosePort();
        }

        // === UI HANDLERS ===

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

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            // jeśli kiedyś będziesz używać opcji CR/LF — odczytamy je w GetTerminator()
        }

        private void InsertLine_Click(object sender, RoutedEventArgs e) { }
        private void InsertPoint_Click(object sender, RoutedEventArgs e) { }
        private void InsertArc_Click(object sender, RoutedEventArgs e) { }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        // === SERIAL PORT ===

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

        private void TryOpenSelectedPort()
        {
            if (comboPorts.SelectedItem == null)
            {
                MessageBox.Show("Nie wybrano portu COM");
                return;
            }

            var portName = comboPorts.SelectedItem.ToString();

            try
            {
                // Parametry: ustaw pod swój tachimetr albo zbierz z zakładki „Ustawienia”
                _serialPort = new SerialPort(
                    portName,
                    9600,               // BaudRate
                    Parity.None,
                    8,                  // DataBits
                    StopBits.One
                );

                _serialPort.Handshake = Handshake.None;
                _serialPort.NewLine = GetTerminator();   // CR/LF w zależności od radia w UI
                _serialPort.Encoding = Encoding.ASCII;

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();

                Log("Połączono z " + portName);
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
                string data;
                if (!string.IsNullOrEmpty(_serialPort.NewLine))
                {
                    data = _serialPort.ReadLine();
                }
                else
                {
                    data = _serialPort.ReadExisting();
                }
                Dispatcher.Invoke(() => Log("RX: " + data));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log("Błąd RX: " + ex.Message));
            }
        }

        private void SendLine(string text)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    Log("Port nie jest otwarty.");
                    return;
                }

                string suffix = GetTerminator();
                string payload = suffix != null ? text + suffix : text;

                _serialPort.Write(payload);
                Log("TX: " + payload.Replace("\r", "\\r").Replace("\n", "\\n"));
            }
            catch (Exception ex)
            {
                Log("Błąd TX: " + ex.Message);
            }
        }

        private string GetTerminator()
        {
            // Odczyt z radio buttonów w sekcji „Opcje przesyłanego tekstu”.
            // Jeżeli nie masz x:Name dla RadioButton, możesz na szybko sprawdzić po Content:
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
            return "\r\n"; // domyślnie CRLF
        }

        // === HELPERS ===

        private void SetStartButtonConnected(bool connected)
        {
            // znajdź przycisk „Start” i zmień podpis
            var startBtn = FindDescendant<Button>(this, b => (b.Content as string) == "Start" || (b.Content as string) == "Stop");
            if (startBtn != null)
                startBtn.Content = connected ? "Stop" : "Start";
        }

        private void Log(string msg)
        {
            logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
            logTextBox.ScrollToEnd();
        }

        // proste wyszukiwacze wizualne (bez dodatkowych bibliotek)
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
    }
}
