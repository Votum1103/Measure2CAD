using Makro4._8.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Makro4._8
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void InsertLine_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InsertPoint_Click(object sender, RoutedEventArgs e)
        {
        }

        private void InsertArc_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
        }

        private void RefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            //comboPorts.ItemsSource = SerialPort.GetPortNames();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            //if (comboPorts.SelectedItem == null)
            //{
            //    MessageBox.Show("Wybierz port COM!");
            //    return;
            //}

            //try
            //{
            //    _serialPort = new SerialPort(comboPorts.SelectedItem.ToString(), 9600, Parity.None, 8, StopBits.One);
            //    _serialPort.DataReceived += SerialPort_DataReceived;
            //    _serialPort.Open();

            //    Log("Połączono z " + _serialPort.PortName);
            //}
            //catch (Exception ex)
            //{
            //    Log("Błąd: " + ex.Message);
            //}
        }

        //private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    //try
        //    //{
        //    //    string data = _serialPort.ReadExisting();
        //    //    Dispatcher.Invoke(() => Log("RX: " + data));
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    Dispatcher.Invoke(() => Log("Błąd RX: " + ex.Message));
        //    //}
        //}

        private void Log(string msg)
        {
            //    logTextBox.AppendText(msg + Environment.NewLine);
            //    logTextBox.ScrollToEnd();
            //}
        }
    }
}