using System.Windows;
using System.Windows.Controls;

namespace Makro_PP
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        // Obsługa zdarzeń z zakładki "Pomiary"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Implementacja wspólna dla wszystkich przycisków
            var button = (Button)sender;
            switch (button.Content.ToString())
            {
                case "Rozpocznij pomiar":
                    // Logika dla rozpoczęcia pomiaru
                    break;
                case "Pomierz punkt":
                    // Logika dla pomiaru punktu
                    break;
                case "Cofnij punkt":
                    // Logika dla cofnięcia punktu
                    break;
                case "Przestaw tachimetr":
                    // Logika dla przestawienia tachimetru
                    break;
            }
        }

        // Obsługa zdarzenia TextChanged dla TextBox
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Logika gdy zmienia się tekst w TextBox
        }

        // Obsługa zdarzeń z zakładki "Komendy CAD"
        private void InsertLine_Click(object sender, RoutedEventArgs e)
        {
            // Logika dla wstawienia linii
        }

        private void InsertPoint_Click(object sender, RoutedEventArgs e)
        {
            // Logika dla wstawienia punktu
        }

        private void InsertArc_Click(object sender, RoutedEventArgs e)
        {
            // Logika dla wstawienia łuku
        }

        // Obsługa zdarzeń RadioButton
        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            // Logika dla wyboru opcji przesyłanego tekstu
        }
    }
}