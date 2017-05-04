using System;
using System.Windows;

namespace Client
{
    /// <summary>
    ///     Interaction logic for Connexion.xaml
    /// </summary>
    public partial class Connexion : Window
    {
        public Connexion()
        {
            InitializeComponent();
            Width = 300;
            Height = 150;
        }

        public event EventHandler Done;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}