﻿using CommandCenter.Model;
using CommandCenter.Model.Protocol;
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

        private UDPCommunication comm;
        private GameController controller;
        public String gameId = null;

        public MainWindow()
        {
            InitializeComponent();
            
            prajurits = new ArrayList();

            // TODO Sample only
            prajurits.Add(new Prajurit("Pascal", new PrajuritState(new Location(-6.87491,107.60643), 0)));
            prajurits.Add(new Prajurit("Kristopher", new PrajuritState(new Location(-6.87503,107.60501), 0)));

            mapDrawer = new MapDrawer(map, prajurits);
            mapDrawer.updateMap();
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
            Random random = new Random();
            gameId = "";
            for (int i = 0; i < 3; i++)
            {
                gameId += random.Next(10);
            }
            idSimulationLabel.Content = gameId;

            pendaftaranButton.IsEnabled = false;
            mulaiButton.IsEnabled = true;
            akhiriButton.IsEnabled = true;

            // Start controller and start listening
            comm = new UDPCommunication(this);
            controller = new GameController(this, comm);
            comm.listenAsync(controller);
            writeLog("Pendaftaran dibuka, game id = " + gameId);
        }

        private void akhiriButton_Click(object sender, RoutedEventArgs e)
        {
            idSimulationLabel.Content = "###";
            gameId = null;
            comm.stopListenAsync();

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
            if (comm != null)
            {
                comm.stopListenAsync();
            }
        }
    }
}