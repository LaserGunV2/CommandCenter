using CommandCenter.Model;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
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
        
        public MapDrawer mapDrawer;
        public List<Prajurit> prajurits;
        public Dictionary<int, Senjata> senjatas;

        private GameController controller;
 
        public MainWindow()
        {
            InitializeComponent();
            
            prajurits = new List<Prajurit>();
            pesertaDataGrid.DataContext = prajurits;
            senjatas = new Dictionary<int, Senjata>();

            controller = new GameController(this);

            // TODO Sample only
            prajurits.Add(new Prajurit(1, "2003730013", new IPAddress(16777343), "A", new Location(-6.87491, 107.60643)));
            prajurits.Add(new Prajurit(2, "2003730010", new IPAddress(16777343), "B", new Location(-6.87503, 107.60501)));

            mapDrawer = new MapDrawer(map, prajurits);
            mapDrawer.updateMap();
        }

        private void pendaftaranButton_Click(object sender, RoutedEventArgs e)
        {
            pendaftaranButton.IsEnabled = false;
            mulaiButton.IsEnabled = true;
            akhiriButton.IsEnabled = true;

            // Start controller and start listening
            idSimulationLabel.Content = controller.startRegistration();
        }

        private void mulaiButton_Click(object sender, RoutedEventArgs e)
        {
            pendaftaranButton.IsEnabled = false;
            mulaiButton.IsEnabled = false;
            akhiriButton.IsEnabled = true;

            controller.startPlaying();
        }

        private void akhiriButton_Click(object sender, RoutedEventArgs e)
        {
            idSimulationLabel.Content = "###";
            controller.stopPlaying();

            pendaftaranButton.IsEnabled = true;
            mulaiButton.IsEnabled = false;
            akhiriButton.IsEnabled = false;
        }

        public void writeLog(String s)
        {
                Dispatcher.InvokeAsync((Action)(() =>
                {
                    if ((bool)peristiwaCheckBox.IsChecked)
                    {
                        peristiwaTextBlock.Text = s + "\n" + peristiwaTextBlock.Text;
                    }
                }));
            }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.stopPlaying();
        }

        public void refreshTable()
        {
            Dispatcher.InvokeAsync((Action)(() =>
            {
                try
                {
                    pesertaDataGrid.Items.Refresh();
                } catch (InvalidOperationException)
                {
                    // void
                }

            }));            
        }
    }
}