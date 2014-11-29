using CommandCenter.Model;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CommandCenter.View
{

    public class MapDrawer
    {
        Map map;
        List<Prajurit> prajurits;
        bool checkA;
        bool checkB;

        private double? lastZoomLevel = null;

        public MapDrawer(Map map, List<Prajurit> prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
            this.checkA = false;
            this.checkB = false;
            map.ViewChangeStart += mapViewChangeStart;
            map.ViewChangeEnd += mapViewChangeEnd;
        }

        public void updateMap(Prajurit prajurit)
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                // Create a push pin if not yet done
                if (prajurit.pushpin == null && prajurit.location != null)
                {
                    prajurit.pushpin = new Image();
                    /*Default prajurits and initialization*/
                    prajurit.pushpin.Source = new BitmapImage(new Uri("img/stand-hit.png", UriKind.Relative));
                    prajurit.pushpin.Height = 60;
                    prajurit.pushpin.Width = 60;
                    prajurit.pushpin.RenderTransformOrigin = new Point(0.5, 0.5);
                    prajurit.pushpin.Margin = new Thickness(-prajurit.pushpin.Height / 2, -prajurit.pushpin.Width / 2, 0, 0);
                    
                    prajurit.assignedText = setTextBlockToMap(prajurit);
                    prajurit.assignedAccuracy = createAccuracyCircle(prajurit);
                    updateAccuracyCircle(prajurit);

                    draw(prajurit);
                    if (prajurit.group.Equals("A") && checkA.Equals(false))
                    {
                        updateToHide(prajurit);
                    }
                    else if(prajurit.group.Equals("B") && checkA.Equals(false)) 
                    {
                        updateToHide(prajurit);
                    }
                }
                // Update and draw the push pin if available
                if (prajurit.pushpin != null)
                {
                    // Note: Bing Maps fix to force update. Hopefully it would work
                    String newImg = setPositionPrajurit(prajurit); 
                    prajurit.pushpin.Source = new BitmapImage(new Uri(newImg, UriKind.Relative)); 
                    prajurit.pushpin.RenderTransform = new RotateTransform(prajurit.heading % 360);

                    updateIfExist(prajurit);
                }
                // Refresh map, if map is ready.
                if (map.ActualHeight > 0 && map.ActualWidth > 0)
                {
                    map.SetView(map.BoundingRectangle);
                }
            }));
        }

        private void draw(Prajurit prajurit) {
            ToolTipService.SetToolTip(prajurit.pushpin, prajurit.nama);
            /* start add textblock to layer */
            MapLayer.SetPosition(prajurit.assignedText, prajurit.location);
            map.Children.Add(prajurit.assignedText);
            /* end add textblock to layer */
            /* start add accuracy to layer */
            MapLayer.SetPosition(prajurit.assignedAccuracy, prajurit.location);
            map.Children.Add(prajurit.assignedAccuracy);
            /* end add accuracy to layer */
            /* start add prajurit to layer */
            MapLayer.SetPosition(prajurit.pushpin, prajurit.location);
            map.Children.Add(prajurit.pushpin);
            /* end add prajurit to layer */
        }

        private void updateIfExist(Prajurit prajurit)
        {
            MapLayer.SetPosition(prajurit.pushpin, prajurit.location);
            /* start update textblock and accuracy */
            setPositionText(prajurit);
            MapLayer.SetPosition(prajurit.assignedText, prajurit.location);
            updateAccuracyCircle(prajurit);
            MapLayer.SetPosition(prajurit.assignedAccuracy, prajurit.location);
            /* end update textblock and accuracy */
        }

        private void updateToHide(Prajurit prajurit) {
            prajurit.pushpin.Visibility = Visibility.Hidden;
            MapLayer.SetPosition(prajurit.pushpin, prajurit.location);
            prajurit.assignedText.Visibility = Visibility.Hidden;
            MapLayer.SetPosition(prajurit.assignedText, prajurit.location);
            prajurit.assignedAccuracy.Visibility = Visibility.Hidden;
            MapLayer.SetPosition(prajurit.assignedAccuracy, prajurit.location);
            //MapLayer.SetZIndex(prajurit.pushpin, -1000);
        }

        private void updateToShow(Prajurit prajurit)
        {
            prajurit.pushpin.Visibility = Visibility.Visible;
            MapLayer.SetPosition(prajurit.pushpin, prajurit.location);
            prajurit.assignedText.Visibility = Visibility.Visible;
            MapLayer.SetPosition(prajurit.assignedText, prajurit.location);
            prajurit.assignedAccuracy.Visibility = Visibility.Visible;
            MapLayer.SetPosition(prajurit.assignedAccuracy, prajurit.location);
            //MapLayer.SetZIndex(prajurit.pushpin, -1000);
        }

        // Show accuracy in map
        private Ellipse createAccuracyCircle(Prajurit prajurit) {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(20, 255, 0, 0));
            ellipse.Stroke = Brushes.Red;
            return ellipse;
        }

        // update accuracy in map
        private void updateAccuracyCircle(Prajurit prajurit)
        {
            double meterPerPixel = (Math.Cos(prajurit.location.Latitude * Math.PI / 180) * 2 * Math.PI * 6378137) / (256 * Math.Pow(2, map.ZoomLevel));
            double pixelRadius = prajurit.accuracy / meterPerPixel;
            prajurit.assignedAccuracy.Height = 2 * pixelRadius;
            prajurit.assignedAccuracy.Width = 2 * pixelRadius;
            double left = -(int)pixelRadius;
            double top = -(int)pixelRadius;
            prajurit.assignedAccuracy.Margin = new Thickness(left, top, 0, 0);
        }

        //Start initial textBlock
        public TextBlock setTextBlockToMap(Prajurit prajurit)
        {
            /*Start add text to soldier*/
            TextBlock textBlock = new TextBlock();
            textBlock.Text = prajurit.nomerUrut + "";
            textBlock.Tag = prajurit.nomerUrut;
            textBlock.Background = new SolidColorBrush(convertCharToColor(prajurit.group[0]));
            textBlock.Width = 30;
            textBlock.FontSize = 20;
            /*Position TextBlock*/
            int quadrant = (int)prajurit.heading / 90;
            if (prajurit.heading == 0)
            {
                textBlock.Margin = new Thickness(-120, 0, 0, 0); //Menghadap kanan atas
            }
            else if (prajurit.heading == 1)
            {
                textBlock.Margin = new Thickness(-120, -40, 0, 0); //Menghadap kanan bawah
            }
            else if (prajurit.heading == 2)
            {
                textBlock.Margin = new Thickness(50, -20, 0, 0); //Menghadap kiri bawah
            }
            else if (prajurit.heading == 3)
            {
                textBlock.Margin = new Thickness(5, 50, 0, 0); //Menghadap kiri atas
            }
            /*End add text to soldier*/
            textBlock.TextAlignment = TextAlignment.Center;
            return textBlock;
        }

        //update position textBlock
        public void setPositionText(Prajurit p)
        {
            int quadrant = (int)p.heading / 90;
            if (p.heading == 0)
            {
                p.assignedText.Margin = new Thickness(-120, 0, 0, 0); //Menghadap kanan atas
            }
            else if (p.heading == 1)
            {
                p.assignedText.Margin = new Thickness(-120, -40, 0, 0); //Menghadap kanan bawah
            }
            else if (p.heading == 2)
            {
                p.assignedText.Margin = new Thickness(50, -20, 0, 0); //Menghadap kiri bawah
            }
            else if (p.heading == 3)
            {
                p.assignedText.Margin = new Thickness(5, 50, 0, 0); //Menghadap kiri atas
            }
        }

        //set image prajurit
        public String setPositionPrajurit(Prajurit prajurit){
            String img = "";
            switch (prajurit.posture)
            {
                case Prajurit.Posture.STAND:
                    if (prajurit.state == Prajurit.State.NORMAL)
                    {
                        img = "img/stand.png";
                    }
                    else if (prajurit.state == Prajurit.State.SHOOT)
                    {
                        img = "img/stand-shoot.png";
                    }
                    else if (prajurit.state == Prajurit.State.HIT)
                    {
                        img = "img/stand-hit.png";
                    }
                    else if (prajurit.state == Prajurit.State.DEAD)
                    {
                        img = "img/stand-dead.png";
                    }
                    break;
                case Prajurit.Posture.CRAWL:
                    if (prajurit.state == Prajurit.State.NORMAL)
                    {
                        img = "img/crawl.png";
                    }
                    else if (prajurit.state == Prajurit.State.SHOOT)
                    {
                        img = "img/crawl-shoot.png";
                    }
                    else if (prajurit.state == Prajurit.State.HIT)
                    {
                        img = "img/crawl-hit.png";
                    }
                    else if (prajurit.state == Prajurit.State.DEAD)
                    {
                        img = "img/crawl-dead.png";
                    }
                    break;
            }
            return img;
        }

        public ControlTemplate setTemplateStatePosture(Prajurit prajurit) {
            ControlTemplate ct = new ControlTemplate();
            switch (prajurit.posture)
            { 
                case Prajurit.Posture.STAND:
                    if (prajurit.state == Prajurit.State.NORMAL)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinStand"];
                    }
                    else if (prajurit.state == Prajurit.State.SHOOT)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinStandShoot"];
                    }
                    else if (prajurit.state == Prajurit.State.HIT) 
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinStandHit"];
                    }
                    else if (prajurit.state == Prajurit.State.DEAD)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinStandDead"];
                    }
                    break;
                case Prajurit.Posture.CRAWL:
                    if (prajurit.state == Prajurit.State.NORMAL)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinCrawl"];
                    }
                    else if (prajurit.state == Prajurit.State.SHOOT)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlShoot"];
                    }
                    else if (prajurit.state == Prajurit.State.HIT) 
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlHit"];
                    }
                    else if (prajurit.state == Prajurit.State.DEAD)
                    {
                        ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlDead"];
                    }
                    break;
            }

            return ct;
        }

        public void showEveryone()
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                LocationCollection locations = new LocationCollection();
                foreach (Prajurit prajurit in prajurits)
                {
                    if (prajurit.location != null)
                    {
                        locations.Add(prajurit.location);
                    }
                }
                LocationRect bounds = new LocationRect(locations);
                map.SetView(bounds);
                map.ZoomLevel--;
            }));
        }

        public void clearMap()
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                map.Children.Clear();
            }));
        }

        private Color convertCharToColor(char c)
        {
            int c2 = (int)c % 8;
            return Color.FromRgb((byte)(127 + (c2 / 4) * 128), (byte)(127 + ((c2 / 2) % 2) * 128), (byte)(127 + (c2 % 2) * 128));
        }

        private void mapViewChangeStart(object sender, MapEventArgs e)
        {
            lastZoomLevel = map.ZoomLevel;
        }

        private void mapViewChangeEnd(object sender, MapEventArgs e)
        {
            if (lastZoomLevel != null && !lastZoomLevel.Equals(map.ZoomLevel))
            {
                foreach (Prajurit prajurit in prajurits) {
                    updateMap(prajurit);
                }
            }
            lastZoomLevel = null;
        }

        private void checkTim(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            MessageBox.Show(check.IsChecked.Value.ToString());
        }

        public void updateVisibility() {
            foreach (Prajurit prajurit in prajurits)
            {
                if (prajurit.group.Equals("A"))
                {
                    if (checkA.Equals(true))
                    {
                        updateToShow(prajurit);
                    }
                    else 
                    {
                        updateToHide(prajurit);
                    }
                }
                else if (prajurit.group.Equals("B"))
                {
                    if (checkB.Equals(true))
                    {
                        updateToShow(prajurit);
                    }
                    else
                    {
                        updateToHide(prajurit);
                    }
                }
            }
        }

        public void setVisibility(bool checkBoxA, bool checkBoxB)
        {
            this.checkA = checkBoxA;
            this.checkB = checkBoxB;
        }
    }
}
