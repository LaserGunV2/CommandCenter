using CommandCenter.Controller;
using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using NLog;
using System.Windows.Markup;

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
        public EventsRecorder recorder;
        private LiveGameController liveGameController;
        private ReplayGameController replayController;
        private WatchGameController watchController;
        public PrajuritDatabase prajuritDatabase;

        public double playSpeed = 1;
        public bool skipRegistration = true;

        private Logger logger;

        public MainWindow()
        {
            //Change locale US (make sure language in application)    
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            XmlLanguage lang = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(lang));
            FrameworkContentElement.LanguageProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement), new FrameworkPropertyMetadata(lang));

            InitializeComponent();

            logger = LogManager.GetCurrentClassLogger();

            prajuritDatabase = new PrajuritDatabase();
            prajurits = new List<Prajurit>();
            pesertaDataGrid.DataContext = prajurits;
            senjatas = new Dictionary<int, Senjata>();

            recorder = new EventsRecorder();
            liveGameController = new LiveGameController(this);
            replayController = new ReplayGameController(this);
            watchController = new WatchGameController(this);

            mapDrawer = new MapDrawer(map, prajurits);
            mapDrawer.setVisibility(showACheckBox.IsChecked.Value, showBCheckBox.IsChecked.Value); //check checkbox Tim
        }

        private void pendaftaranButton_Click(object sender, RoutedEventArgs e)
        {
            int initialAmmo;
            // Validate potentially erroneus user inputs
            try
            {
                initialAmmo = Int32.Parse(ammoTextBox.Text);
            }
            catch (FormatException)
            {
                showError("Isi jumlah peluru dengan angka!");
                return;
            }

            String result = liveGameController.startRegistration(initialAmmo);
            if (result != null)
            {
                pendaftaranButton.IsEnabled = false;
                mulaiButton.IsEnabled = true;
                akhiriButton.IsEnabled = true;
                saveButton.IsEnabled = false;
                setActiveTab(latihanTabItem);
                ammoTextBox.IsEnabled = false;
                replayLengthLabel.Content = "0:00.000";

                // Start controller and start listening
                idSimulationLabel.Content = result;
            }
        }

        private void mulaiButton_Click(object sender, RoutedEventArgs e)
        {
            //Check idSenjata
            Boolean idSenjataCheck = true;
            foreach (Prajurit prajurit in prajurits)
            { 
                if(prajurit.senjata==null){
                    MessageBoxResult result = MessageBox.Show("Belum prajurit memiliki senjata. Apakah ingin melanjutkan?", "Konfirmasi", MessageBoxButton.YesNo);
                    idSenjataCheck = result == MessageBoxResult.Yes;
                    break;
                }
            }
            if (idSenjataCheck==true)
            {
                //check checkbox value
                mapDrawer.setVisibility(showACheckBox.IsChecked.Value, showBCheckBox.IsChecked.Value);

                prajuritDatabase.saveNamesToDatabase(prajurits);
                pendaftaranButton.IsEnabled = false;
                mulaiButton.IsEnabled = false;
                akhiriButton.IsEnabled = true;
                pesertaDataGrid.IsEnabled = false;

                liveGameController.startExercise();    
            }
            
        }

        private void akhiriButton_Click(object sender, RoutedEventArgs e)
        {
            prajuritDatabase.saveNamesToDatabase(prajurits);
            idSimulationLabel.Content = "###";
            liveGameController.stopExercise(true);

            pendaftaranButton.IsEnabled = true;
            mulaiButton.IsEnabled = false;
            akhiriButton.IsEnabled = false;
            saveButton.IsEnabled = true;
            pesertaDataGrid.IsEnabled = true;
            setActiveTab(null);
            ammoTextBox.IsEnabled = true;
            updateReplayLength();
        }

        public void writeLog(LogLevel level, String s)
        {
            logger.Log(level, s);
            Dispatcher.Invoke((Action)(() =>
            {
                if ((bool)peristiwaCheckBox.IsChecked)
                {
                    peristiwaTextBlock.Text = s + "\n" + peristiwaTextBlock.Text;
                }
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            liveGameController.stopExercise(true);
            replayController.stopPlayback();
            watchController.stopExercise(true);
            prajuritDatabase.closeConnection();
            EventsRecorder.closeConnection();
        }

        public void refreshTable()
        {
            pesertaDataGrid.Dispatcher.Invoke((Action)(() =>
            {
                pesertaDataGrid.Items.Refresh();
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
                    writeLog(LogLevel.Info, "Replay disimpan ke " + saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                writeLog(LogLevel.Error, ex.ToString());
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
                    EventsRecorder.loadFrom(openDialog.FileName);
                    updateReplayLength();
                    playButton.IsEnabled = true;
                    tabControl.SelectedIndex = 1;
                    writeLog(LogLevel.Info, "Replay dibaca dari " + openDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                writeLog(LogLevel.Error, ex.ToString());
            }

        }

        private void updateReplayLength()
        {
            try
            {
                long milliseconds = recorder.getRecordingLength();
                replayProgressBar.Maximum = 1e-3 * milliseconds;
                replayProgressBar.Value = 0;
                long seconds = milliseconds / 1000;
                long minutes = seconds / 60;
                milliseconds %= 1000;
                seconds %= 60;
                replayLengthLabel.Content = String.Format("{0}:{1,2:D2}.{2,3:D3}", minutes, seconds, milliseconds);
            }
            catch (Exception e)
            {
                writeLog(LogLevel.Error, e.ToString());
            }
        }

        public void updateReplayProgress(double progress, string suffix)
        {
            long milliseconds = (int)(progress * 1000);
            long seconds = milliseconds / 1000;
            long minutes = seconds / 60;
            milliseconds %= 1000;
            seconds %= 60;
            string content = String.Format("{0}:{1,2:D2}.{2,3:D3}", minutes, seconds, milliseconds);
            if (suffix != null)
            {
                content += " " + suffix;
            }
            
            replayProgressBar.Dispatcher.Invoke((Action)(() =>
            {
                replayProgressBar.Value = progress;
            }));
            replayProgressLabel.Dispatcher.Invoke((Action)(() =>
            {
                replayProgressLabel.Content = content;
            }));
        }

        public void updateReplayProgress(double progress)
        {
            updateReplayProgress(progress, null);
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            replayController.startPlayback();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            replayController.stopPlayback();
        }

        public void setReplayingEnabled(bool isReplaying)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                ammoTextBox.IsEnabled = !isReplaying;
                loadButton.IsEnabled = !isReplaying;
                playButton.IsEnabled = !isReplaying;
                stopButton.IsEnabled = isReplaying;
                playSpeedComboBox.IsEnabled = !isReplaying;
                skipRegistrationCheckBox.IsEnabled = !isReplaying;
                setActiveTab(isReplaying ? replayTabItem : null);
            }));     
        }

        public void setWatchingEnabled(bool isWatching)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                pantauIdLatihanTextBox.IsEnabled = !isWatching;
                pantauLatihanButton.IsEnabled = !isWatching;
                stopPantauButton.IsEnabled = isWatching;
                setActiveTab(isWatching ? pantauTabItem : null);
            }));     
        }

        private void playSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedValue = (e.AddedItems[0] as ComboBoxItem).Content as string;
            if (selectedValue.Length != 0)
            {
                CultureInfo culture = new CultureInfo("en-US");
                playSpeed = Double.Parse(selectedValue.Substring(0, selectedValue.Length - 1), culture.NumberFormat);
            }
        }

        private void skipRegistrationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            skipRegistration = true;
        }

        private void skipRegistrationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            skipRegistration = false;
        }

        private void setActiveTab(TabItem activeTab)
        {
            if (activeTab == null)
            {
                latihanTabItem.IsEnabled = true;
                replayTabItem.IsEnabled = true;
                pantauTabItem.IsEnabled = true;
            }
            else
            {
                latihanTabItem.IsEnabled = (activeTab == latihanTabItem);
                replayTabItem.IsEnabled = (activeTab == replayTabItem);
                pantauTabItem.IsEnabled = (activeTab == pantauTabItem);
            }
        }

        private void pantauLatihanButton_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^\d\d\d$");
            if (regex.Match(pantauIdLatihanTextBox.Text).Success)
            {
                watchController.watchExercise(pantauIdLatihanTextBox.Text);
            }
            else
            {
                showError("Isi ID latihan dengan tiga dijit angka!");
            }
        }

        private void stopPantauButton_Click(object sender, RoutedEventArgs e)
        {
            watchController.stopExercise(true);
        }

		public void setVisible(object sender, RoutedEventArgs e)
        {
            mapDrawer.setVisibility(showACheckBox.IsChecked.Value, showBCheckBox.IsChecked.Value);
            mapDrawer.updateVisibility();
        }

        private void CharValidationTextBox(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals('A') || e.Key.Equals('B'))
            {
                e.Handled = true;
            }
        }

        public void showError(string message)
        {
            MessageBox.Show(message, "Kesalahan!");
        }
    }
}