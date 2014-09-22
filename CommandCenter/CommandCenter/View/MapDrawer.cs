﻿using CommandCenter.Model;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Ellipse standingPrajuritIcon, crawlingPrajuritIcon;

        public MapDrawer(Map map, List<Prajurit> prajurits)
        {
            this.map = map;
            this.prajurits = prajurits;
            standingPrajuritIcon = new Ellipse()
            {
                Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("img/stand.png", UriKind.Relative)) },
                Height = 20,
                Width = 20,
            };

            crawlingPrajuritIcon = new Ellipse()
            {
                Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("img/crawl.png", UriKind.Relative)) },
                Height = 20,
                Width = 20,
            };
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
                        Uri imgUri = new Uri("img/soldier.png", UriKind.Relative);
                        BitmapImage imgSourceR = new BitmapImage(imgUri);
                        ImageBrush imgBrush = new ImageBrush() { ImageSource = imgSourceR };
                        prajurit.assignedPushPin.Background = new SolidColorBrush(Colors.Red);
                        prajurit.assignedPushPin.Content = standingPrajuritIcon;
                        prajurit.assignedPushPin.Location = prajurit.location;
                        ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
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
                    prajurit.assignedPushPin.Background = new SolidColorBrush(Colors.Red);
                    prajurit.assignedPushPin.Content = standingPrajuritIcon;
                    prajurit.assignedPushPin.Location = prajurit.location;
                    ToolTipService.SetToolTip(prajurit.assignedPushPin, prajurit.nama);
                    map.Children.Add(prajurit.assignedPushPin);
                }
                // Update and draw the push pin if available
                if (prajurit.assignedPushPin != null)
                {
                    // Note: Bing Maps fix to force update. Hopefully it would work
                    prajurit.assignedPushPin.Location = new Location(prajurit.location);
                    prajurit.assignedPushPin.Heading = (180 + prajurit.heading) % 360;
                    prajurit.assignedPushPin.Content = prajurit.posture != null && prajurit.posture.Equals("crawl") ? crawlingPrajuritIcon : standingPrajuritIcon;
                }
                // Refresh map, if map is ready.
                if (map.ActualHeight > 0 && map.ActualWidth > 0)
                {
                    map.SetView(map.BoundingRectangle);
                }
            }));
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
