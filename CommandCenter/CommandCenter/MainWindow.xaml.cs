using CommandCenter.Dummy;
using CommandCenter.Model;
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CommandCenter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        private MapDrawer mapDrawer;
        private ArrayList prajurits;

        public MainWindow()
        {
            InitializeComponent();
            
            prajurits = new ArrayList();

            // Start listener
            UDPListener listener = new UDPListener();
            Thread thread = new Thread(new ThreadStart(listener.listen));
            thread.Start();

            // TODO Sample only
            prajurits.Add(new Prajurit("Pascal", new PrajuritState(new Location(-6.87491,107.60643), 0)));
            prajurits.Add(new Prajurit("Kristopher", new PrajuritState(new Location(-6.87503,107.60501), 0)));

            mapDrawer = new MapDrawer(map, prajurits);
            mapDrawer.updateMap();

            // TODO sample only
            new DummyPrajuritMovement(prajurits, mapDrawer);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == showEveryoneButton)
            {
                mapDrawer.showEveryone();
            }
        }

        private void pendaftaranButton_Click(object sender, RoutedEventArgs e)
        {
            String gameId = "";
            Random random = new Random();
            for (int i = 0; i < 3; i++)
            {
                gameId += random.Next(10);
            }
            idSimulationLabel.Content = gameId;

            pendaftaranButton.IsEnabled = false;
            mulaiButton.IsEnabled = true;
            akhiriButton.IsEnabled = true;
        }

        private void akhiriButton_Click(object sender, RoutedEventArgs e)
        {
            idSimulationLabel.Content = "###";

            pendaftaranButton.IsEnabled = true;
            mulaiButton.IsEnabled = false;
            akhiriButton.IsEnabled = false;
        }
    }
}
