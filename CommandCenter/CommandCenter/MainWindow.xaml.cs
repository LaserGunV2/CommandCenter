﻿using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

        EventsPlayer player;
 
        public MainWindow()
        {
            InitializeComponent();
            
            prajurits = new List<Prajurit>();
            pesertaDataGrid.DataContext = prajurits;
            senjatas = new Dictionary<int, Senjata>();

            controller = new GameController(this);
            player = new EventsPlayer();

            mapDrawer = new MapDrawer(map, prajurits);
            mapDrawer.updateMap();
        }

        private void pendaftaranButton_Click(object sender, RoutedEventArgs e)
        {
            String result = controller.startRegistration();
            if (result != null)
            {
                pendaftaranButton.IsEnabled = false;
                mulaiButton.IsEnabled = true;
                akhiriButton.IsEnabled = true;
                loadButton.IsEnabled = false;
                saveButton.IsEnabled = false;
                replayLengthLabel.Content = "0:00.000";

                // Start controller and start listening
                idSimulationLabel.Content = result;
            }
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
            loadButton.IsEnabled = true;
            saveButton.IsEnabled = true;
            updateReplayLength();
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

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "SQLite files (*.sqlite)|*.sqlite|All files (*.*)|*.*";
                saveDialog.RestoreDirectory = true;
                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(EventsRecorder.FILENAME, saveDialog.FileName, true);
                    writeLog("Replay disimpan ke " + saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                writeLog(ex.ToString());
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.Filter = "SQLite files (*.sqlite)|*.sqlite|All files (*.*)|*.*";
                openDialog.RestoreDirectory = true;
                if (openDialog.ShowDialog() == true)
                {
                    File.Copy(openDialog.FileName, EventsPlayer.FILENAME, true);
                    writeLog("Replay dibaca dari " + openDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                writeLog(ex.ToString());
            }

        }

        private void updateReplayLength()
        {
            try
            {
                long milliseconds = player.getLength();
                long seconds = milliseconds / 1000;
                long minutes = seconds / 60;
                milliseconds %= 1000;
                seconds %= 60;
                replayLengthLabel.Content = String.Format("{0}:{1,2:D2}.{2,3:D3}", minutes, seconds, milliseconds);
            }
            catch (Exception e)
            {
                writeLog(e.ToString());
            }
        }
    }
}