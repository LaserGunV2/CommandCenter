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

        public MapDrawer(Map map, List<Prajurit> prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
        }

        // TODO Deprecated, i guess
        public void updateMap()
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                foreach (Prajurit prajurit in prajurits)
                {
                    // Create a push pin if not yet done
                    if (prajurit.assignedPushPin == null && prajurit.location != null)
                    {
                        prajurit.assignedPushPin = new Pushpin();
                        prajurit.assignedPushPin.Template = setTemplateStatePosture(prajurit);
                        prajurit.assignedPushPin.Location = prajurit.location;
                        ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
                        /* start add textblock to layer */
                        TextBlock tx = setTextBlockToMap(prajurit);
                        MapLayer.SetPosition(tx, prajurit.location);
                        map.Children.Add(tx);
                        /* end add textblock to layer */
                        /* start add ellipse accurcy to layer */
                        Ellipse acc = getAccuracy(prajurit);
                        MapLayer.SetPosition(acc, prajurit.location);
                        map.Children.Add(acc);
                        /* end add ellipse accurcy to layer */
                        map.Children.Add(prajurit.assignedPushPin);
                    }
                    // Update and draw the push pin if available
                    if (prajurit.assignedPushPin != null)
                    {
                        prajurit.assignedPushPin.Location = prajurit.location;
                        prajurit.assignedPushPin.Heading = (180 + prajurit.heading) % 360;
                    }
                }
                // Refresh map, if map is ready.
                if (map.ActualHeight > 0 && map.ActualWidth > 0)
                {
                    map.SetView(map.BoundingRectangle);
                }
            }));
        }

        public void updateMap(Prajurit prajurit)
        {
            map.Dispatcher.InvokeAsync((Action)(() =>
            {
                // Create a push pin if not yet done
                if (prajurit.assignedPushPin == null && prajurit.location != null)
                {
                    prajurit.assignedPushPin = new Pushpin();
 
                    //prajurit.assignedPushPin.Content = standingPrajuritIcon;
                    prajurit.assignedPushPin.Template = setTemplateStatePosture(prajurit);
                    prajurit.assignedPushPin.Location = prajurit.location;
                    ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
                    /* start add textblock to layer */
                    TextBlock tx = setTextBlockToMap(prajurit);
                    MapLayer.SetPosition(tx, prajurit.location);
                    map.Children.Add(tx);
                    /* end add textblock to layer */
                    map.Children.Add(prajurit.assignedPushPin);
                }
                // Update and draw the push pin if available
                if (prajurit.assignedPushPin != null)
                {
                    // Note: Bing Maps fix to force update. Hopefully it would work
                    prajurit.assignedPushPin.Location = new Location(prajurit.location);
                    prajurit.assignedPushPin.Heading = (180 + prajurit.heading) % 360;
                    prajurit.assignedPushPin.Template = setTemplateStatePosture(prajurit);
                    /* start add textblock to layer */
                    var tBlockRemove = map.Children.OfType<TextBlock>().Where(p => ((TextBlock)p).Tag.Equals(prajurit.nomerUrut)).ToList();
                    foreach (var tb in tBlockRemove)
                    {
                        map.Children.Remove(tb);
                    }
                    TextBlock tx = setTextBlockToMap(prajurit);
                    MapLayer.SetPosition(tx, prajurit.location);
                    map.Children.Add(tx);
                    /* end add textblock to layer */
                }
                // Refresh map, if map is ready.
                if (map.ActualHeight > 0 && map.ActualWidth > 0)
                {
                    map.SetView(map.BoundingRectangle);
                }
            }));
        }

        // Show accuracy in map
        public Ellipse getAccuracy(Prajurit prajurit) {
            Ellipse ellipse = new Ellipse();
            ellipse.Fill = new SolidColorBrush(Color.FromArgb(20, 255, 0, 0));
            ellipse.Stroke = Brushes.Red; ;
            ellipse.Height = prajurit.accuracy;
            ellipse.Width = prajurit.accuracy;
            double left = prajurit.location.Latitude - (prajurit.accuracy / 2);
            double top = prajurit.location.Longitude - (prajurit.accuracy*3/4);
            ellipse.Margin = new Thickness(left, top, 0, 0);
            return ellipse;
        }

        public TextBlock setTextBlockToMap(Prajurit prajurit)
        {
            /*Start add text to soldier*/
            TextBlock textBlock = new TextBlock();
            textBlock.Text = prajurit.nomerUrut + "";
            textBlock.Tag = prajurit.nomerUrut;
            /*Check Team A OR B*/
            if (prajurit.group.Equals("A"))
            {
                textBlock.Background = new SolidColorBrush(Colors.Orange);
            }
            else
            {
                textBlock.Background = new SolidColorBrush(Colors.Aqua);
            }
            textBlock.Width = 30;
            textBlock.FontSize = 20;
            /*Position TextBlock*/
            if (prajurit.heading >= 0 && prajurit.heading <= 90)
            {
                textBlock.Margin = new Thickness(-120, 10, 0, 0); //Menghadap kanan atas
            }
            else if (prajurit.heading > 90 && prajurit.heading <= 180)
            {
                textBlock.Margin = new Thickness(-120, -40, 0, 0); //Menghadap kanan bawah
            }
            else if (prajurit.heading > 180 && prajurit.heading <= 270)
            {
                textBlock.Margin = new Thickness(50, -20, 0, 0); //Menghadap kiri bawah
            }
            else if (prajurit.heading > 270 && prajurit.heading <= 360)
            {
                textBlock.Margin = new Thickness(5, 50, 0, 0); //Menghadap kiri atas
            }
            /*End add text to soldier*/
            textBlock.TextAlignment = TextAlignment.Center;
            return textBlock;
        }

        public ControlTemplate setTemplateStatePosture(Prajurit prajurit) {
            ControlTemplate ct = new ControlTemplate();
            if (prajurit.state+""=="NORMAL" && prajurit.posture+""=="STAND")
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinStand"];
            }
            else if (prajurit.state+""=="NORMAL" && prajurit.posture+""=="CRAWL")
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinCrawl"];
            }
            else if (prajurit.state+""==("SHOOT") && prajurit.posture+""==("STAND"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinStandShoot"];
            }
            else if (prajurit.state+""==("SHOOT") && prajurit.posture+""==("CRAWL"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlShoot"];
            }
            else if (prajurit.state+""==("HIT") && prajurit.posture+""==("STAND"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinStandHit"];
            }
            else if (prajurit.state+""==("HIT") && prajurit.posture+""==("CRAWL"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlHit"];
            }
            else if (prajurit.state+""==("DEAD") && prajurit.posture+""==("STAND"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinStandDead"];
            }
            else if (prajurit.state+""==("DEAD") && prajurit.posture+""==("CRAWL"))
            {
                ct = (ControlTemplate)Application.Current.Resources["pushpinCrawlDead"];
            }

            return ct;
        }

        public void showEveryone()
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
        }

        public void clearMap()
        {
            map.Children.Clear();
        }
    }
}
