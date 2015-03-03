# Modul-Modul Command Center

Dokumen ini berisi pemetaan antara modul-modul yang direncanakan di awal dengan
implementasinya.

## Software Server Group A (PC)

Bagian ini berfungsi menampilkan prajurit-prajurit regu A.
Server Group A dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.

### Data Selector A

Modul ini berfungsi untuk memantau dan memilih prajurit mana yang akan
ditampilkan pada peta. Kelas yang bertanggung jawab untuk modul ini adalah
kelas `WatchGameController`:

```cs
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.Controller
{
  class WatchGameController : AbstractGameController
  {
    WatchSilentUDPCommunication modifiedCommunication;
    public WatchGameController(MainWindow parent)
      : base(parent, new WatchSilentUDPCommunication(parent), new WatchSilentEventsRecorder())
    {
      modifiedCommunication = (WatchSilentUDPCommunication)this.communication;
    }

    public void watchExercise(String gameId)
    {
      JSONPacket packet = new JSONPacket("pantau/register");
      this.gameId = gameId;
      packet.setParameter("gameid", gameId);
      modifiedCommunication.broadcast(packet);
      modifiedCommunication.listenWatchConfirmationAsync(this);
      parent.setWatchingEnabled(true);
    }

    public override void stopExercise(bool force)
    {
      base.stopExercise(force);
      modifiedCommunication.stopListenWatchConfirmationAsync();
      parent.setWatchingEnabled(false);
    }
  }

  class WatchSilentUDPCommunication : UDPCommunication
  {
    private Thread watchConfirmationThread = null;

    public WatchSilentUDPCommunication(MainWindow parent)
      : base(parent)
    {
      this.parent = parent;
    }

    public override void send(IPAddress address, JSONPacket outPacket)
    {
      string sendString = outPacket.ToString();
      this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
    }

    public void broadcast(JSONPacket outPacket)
    {
      NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (NetworkInterface Interface in Interfaces)
      {
        if (Interface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
        if (Interface.OperationalStatus != OperationalStatus.Up) continue;
        Console.WriteLine(Interface.Description);
        UnicastIPAddressInformationCollection UnicastIPInfoCol = Interface.GetIPProperties().UnicastAddresses;
        HashSet<string> broadcastAddresses = new HashSet<string>();
        foreach (UnicastIPAddressInformation UnicatIPInfo in UnicastIPInfoCol)
        {
          byte[] ipAdressBytes = UnicatIPInfo.Address.GetAddressBytes();
          if (ipAdressBytes.Length > 4)
          {
            continue;
          }
          byte[] subnetMaskBytes = UnicatIPInfo.IPv4Mask.GetAddressBytes();
          byte[] broadcastAddress = new byte[ipAdressBytes.Length];
          for (int i = 0; i < broadcastAddress.Length; i++)
          {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255)); // OR ip address dengan subnet
          }
          String ipBroadcastStr = "";
          for (int i = 0; i < broadcastAddress.Length; i++) //for print only
          {
            ipBroadcastStr += broadcastAddress[i] + ".";
          }
          broadcastAddresses.Add(ipBroadcastStr.Substring(0, ipBroadcastStr.Length - 1));
        }
        foreach (string addressStr in broadcastAddresses) {
          IPAddress ipBroadcast = IPAddress.Parse(addressStr);
          base.send(ipBroadcast, outPacket, UDPCommunication.IN_PORT);
          parent.writeLog(LogLevel.Info, "Broadcast ke " + ipBroadcast.ToString());

        }
      }
    }

    private void listenWatchConfirmation()
    {
      UdpClient client = null;
      try
      {
        client = new UdpClient(IN_PORT);
        client.Client.ReceiveTimeout = 1000;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
          try
          {
            byte[] receivedBytes = client.Receive(ref endPoint);
            parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
            JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
            if (inPacket.getParameter("type").Equals("pantau/confirm"))
            {
              if (inPacket.getParameter("status").Equals("ok") && inPacket.getParameter("gameid").Equals(controller.gameId))
              {
                String gameId = inPacket.getParameter("gameid");
                int ammo = Int32.Parse(inPacket.getParameter("ammo"));
                client.Close();
                this.controller.startRegistration(gameId, ammo);
              }
              else
              {
                client.Close();
                parent.showError("Pantau ditolak: " + inPacket.getParameter("status"));
                parent.setWatchingEnabled(false);
              }
              return;
            }
            else
            {
              parent.writeLog(LogLevel.Warn, "Paket diabaikan karena belum memantau: " + inPacket);
            }
          }
          catch (SocketException)
          {
            // void
          }
          catch (ThreadAbortException)
          {
            client.Close();
            return;
          }
        }
      }
      catch (ThreadAbortException)
      {
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public void listenWatchConfirmationAsync(WatchGameController controller)
    {
      this.controller = controller;
      watchConfirmationThread = new Thread(listenWatchConfirmation);
      watchConfirmationThread.Start();
    }

    public void stopListenWatchConfirmationAsync()
    {
      if (watchConfirmationThread != null)
      {
        watchConfirmationThread.Abort();
      }
    }
  }

  class WatchSilentEventsRecorder : EventsRecorder
  {
    public override void startRecording()
    {
      // silenced
    }

    public override void record(IPAddress sender, string eventText)
    {
      // silenced
    }

    public override void stopRecording()
    {
      // silenced
    }

    public override void setProperty(string name, string value)
    {
      // silenced
    }
  }
}
```

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Senjata
  {
    public int idSenjata;
    public Prajurit owner;
    public int initialCounter, currentCounter, initialAmmo;

    public Senjata(int idSenjata, Prajurit owner, int counter, int initialAmmo)
    {
      this.idSenjata = idSenjata;
      this.owner = owner;
      this.initialCounter = counter;
      this.currentCounter = counter;
      this.initialAmmo = initialAmmo;
    }

    public int getRemainingAmmo()
    {
      return (initialCounter + initialAmmo - currentCounter);
    }

    override public String ToString()
    {
      return "#" + idSenjata + " / " + getRemainingAmmo();
    }
  }
}
```

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, dikirimkan oleh
software android dan direpresentasikan dalam kelas `JSONPacket`:

```cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
  public class JSONPacket
  {
    private Dictionary<string, string> parameters = new Dictionary<string,string>();

    public JSONPacket(string type)
    {
      parameters.Add("type", type);
    }

    protected JSONPacket()
    {
      // void
    }

    public static JSONPacket createFromJSONBytes(byte[] jsonBytes)
    {
      JSONPacket packet = new JSONPacket();
      string jsonString = Encoding.UTF8.GetString(jsonBytes);
      packet.parameters = JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonString);
      return packet;
    }

    public void setParameter(string name, string value)
    {
      parameters[name] = value;
    }

    public string getParameter(string name)
    {
      return parameters[name];
    }

    public override string ToString()
    {
      return JsonConvert.SerializeObject(parameters);
    }

    public byte[] toBytes()
    {
      return Encoding.UTF8.GetBytes(ToString());
    }
  }
}
```

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama. Untuk mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut, dibutuhkan kelas `MapDrawer`:

```cs
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
      map.Dispatcher.Invoke((Action)(() =>
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
      p.assignedText.Background = new SolidColorBrush(convertCharToColor(p.group[0]));
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
      map.Dispatcher.Invoke((Action)(() =>
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
      map.Dispatcher.Invoke((Action)(() =>
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
```

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `LiveGameController`:

```cs
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using NLog;

namespace CommandCenter.Controller
{
  class LiveGameController : AbstractGameController
  {

    public LiveGameController(MainWindow parent)
      : base(parent, new UDPCommunication(parent), parent.recorder)
    {
      // void
    }

    public String startRegistration(int initialAmmo)
    {
      Random random = new Random();
      String gameId = "";
      for (int i = 0; i < 3; i++)
      {
        gameId += random.Next(10);
      }

      return startRegistration(gameId, initialAmmo);
    }

    public override void handlePacket(IPAddress address, JSONPacket inPacket, bool updateUI)
    {
      base.handlePacket(address, inPacket, updateUI);
      if (!inPacket.getParameter("type").StartsWith("pantau/"))
      {
        foreach (IPAddress watcher in watchers)
        {
          parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + inPacket);
          communication.send(watcher, inPacket, UDPCommunication.IN_PORT);
        }
      }
    }

    public override void startExercise()
    {
      base.startExercise();
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        string teamGroups = "";
        for (int i = 0; i < prajurits.Count; i++)
        {
          teamGroups += prajurits[i].group;
        }
        packet.setParameter("state", "START/" + teamGroups);
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }

    public override void stopExercise(bool force)
    {
      base.stopExercise(force);
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        packet.setParameter("state", "STOP");
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }
  }
}
```

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Player

Modul ini mencatat koordinat prajurit, arah pandang, status (mati/hidup,
tiarap/berdiri), nomer urut, nomer induk, nama, regu, dll.

Pada software, prajurit direpresentasikan pada kelas `Prajurit`:

```cs
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CommandCenter.Model
{
  public class Prajurit
  {

    public enum State { NORMAL, SHOOT, HIT, DEAD }
    public enum Posture { STAND, CRAWL }

    public IPAddress ipAddress;
    public int nomerUrut { get; set; }
    public string nomerInduk { get; set; }
    public string nama {get; set; }
    public Location location { get; set; }
    public double heading { get; set; }
    public string group { get; set; }
    public DateTime lastUpdate { get; set; }
    public Senjata senjata { get; set; }
    public State state { get; set; }
    public Posture posture { get; set; }
    public int accuracy { get; set; }

    public Image pushpin = null;
    public TextBlock assignedText = null;
    public Ellipse assignedAccuracy = null;

    public static List<string> GROUPS_AVAILABLE = new List<string>() { "A", "B" };

    public Prajurit(int nomerUrut, string nomerInduk, IPAddress ipAddress, string group, Location location)
    {
      this.nomerUrut = nomerUrut;
      this.nomerInduk = nomerInduk;
      this.ipAddress = ipAddress;
      this.group = group;
      this.posture = (Posture)0;
      this.state = (State)0;
      this.accuracy = 200; //sementara - test doang
      if (location == null)
      {
        this.location = null;
      }
      else
      {
        this.location = location;
      }
      lastUpdate = DateTime.Now;
    }

    public static int findPrajuritIndexByNomerInduk(List<Prajurit> prajurits, String nomerInduk)
    {
      for (int i = 0; i < prajurits.Count; i++)
      {
        if (prajurits[i].nomerInduk.Equals(nomerInduk))
        {
          return i;
        }
      }
      return -1;
    }

    public void setLocation(String locationString)
    {
      string[] latlon = locationString.Split(',');
      if (location == null)
      {
        location = new Location();
      }
      location.Latitude = Double.Parse(latlon[0]);
      location.Longitude = Double.Parse(latlon[1]);
      lastUpdate = DateTime.Now;
    }
  }
}
```

### Recorder

Modul ini berfungsi untuk mencatat hal-hal yang terjadi di lapangan,
direpresentasikan dalam kelas `EventsRecorder`:

```cs
using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Events
{
  public class EventsRecorder
  {
    public const string REGISTER = "REGISTER";
    public const string START = "START";
    public const string STOP = "STOP";
    public const string PROP_GAMEID = "gameId";
    public const string PROP_AMMO = "ammo";
    public const string FILENAME = "events.internal-sqlite";
    protected const int COMMIT_PERIOD = 100;

    SQLiteDataReader reader;
    SQLiteTransaction transaction;
    Stopwatch stopwatch;
    protected int commitCounter;

    public EventsRecorder()
    {
    }

    public virtual void startRecording()
    {
      ConnectionSingleton.getInstance().resetDatabase();
      stopwatch = new Stopwatch();
      stopwatch.Start();
      commitCounter = 0;
      transaction = ConnectionSingleton.getInstance().connection.BeginTransaction();
      record(null, REGISTER);
    }

    public virtual void record(IPAddress sender, string eventText)
    {
      long timeOffset = stopwatch.ElapsedMilliseconds;
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
      command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
      command.Parameters.AddWithValue("@SENDER", sender);
      command.Parameters.AddWithValue("@PACKET", eventText);
      int returnValue = command.ExecuteNonQuery();
      if (returnValue != 1)
      {
        throw new Exception("Warning: event not inserted + " + command.ToString());
      }
      commitCounter++;
      if (commitCounter > COMMIT_PERIOD)
      {
        commitCounter = 0;
        transaction.Commit();
        transaction = connection.BeginTransaction();
      }
    }

    public virtual void stopRecording()
    {
      record(null, STOP);
      transaction.Commit();
    }

    public virtual void setProperty(string name, string value)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
      command.Parameters.AddWithValue("@NAME", name);
      command.Parameters.AddWithValue("@VALUE", value);
      command.ExecuteNonQuery();
    }

    public string getProperty(string name)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT value FROM properties WHERE name=@NAME", connection);
      command.Parameters.AddWithValue("@NAME", name);
      return (string)command.ExecuteScalar();
    }

    public Int64 getRecordingLength()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
      Int64 length = (Int64)command.ExecuteScalar();
      return length;
    }

    public void startReplaying()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection);
      reader = command.ExecuteReader();
    }

    public Event getNextPlayEvent()
    {
      if (reader.Read())
      {
        Event newEvent = new Event();
        newEvent.timeOffset = (Int64)reader["timeOffset"];
        newEvent.sender = reader["sender"] is DBNull ? null : IPAddress.Parse((string)reader["sender"]);
        newEvent.packet = reader["packet"] is DBNull ? null : (string)reader["packet"];
        return newEvent;
      }
      else
      {
        return null;
      }
    }

    public void stopReplaying()
    {
      // void
    }

    public static void loadFrom(string filename)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteConnection connection2 = new SQLiteConnection("Data Source=" + filename + "; Version=3;");
      connection2.Open();
      ConnectionSingleton.getInstance().resetDatabase();
      new SQLiteCommand("BEGIN", connection).ExecuteNonQuery();
      SQLiteCommand command2 = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection2);
      SQLiteDataReader reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
        command.Parameters.AddWithValue("@TIMEOFFSET", reader2["timeOffset"]);
        command.Parameters.AddWithValue("@SENDER", reader2["sender"]);
        command.Parameters.AddWithValue("@PACKET", reader2["packet"]);
        command.ExecuteNonQuery();
      }
      command2 = new SQLiteCommand("SELECT name, value FROM properties", connection2);
      reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
        command.Parameters.AddWithValue("@NAME", reader2["name"]);
        command.Parameters.AddWithValue("@VALUE", reader2["value"]);
        command.ExecuteNonQuery();
      }
      new SQLiteCommand("COMMIT", connection).ExecuteNonQuery();
      connection2.Close();
    }

    public static void closeConnection()
    {
      ConnectionSingleton.getInstance().connection.Close();
    }
  }

  class ConnectionSingleton
  {
    protected const string FILENAME = EventsRecorder.FILENAME;
    public SQLiteConnection connection;
    protected static ConnectionSingleton instance = null;

    protected ConnectionSingleton() {
      SQLiteConnection.CreateFile(FILENAME);
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE events (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("CREATE TABLE properties (name TEXT PRIMARY KEY UNIQUE, value TEXT)", connection);
      command.ExecuteNonQuery();
    }

    public static ConnectionSingleton getInstance()
    {
      if (instance == null)
      {
        instance = new ConnectionSingleton();
      }
      return instance;
    }

    public void resetDatabase()
    {
      SQLiteCommand command = new SQLiteCommand("DELETE FROM events", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("DELETE FROM properties", connection);
      command.ExecuteNonQuery();
    }
  }
}
```

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`:

```cs
using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CommandCenter.Controller
{
  class ReplayGameController : AbstractGameController
  {

    Timer eventTimer, heartbeatTimer;
    EventsRecorder player;
    Stopwatch stopwatch;
    Event scheduledEvent;
    long skippedMilliseconds;

    public ReplayGameController(MainWindow parent)
      : base(parent, new ReplaySilentUDPCommunication(parent), new ReplaySilentEventsRecorder())
    {
      stopwatch = new Stopwatch();
      eventTimer = new Timer();
      eventTimer.Elapsed += OnEventTimedEvent;
      heartbeatTimer = new Timer();
      heartbeatTimer.Elapsed += OnHeartbeatTimedEvent;
      player = new EventsRecorder();
    }

    public void startPlayback()
    {
      player.startReplaying();
      stopwatch.Restart();
      parent.updateReplayProgress(0);
      scheduledEvent = null;
      executePacketAndScheduleNext();
      eventTimer.Enabled = true;
      skippedMilliseconds = 0;
      heartbeatTimer.Interval = 1000 / parent.playSpeed;
      heartbeatTimer.Enabled = true;
      parent.setReplayingEnabled(true);
    }

    public void stopPlayback()
    {
      eventTimer.Enabled = false;
      heartbeatTimer.Enabled = false;
      stopwatch.Stop();
      player.stopReplaying();
      scheduledEvent = null;
      this.state = State.IDLE;
      parent.setReplayingEnabled(false);
    }

    private void executePacketAndScheduleNext()
    {
      eventTimer.Enabled = false;
      bool updateUI = !(parent.skipRegistration && state == State.REGISTRATION);
      if (scheduledEvent != null)
      {
        if (updateUI)
        {
          parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
        }
        if (scheduledEvent.packet.Equals(EventsRecorder.REGISTER))
        {
          startRegistration(player.getProperty(EventsRecorder.PROP_GAMEID), Int32.Parse(player.getProperty(EventsRecorder.PROP_AMMO)));
        }
        else if (scheduledEvent.packet.StartsWith(EventsRecorder.START))
        {
          string[] tokens = scheduledEvent.packet.Split('/');
          for (int i = 0; i < tokens[1].Length; i++)
          {
            prajurits[i].group = "" + tokens[1][i];
          }
          startExercise();
        }
        else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
        {
          stopExercise(true);
        }
        else
        {
          parent.writeLog(LogLevel.Info, "Pura-pura terima dari " + scheduledEvent.sender + ": " + scheduledEvent.packet);
          parent.pesertaDataGrid.Dispatcher.Invoke((Action)(() =>
          {
            this.handlePacket(scheduledEvent.sender, JSONPacket.createFromJSONBytes(Encoding.UTF8.GetBytes(scheduledEvent.packet)), updateUI);
          }));      

        }
      }
      Event nextEvent = player.getNextPlayEvent();
      if (nextEvent != null)
      {
        scheduledEvent = nextEvent;
        long currentTime = (long)((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed);
        long interval = (long)((nextEvent.timeOffset - currentTime) / parent.playSpeed);
        if (parent.skipRegistration && state == State.REGISTRATION)
        {
          skippedMilliseconds += interval;
          interval = 0;
        }
        eventTimer.Interval = interval <= 0 ? 1 : interval;
        eventTimer.Enabled = true;
      }
      else
      {
        stopPlayback();
      }
    }

    private void OnEventTimedEvent(Object source, ElapsedEventArgs e)
    {
      executePacketAndScheduleNext();
    }

    private void OnHeartbeatTimedEvent(Object source, ElapsedEventArgs e)
    {
      if (parent.skipRegistration && state == State.REGISTRATION)
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed), "(>>)");
      }
      else
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
      }
    }
  }

  class ReplaySilentUDPCommunication : UDPCommunication
  {
    public ReplaySilentUDPCommunication(MainWindow parent) : base(parent)
    {
      this.parent = parent;
    }

    public override void listenAsync(AbstractGameController controller)
    {
      // void
    }

    public override void send(IPAddress address, JSONPacket outPacket)
    {
      string sendString = outPacket.ToString();
      this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
    }
  }

  class ReplaySilentEventsRecorder : EventsRecorder
  {
    public override void startRecording()
    {
      // silenced
    }

    public override void record(IPAddress sender, string eventText)
    {
      // silenced
    }

    public override void stopRecording()
    {
      // silenced
    }

    public override void setProperty(string name, string value)
    {
      // silenced
    }
  }
}
```

### Data Interface

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication`:

```cs
using CommandCenter.Controller;
using CommandCenter.Model.Protocol;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.View
{
  public class UDPCommunication
  {
    public const int IN_PORT = 21500;
    public const int OUT_PORT = 21501;

    protected MainWindow parent;
    protected AbstractGameController controller = null;
    private bool softAbort;
    Thread thread = null;

    public UDPCommunication(MainWindow parent)
    {
      this.parent = parent;
    }

    private void listen()
    {
      UdpClient client = null;
      try
      {
        client = new UdpClient(IN_PORT);
        client.Client.ReceiveTimeout = 1000;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        softAbort = false;
        while (!softAbort)
        {
          try
          {
            byte[] receivedBytes = client.Receive(ref endPoint);
            parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
            JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
            controller.handlePacket(endPoint.Address, inPacket, true);
          }
          catch (SocketException)
          {
            // void
          }
          catch (JsonReaderException jre)
          {
            parent.writeLog(LogLevel.Error, "Error: " + jre);
          }
        }
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation soft-closed");
      }
      catch (ThreadAbortException)
      {
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation hard-closed");

        return;
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public virtual void listenAsync(AbstractGameController controller)
    {
      this.controller = controller;
      thread = new Thread(listen);
      thread.Start();
    }

    public virtual void send(IPAddress address, JSONPacket outPacket)
    {
      send(address, outPacket, OUT_PORT);
    }

    public void send(IPAddress address, JSONPacket outPacket, int port)
    {
      UdpClient client = new UdpClient(address + "", port);
      string sendString = outPacket.ToString();
      Byte[] sendBytes = Encoding.UTF8.GetBytes(sendString);
      try
      {
        client.Send(sendBytes, sendBytes.Length);
        parent.writeLog(LogLevel.Info, "Kirim ke " + address + ":" + port + "/" + sendString);
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public void stopListenAsync(bool force)
    {
      if (force)
      {
        if (thread != null)
        {
          thread.Abort();
        }
      }
      else
      {
        softAbort = true;
      }
    }
  }
}
```

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking. Bagian ini ditangani oleh kelas `PrajuritDatabase`:

```cs
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class PrajuritDatabase
  {
    SQLiteConnection connection;
    public const string FILENAME = "prajurits.internal-sqlite";

    public PrajuritDatabase()
    {
      if (!File.Exists(FILENAME))
      {
        createDatabase();
      }
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
    }

    void createDatabase()
    {
      SQLiteConnection.CreateFile("prajurits.sqlite");
      connection = new SQLiteConnection("Data Source=" + FILENAME +"; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE prajurits (nomerInduk TEXT PRIMARY KEY UNIQUE, name TEXT)", connection);
      command.ExecuteNonQuery();
      connection.Close();
    }

    public bool retrieveNameFromDatabase(Prajurit prajurit)
    {
      SQLiteCommand command = new SQLiteCommand("SELECT name FROM prajurits WHERE nomerInduk=@NOMERINDUK", connection);
      command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
      Object result = command.ExecuteScalar();
      if (result == System.DBNull.Value)
      {
        return false;
      }
      else
      {
        prajurit.nama = (String)result;
        return true;
      }
    }

    public void saveNamesToDatabase(List<Prajurit> prajurits)
    {
      foreach (Prajurit prajurit in prajurits)
      {
        SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO prajurits(nomerInduk, name) VALUES(@NOMERINDUK, @NAME)", connection);
        command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
        command.Parameters.AddWithValue("@NAME", prajurit.nama);
        command.ExecuteNonQuery();
      }
    }

    public void closeConnection()
    {
      connection.Close();
    }
  }
}
```

## Software Server Group B (PC)

Bagian ini berfungsi menampilkan prajurit-prajurit regu B.
Server Group B dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.

### Data Selector B

Modul ini berfungsi untuk memantau dan memilih prajurit mana yang akan
ditampilkan pada peta. Kelas yang bertanggung jawab untuk modul ini adalah
kelas `WatchGameController`:

```cs
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.Controller
{
  class WatchGameController : AbstractGameController
  {
    WatchSilentUDPCommunication modifiedCommunication;
    public WatchGameController(MainWindow parent)
      : base(parent, new WatchSilentUDPCommunication(parent), new WatchSilentEventsRecorder())
    {
      modifiedCommunication = (WatchSilentUDPCommunication)this.communication;
    }

    public void watchExercise(String gameId)
    {
      JSONPacket packet = new JSONPacket("pantau/register");
      this.gameId = gameId;
      packet.setParameter("gameid", gameId);
      modifiedCommunication.broadcast(packet);
      modifiedCommunication.listenWatchConfirmationAsync(this);
      parent.setWatchingEnabled(true);
    }

    public override void stopExercise(bool force)
    {
      base.stopExercise(force);
      modifiedCommunication.stopListenWatchConfirmationAsync();
      parent.setWatchingEnabled(false);
    }
  }

  class WatchSilentUDPCommunication : UDPCommunication
  {
    private Thread watchConfirmationThread = null;

    public WatchSilentUDPCommunication(MainWindow parent)
      : base(parent)
    {
      this.parent = parent;
    }

    public override void send(IPAddress address, JSONPacket outPacket)
    {
      string sendString = outPacket.ToString();
      this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
    }

    public void broadcast(JSONPacket outPacket)
    {
      NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (NetworkInterface Interface in Interfaces)
      {
        if (Interface.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
        if (Interface.OperationalStatus != OperationalStatus.Up) continue;
        Console.WriteLine(Interface.Description);
        UnicastIPAddressInformationCollection UnicastIPInfoCol = Interface.GetIPProperties().UnicastAddresses;
        HashSet<string> broadcastAddresses = new HashSet<string>();
        foreach (UnicastIPAddressInformation UnicatIPInfo in UnicastIPInfoCol)
        {
          byte[] ipAdressBytes = UnicatIPInfo.Address.GetAddressBytes();
          if (ipAdressBytes.Length > 4)
          {
            continue;
          }
          byte[] subnetMaskBytes = UnicatIPInfo.IPv4Mask.GetAddressBytes();
          byte[] broadcastAddress = new byte[ipAdressBytes.Length];
          for (int i = 0; i < broadcastAddress.Length; i++)
          {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255)); // OR ip address dengan subnet
          }
          String ipBroadcastStr = "";
          for (int i = 0; i < broadcastAddress.Length; i++) //for print only
          {
            ipBroadcastStr += broadcastAddress[i] + ".";
          }
          broadcastAddresses.Add(ipBroadcastStr.Substring(0, ipBroadcastStr.Length - 1));
        }
        foreach (string addressStr in broadcastAddresses) {
          IPAddress ipBroadcast = IPAddress.Parse(addressStr);
          base.send(ipBroadcast, outPacket, UDPCommunication.IN_PORT);
          parent.writeLog(LogLevel.Info, "Broadcast ke " + ipBroadcast.ToString());

        }
      }
    }

    private void listenWatchConfirmation()
    {
      UdpClient client = null;
      try
      {
        client = new UdpClient(IN_PORT);
        client.Client.ReceiveTimeout = 1000;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
          try
          {
            byte[] receivedBytes = client.Receive(ref endPoint);
            parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
            JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
            if (inPacket.getParameter("type").Equals("pantau/confirm"))
            {
              if (inPacket.getParameter("status").Equals("ok") && inPacket.getParameter("gameid").Equals(controller.gameId))
              {
                String gameId = inPacket.getParameter("gameid");
                int ammo = Int32.Parse(inPacket.getParameter("ammo"));
                client.Close();
                this.controller.startRegistration(gameId, ammo);
              }
              else
              {
                client.Close();
                parent.showError("Pantau ditolak: " + inPacket.getParameter("status"));
                parent.setWatchingEnabled(false);
              }
              return;
            }
            else
            {
              parent.writeLog(LogLevel.Warn, "Paket diabaikan karena belum memantau: " + inPacket);
            }
          }
          catch (SocketException)
          {
            // void
          }
          catch (ThreadAbortException)
          {
            client.Close();
            return;
          }
        }
      }
      catch (ThreadAbortException)
      {
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public void listenWatchConfirmationAsync(WatchGameController controller)
    {
      this.controller = controller;
      watchConfirmationThread = new Thread(listenWatchConfirmation);
      watchConfirmationThread.Start();
    }

    public void stopListenWatchConfirmationAsync()
    {
      if (watchConfirmationThread != null)
      {
        watchConfirmationThread.Abort();
      }
    }
  }

  class WatchSilentEventsRecorder : EventsRecorder
  {
    public override void startRecording()
    {
      // silenced
    }

    public override void record(IPAddress sender, string eventText)
    {
      // silenced
    }

    public override void stopRecording()
    {
      // silenced
    }

    public override void setProperty(string name, string value)
    {
      // silenced
    }
  }
}
```

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Senjata
  {
    public int idSenjata;
    public Prajurit owner;
    public int initialCounter, currentCounter, initialAmmo;

    public Senjata(int idSenjata, Prajurit owner, int counter, int initialAmmo)
    {
      this.idSenjata = idSenjata;
      this.owner = owner;
      this.initialCounter = counter;
      this.currentCounter = counter;
      this.initialAmmo = initialAmmo;
    }

    public int getRemainingAmmo()
    {
      return (initialCounter + initialAmmo - currentCounter);
    }

    override public String ToString()
    {
      return "#" + idSenjata + " / " + getRemainingAmmo();
    }
  }
}
```

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, dikirimkan oleh
software android dan direpresentasikan dalam kelas `JSONPacket`:

```cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
  public class JSONPacket
  {
    private Dictionary<string, string> parameters = new Dictionary<string,string>();

    public JSONPacket(string type)
    {
      parameters.Add("type", type);
    }

    protected JSONPacket()
    {
      // void
    }

    public static JSONPacket createFromJSONBytes(byte[] jsonBytes)
    {
      JSONPacket packet = new JSONPacket();
      string jsonString = Encoding.UTF8.GetString(jsonBytes);
      packet.parameters = JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonString);
      return packet;
    }

    public void setParameter(string name, string value)
    {
      parameters[name] = value;
    }

    public string getParameter(string name)
    {
      return parameters[name];
    }

    public override string ToString()
    {
      return JsonConvert.SerializeObject(parameters);
    }

    public byte[] toBytes()
    {
      return Encoding.UTF8.GetBytes(ToString());
    }
  }
}
```

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama. Untuk mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut, dibutuhkan kelas `MapDrawer`:

```cs
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
      map.Dispatcher.Invoke((Action)(() =>
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
      p.assignedText.Background = new SolidColorBrush(convertCharToColor(p.group[0]));
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
      map.Dispatcher.Invoke((Action)(() =>
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
      map.Dispatcher.Invoke((Action)(() =>
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
```

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `LiveGameController`:

```cs
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using NLog;

namespace CommandCenter.Controller
{
  class LiveGameController : AbstractGameController
  {

    public LiveGameController(MainWindow parent)
      : base(parent, new UDPCommunication(parent), parent.recorder)
    {
      // void
    }

    public String startRegistration(int initialAmmo)
    {
      Random random = new Random();
      String gameId = "";
      for (int i = 0; i < 3; i++)
      {
        gameId += random.Next(10);
      }

      return startRegistration(gameId, initialAmmo);
    }

    public override void handlePacket(IPAddress address, JSONPacket inPacket, bool updateUI)
    {
      base.handlePacket(address, inPacket, updateUI);
      if (!inPacket.getParameter("type").StartsWith("pantau/"))
      {
        foreach (IPAddress watcher in watchers)
        {
          parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + inPacket);
          communication.send(watcher, inPacket, UDPCommunication.IN_PORT);
        }
      }
    }

    public override void startExercise()
    {
      base.startExercise();
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        string teamGroups = "";
        for (int i = 0; i < prajurits.Count; i++)
        {
          teamGroups += prajurits[i].group;
        }
        packet.setParameter("state", "START/" + teamGroups);
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }

    public override void stopExercise(bool force)
    {
      base.stopExercise(force);
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        packet.setParameter("state", "STOP");
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }
  }
}
```

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Player

Modul ini mencatat koordinat prajurit, arah pandang, status (mati/hidup,
tiarap/berdiri), nomer urut, nomer induk, nama, regu, dll.

Pada software, prajurit direpresentasikan pada kelas `Prajurit`:

```cs
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CommandCenter.Model
{
  public class Prajurit
  {

    public enum State { NORMAL, SHOOT, HIT, DEAD }
    public enum Posture { STAND, CRAWL }

    public IPAddress ipAddress;
    public int nomerUrut { get; set; }
    public string nomerInduk { get; set; }
    public string nama {get; set; }
    public Location location { get; set; }
    public double heading { get; set; }
    public string group { get; set; }
    public DateTime lastUpdate { get; set; }
    public Senjata senjata { get; set; }
    public State state { get; set; }
    public Posture posture { get; set; }
    public int accuracy { get; set; }

    public Image pushpin = null;
    public TextBlock assignedText = null;
    public Ellipse assignedAccuracy = null;

    public static List<string> GROUPS_AVAILABLE = new List<string>() { "A", "B" };

    public Prajurit(int nomerUrut, string nomerInduk, IPAddress ipAddress, string group, Location location)
    {
      this.nomerUrut = nomerUrut;
      this.nomerInduk = nomerInduk;
      this.ipAddress = ipAddress;
      this.group = group;
      this.posture = (Posture)0;
      this.state = (State)0;
      this.accuracy = 200; //sementara - test doang
      if (location == null)
      {
        this.location = null;
      }
      else
      {
        this.location = location;
      }
      lastUpdate = DateTime.Now;
    }

    public static int findPrajuritIndexByNomerInduk(List<Prajurit> prajurits, String nomerInduk)
    {
      for (int i = 0; i < prajurits.Count; i++)
      {
        if (prajurits[i].nomerInduk.Equals(nomerInduk))
        {
          return i;
        }
      }
      return -1;
    }

    public void setLocation(String locationString)
    {
      string[] latlon = locationString.Split(',');
      if (location == null)
      {
        location = new Location();
      }
      location.Latitude = Double.Parse(latlon[0]);
      location.Longitude = Double.Parse(latlon[1]);
      lastUpdate = DateTime.Now;
    }
  }
}
```

### Recorder

Modul ini berfungsi untuk mencatat hal-hal yang terjadi di lapangan,
direpresentasikan dalam kelas `EventsRecorder`:

```cs
using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Events
{
  public class EventsRecorder
  {
    public const string REGISTER = "REGISTER";
    public const string START = "START";
    public const string STOP = "STOP";
    public const string PROP_GAMEID = "gameId";
    public const string PROP_AMMO = "ammo";
    public const string FILENAME = "events.internal-sqlite";
    protected const int COMMIT_PERIOD = 100;

    SQLiteDataReader reader;
    SQLiteTransaction transaction;
    Stopwatch stopwatch;
    protected int commitCounter;

    public EventsRecorder()
    {
    }

    public virtual void startRecording()
    {
      ConnectionSingleton.getInstance().resetDatabase();
      stopwatch = new Stopwatch();
      stopwatch.Start();
      commitCounter = 0;
      transaction = ConnectionSingleton.getInstance().connection.BeginTransaction();
      record(null, REGISTER);
    }

    public virtual void record(IPAddress sender, string eventText)
    {
      long timeOffset = stopwatch.ElapsedMilliseconds;
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
      command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
      command.Parameters.AddWithValue("@SENDER", sender);
      command.Parameters.AddWithValue("@PACKET", eventText);
      int returnValue = command.ExecuteNonQuery();
      if (returnValue != 1)
      {
        throw new Exception("Warning: event not inserted + " + command.ToString());
      }
      commitCounter++;
      if (commitCounter > COMMIT_PERIOD)
      {
        commitCounter = 0;
        transaction.Commit();
        transaction = connection.BeginTransaction();
      }
    }

    public virtual void stopRecording()
    {
      record(null, STOP);
      transaction.Commit();
    }

    public virtual void setProperty(string name, string value)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
      command.Parameters.AddWithValue("@NAME", name);
      command.Parameters.AddWithValue("@VALUE", value);
      command.ExecuteNonQuery();
    }

    public string getProperty(string name)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT value FROM properties WHERE name=@NAME", connection);
      command.Parameters.AddWithValue("@NAME", name);
      return (string)command.ExecuteScalar();
    }

    public Int64 getRecordingLength()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
      Int64 length = (Int64)command.ExecuteScalar();
      return length;
    }

    public void startReplaying()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection);
      reader = command.ExecuteReader();
    }

    public Event getNextPlayEvent()
    {
      if (reader.Read())
      {
        Event newEvent = new Event();
        newEvent.timeOffset = (Int64)reader["timeOffset"];
        newEvent.sender = reader["sender"] is DBNull ? null : IPAddress.Parse((string)reader["sender"]);
        newEvent.packet = reader["packet"] is DBNull ? null : (string)reader["packet"];
        return newEvent;
      }
      else
      {
        return null;
      }
    }

    public void stopReplaying()
    {
      // void
    }

    public static void loadFrom(string filename)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteConnection connection2 = new SQLiteConnection("Data Source=" + filename + "; Version=3;");
      connection2.Open();
      ConnectionSingleton.getInstance().resetDatabase();
      new SQLiteCommand("BEGIN", connection).ExecuteNonQuery();
      SQLiteCommand command2 = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection2);
      SQLiteDataReader reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
        command.Parameters.AddWithValue("@TIMEOFFSET", reader2["timeOffset"]);
        command.Parameters.AddWithValue("@SENDER", reader2["sender"]);
        command.Parameters.AddWithValue("@PACKET", reader2["packet"]);
        command.ExecuteNonQuery();
      }
      command2 = new SQLiteCommand("SELECT name, value FROM properties", connection2);
      reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
        command.Parameters.AddWithValue("@NAME", reader2["name"]);
        command.Parameters.AddWithValue("@VALUE", reader2["value"]);
        command.ExecuteNonQuery();
      }
      new SQLiteCommand("COMMIT", connection).ExecuteNonQuery();
      connection2.Close();
    }

    public static void closeConnection()
    {
      ConnectionSingleton.getInstance().connection.Close();
    }
  }

  class ConnectionSingleton
  {
    protected const string FILENAME = EventsRecorder.FILENAME;
    public SQLiteConnection connection;
    protected static ConnectionSingleton instance = null;

    protected ConnectionSingleton() {
      SQLiteConnection.CreateFile(FILENAME);
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE events (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("CREATE TABLE properties (name TEXT PRIMARY KEY UNIQUE, value TEXT)", connection);
      command.ExecuteNonQuery();
    }

    public static ConnectionSingleton getInstance()
    {
      if (instance == null)
      {
        instance = new ConnectionSingleton();
      }
      return instance;
    }

    public void resetDatabase()
    {
      SQLiteCommand command = new SQLiteCommand("DELETE FROM events", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("DELETE FROM properties", connection);
      command.ExecuteNonQuery();
    }
  }
}
```

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`:

```cs
using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CommandCenter.Controller
{
  class ReplayGameController : AbstractGameController
  {

    Timer eventTimer, heartbeatTimer;
    EventsRecorder player;
    Stopwatch stopwatch;
    Event scheduledEvent;
    long skippedMilliseconds;

    public ReplayGameController(MainWindow parent)
      : base(parent, new ReplaySilentUDPCommunication(parent), new ReplaySilentEventsRecorder())
    {
      stopwatch = new Stopwatch();
      eventTimer = new Timer();
      eventTimer.Elapsed += OnEventTimedEvent;
      heartbeatTimer = new Timer();
      heartbeatTimer.Elapsed += OnHeartbeatTimedEvent;
      player = new EventsRecorder();
    }

    public void startPlayback()
    {
      player.startReplaying();
      stopwatch.Restart();
      parent.updateReplayProgress(0);
      scheduledEvent = null;
      executePacketAndScheduleNext();
      eventTimer.Enabled = true;
      skippedMilliseconds = 0;
      heartbeatTimer.Interval = 1000 / parent.playSpeed;
      heartbeatTimer.Enabled = true;
      parent.setReplayingEnabled(true);
    }

    public void stopPlayback()
    {
      eventTimer.Enabled = false;
      heartbeatTimer.Enabled = false;
      stopwatch.Stop();
      player.stopReplaying();
      scheduledEvent = null;
      this.state = State.IDLE;
      parent.setReplayingEnabled(false);
    }

    private void executePacketAndScheduleNext()
    {
      eventTimer.Enabled = false;
      bool updateUI = !(parent.skipRegistration && state == State.REGISTRATION);
      if (scheduledEvent != null)
      {
        if (updateUI)
        {
          parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
        }
        if (scheduledEvent.packet.Equals(EventsRecorder.REGISTER))
        {
          startRegistration(player.getProperty(EventsRecorder.PROP_GAMEID), Int32.Parse(player.getProperty(EventsRecorder.PROP_AMMO)));
        }
        else if (scheduledEvent.packet.StartsWith(EventsRecorder.START))
        {
          string[] tokens = scheduledEvent.packet.Split('/');
          for (int i = 0; i < tokens[1].Length; i++)
          {
            prajurits[i].group = "" + tokens[1][i];
          }
          startExercise();
        }
        else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
        {
          stopExercise(true);
        }
        else
        {
          parent.writeLog(LogLevel.Info, "Pura-pura terima dari " + scheduledEvent.sender + ": " + scheduledEvent.packet);
          parent.pesertaDataGrid.Dispatcher.Invoke((Action)(() =>
          {
            this.handlePacket(scheduledEvent.sender, JSONPacket.createFromJSONBytes(Encoding.UTF8.GetBytes(scheduledEvent.packet)), updateUI);
          }));      

        }
      }
      Event nextEvent = player.getNextPlayEvent();
      if (nextEvent != null)
      {
        scheduledEvent = nextEvent;
        long currentTime = (long)((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed);
        long interval = (long)((nextEvent.timeOffset - currentTime) / parent.playSpeed);
        if (parent.skipRegistration && state == State.REGISTRATION)
        {
          skippedMilliseconds += interval;
          interval = 0;
        }
        eventTimer.Interval = interval <= 0 ? 1 : interval;
        eventTimer.Enabled = true;
      }
      else
      {
        stopPlayback();
      }
    }

    private void OnEventTimedEvent(Object source, ElapsedEventArgs e)
    {
      executePacketAndScheduleNext();
    }

    private void OnHeartbeatTimedEvent(Object source, ElapsedEventArgs e)
    {
      if (parent.skipRegistration && state == State.REGISTRATION)
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed), "(>>)");
      }
      else
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
      }
    }
  }

  class ReplaySilentUDPCommunication : UDPCommunication
  {
    public ReplaySilentUDPCommunication(MainWindow parent) : base(parent)
    {
      this.parent = parent;
    }

    public override void listenAsync(AbstractGameController controller)
    {
      // void
    }

    public override void send(IPAddress address, JSONPacket outPacket)
    {
      string sendString = outPacket.ToString();
      this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
    }
  }

  class ReplaySilentEventsRecorder : EventsRecorder
  {
    public override void startRecording()
    {
      // silenced
    }

    public override void record(IPAddress sender, string eventText)
    {
      // silenced
    }

    public override void stopRecording()
    {
      // silenced
    }

    public override void setProperty(string name, string value)
    {
      // silenced
    }
  }
}
```

### Data Interface

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication`:

```cs
using CommandCenter.Controller;
using CommandCenter.Model.Protocol;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.View
{
  public class UDPCommunication
  {
    public const int IN_PORT = 21500;
    public const int OUT_PORT = 21501;

    protected MainWindow parent;
    protected AbstractGameController controller = null;
    private bool softAbort;
    Thread thread = null;

    public UDPCommunication(MainWindow parent)
    {
      this.parent = parent;
    }

    private void listen()
    {
      UdpClient client = null;
      try
      {
        client = new UdpClient(IN_PORT);
        client.Client.ReceiveTimeout = 1000;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        softAbort = false;
        while (!softAbort)
        {
          try
          {
            byte[] receivedBytes = client.Receive(ref endPoint);
            parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
            JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
            controller.handlePacket(endPoint.Address, inPacket, true);
          }
          catch (SocketException)
          {
            // void
          }
          catch (JsonReaderException jre)
          {
            parent.writeLog(LogLevel.Error, "Error: " + jre);
          }
        }
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation soft-closed");
      }
      catch (ThreadAbortException)
      {
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation hard-closed");

        return;
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public virtual void listenAsync(AbstractGameController controller)
    {
      this.controller = controller;
      thread = new Thread(listen);
      thread.Start();
    }

    public virtual void send(IPAddress address, JSONPacket outPacket)
    {
      send(address, outPacket, OUT_PORT);
    }

    public void send(IPAddress address, JSONPacket outPacket, int port)
    {
      UdpClient client = new UdpClient(address + "", port);
      string sendString = outPacket.ToString();
      Byte[] sendBytes = Encoding.UTF8.GetBytes(sendString);
      try
      {
        client.Send(sendBytes, sendBytes.Length);
        parent.writeLog(LogLevel.Info, "Kirim ke " + address + ":" + port + "/" + sendString);
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public void stopListenAsync(bool force)
    {
      if (force)
      {
        if (thread != null)
        {
          thread.Abort();
        }
      }
      else
      {
        softAbort = true;
      }
    }
  }
}
```

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking. Bagian ini ditangani oleh kelas `PrajuritDatabase`:

```cs
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class PrajuritDatabase
  {
    SQLiteConnection connection;
    public const string FILENAME = "prajurits.internal-sqlite";

    public PrajuritDatabase()
    {
      if (!File.Exists(FILENAME))
      {
        createDatabase();
      }
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
    }

    void createDatabase()
    {
      SQLiteConnection.CreateFile("prajurits.sqlite");
      connection = new SQLiteConnection("Data Source=" + FILENAME +"; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE prajurits (nomerInduk TEXT PRIMARY KEY UNIQUE, name TEXT)", connection);
      command.ExecuteNonQuery();
      connection.Close();
    }

    public bool retrieveNameFromDatabase(Prajurit prajurit)
    {
      SQLiteCommand command = new SQLiteCommand("SELECT name FROM prajurits WHERE nomerInduk=@NOMERINDUK", connection);
      command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
      Object result = command.ExecuteScalar();
      if (result == System.DBNull.Value)
      {
        return false;
      }
      else
      {
        prajurit.nama = (String)result;
        return true;
      }
    }

    public void saveNamesToDatabase(List<Prajurit> prajurits)
    {
      foreach (Prajurit prajurit in prajurits)
      {
        SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO prajurits(nomerInduk, name) VALUES(@NOMERINDUK, @NAME)", connection);
        command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
        command.Parameters.AddWithValue("@NAME", prajurit.nama);
        command.ExecuteNonQuery();
      }
    }

    public void closeConnection()
    {
      connection.Close();
    }
  }
}
```

## Software Server Utama (PC)

Server utama dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Senjata
  {
    public int idSenjata;
    public Prajurit owner;
    public int initialCounter, currentCounter, initialAmmo;

    public Senjata(int idSenjata, Prajurit owner, int counter, int initialAmmo)
    {
      this.idSenjata = idSenjata;
      this.owner = owner;
      this.initialCounter = counter;
      this.currentCounter = counter;
      this.initialAmmo = initialAmmo;
    }

    public int getRemainingAmmo()
    {
      return (initialCounter + initialAmmo - currentCounter);
    }

    override public String ToString()
    {
      return "#" + idSenjata + " / " + getRemainingAmmo();
    }
  }
}
```

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, dikirimkan oleh
software android dan direpresentasikan dalam kelas `JSONPacket`:

```cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Protocol
{
  public class JSONPacket
  {
    private Dictionary<string, string> parameters = new Dictionary<string,string>();

    public JSONPacket(string type)
    {
      parameters.Add("type", type);
    }

    protected JSONPacket()
    {
      // void
    }

    public static JSONPacket createFromJSONBytes(byte[] jsonBytes)
    {
      JSONPacket packet = new JSONPacket();
      string jsonString = Encoding.UTF8.GetString(jsonBytes);
      packet.parameters = JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonString);
      return packet;
    }

    public void setParameter(string name, string value)
    {
      parameters[name] = value;
    }

    public string getParameter(string name)
    {
      return parameters[name];
    }

    public override string ToString()
    {
      return JsonConvert.SerializeObject(parameters);
    }

    public byte[] toBytes()
    {
      return Encoding.UTF8.GetBytes(ToString());
    }
  }
}
```

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama. Untuk mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut, dibutuhkan kelas `MapDrawer`:

```cs
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
      map.Dispatcher.Invoke((Action)(() =>
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
      p.assignedText.Background = new SolidColorBrush(convertCharToColor(p.group[0]));
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
      map.Dispatcher.Invoke((Action)(() =>
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
      map.Dispatcher.Invoke((Action)(() =>
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
```

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `LiveGameController`:

```cs
using CommandCenter.View;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using NLog;

namespace CommandCenter.Controller
{
  class LiveGameController : AbstractGameController
  {

    public LiveGameController(MainWindow parent)
      : base(parent, new UDPCommunication(parent), parent.recorder)
    {
      // void
    }

    public String startRegistration(int initialAmmo)
    {
      Random random = new Random();
      String gameId = "";
      for (int i = 0; i < 3; i++)
      {
        gameId += random.Next(10);
      }

      return startRegistration(gameId, initialAmmo);
    }

    public override void handlePacket(IPAddress address, JSONPacket inPacket, bool updateUI)
    {
      base.handlePacket(address, inPacket, updateUI);
      if (!inPacket.getParameter("type").StartsWith("pantau/"))
      {
        foreach (IPAddress watcher in watchers)
        {
          parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + inPacket);
          communication.send(watcher, inPacket, UDPCommunication.IN_PORT);
        }
      }
    }

    public override void startExercise()
    {
      base.startExercise();
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        string teamGroups = "";
        for (int i = 0; i < prajurits.Count; i++)
        {
          teamGroups += prajurits[i].group;
        }
        packet.setParameter("state", "START/" + teamGroups);
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }

    public override void stopExercise(bool force)
    {
      base.stopExercise(force);
      foreach (IPAddress watcher in watchers)
      {
        JSONPacket packet = new JSONPacket("pantau/state");
        packet.setParameter("state", "STOP");
        parent.writeLog(LogLevel.Info, "Kirim ke pemantau " + watcher + " pesan " + packet);
        communication.send(watcher, packet, UDPCommunication.IN_PORT);
      }
    }
  }
}
```

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

Modul ini ditangani oleh kelas `Event`:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class Event
  {
    public Int64 timeOffset;
    public IPAddress sender;
    public string packet;
  }
}
```

### Player

Modul ini mencatat koordinat prajurit, arah pandang, status (mati/hidup,
tiarap/berdiri), nomer urut, nomer induk, nama, regu, dll.

Pada software, prajurit direpresentasikan pada kelas `Prajurit`:

```cs
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CommandCenter.Model
{
  public class Prajurit
  {

    public enum State { NORMAL, SHOOT, HIT, DEAD }
    public enum Posture { STAND, CRAWL }

    public IPAddress ipAddress;
    public int nomerUrut { get; set; }
    public string nomerInduk { get; set; }
    public string nama {get; set; }
    public Location location { get; set; }
    public double heading { get; set; }
    public string group { get; set; }
    public DateTime lastUpdate { get; set; }
    public Senjata senjata { get; set; }
    public State state { get; set; }
    public Posture posture { get; set; }
    public int accuracy { get; set; }

    public Image pushpin = null;
    public TextBlock assignedText = null;
    public Ellipse assignedAccuracy = null;

    public static List<string> GROUPS_AVAILABLE = new List<string>() { "A", "B" };

    public Prajurit(int nomerUrut, string nomerInduk, IPAddress ipAddress, string group, Location location)
    {
      this.nomerUrut = nomerUrut;
      this.nomerInduk = nomerInduk;
      this.ipAddress = ipAddress;
      this.group = group;
      this.posture = (Posture)0;
      this.state = (State)0;
      this.accuracy = 200; //sementara - test doang
      if (location == null)
      {
        this.location = null;
      }
      else
      {
        this.location = location;
      }
      lastUpdate = DateTime.Now;
    }

    public static int findPrajuritIndexByNomerInduk(List<Prajurit> prajurits, String nomerInduk)
    {
      for (int i = 0; i < prajurits.Count; i++)
      {
        if (prajurits[i].nomerInduk.Equals(nomerInduk))
        {
          return i;
        }
      }
      return -1;
    }

    public void setLocation(String locationString)
    {
      string[] latlon = locationString.Split(',');
      if (location == null)
      {
        location = new Location();
      }
      location.Latitude = Double.Parse(latlon[0]);
      location.Longitude = Double.Parse(latlon[1]);
      lastUpdate = DateTime.Now;
    }
  }
}
```

### Recorder

Modul ini berfungsi untuk mencatat hal-hal yang terjadi di lapangan,
direpresentasikan dalam kelas `EventsRecorder`:

```cs
using CommandCenter.Model.Protocol;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model.Events
{
  public class EventsRecorder
  {
    public const string REGISTER = "REGISTER";
    public const string START = "START";
    public const string STOP = "STOP";
    public const string PROP_GAMEID = "gameId";
    public const string PROP_AMMO = "ammo";
    public const string FILENAME = "events.internal-sqlite";
    protected const int COMMIT_PERIOD = 100;

    SQLiteDataReader reader;
    SQLiteTransaction transaction;
    Stopwatch stopwatch;
    protected int commitCounter;

    public EventsRecorder()
    {
    }

    public virtual void startRecording()
    {
      ConnectionSingleton.getInstance().resetDatabase();
      stopwatch = new Stopwatch();
      stopwatch.Start();
      commitCounter = 0;
      transaction = ConnectionSingleton.getInstance().connection.BeginTransaction();
      record(null, REGISTER);
    }

    public virtual void record(IPAddress sender, string eventText)
    {
      long timeOffset = stopwatch.ElapsedMilliseconds;
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
      command.Parameters.AddWithValue("@TIMEOFFSET", timeOffset);
      command.Parameters.AddWithValue("@SENDER", sender);
      command.Parameters.AddWithValue("@PACKET", eventText);
      int returnValue = command.ExecuteNonQuery();
      if (returnValue != 1)
      {
        throw new Exception("Warning: event not inserted + " + command.ToString());
      }
      commitCounter++;
      if (commitCounter > COMMIT_PERIOD)
      {
        commitCounter = 0;
        transaction.Commit();
        transaction = connection.BeginTransaction();
      }
    }

    public virtual void stopRecording()
    {
      record(null, STOP);
      transaction.Commit();
    }

    public virtual void setProperty(string name, string value)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
      command.Parameters.AddWithValue("@NAME", name);
      command.Parameters.AddWithValue("@VALUE", value);
      command.ExecuteNonQuery();
    }

    public string getProperty(string name)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT value FROM properties WHERE name=@NAME", connection);
      command.Parameters.AddWithValue("@NAME", name);
      return (string)command.ExecuteScalar();
    }

    public Int64 getRecordingLength()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT MAX(timeOffset) FROM events", connection);
      Int64 length = (Int64)command.ExecuteScalar();
      return length;
    }

    public void startReplaying()
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteCommand command = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection);
      reader = command.ExecuteReader();
    }

    public Event getNextPlayEvent()
    {
      if (reader.Read())
      {
        Event newEvent = new Event();
        newEvent.timeOffset = (Int64)reader["timeOffset"];
        newEvent.sender = reader["sender"] is DBNull ? null : IPAddress.Parse((string)reader["sender"]);
        newEvent.packet = reader["packet"] is DBNull ? null : (string)reader["packet"];
        return newEvent;
      }
      else
      {
        return null;
      }
    }

    public void stopReplaying()
    {
      // void
    }

    public static void loadFrom(string filename)
    {
      SQLiteConnection connection = ConnectionSingleton.getInstance().connection;
      SQLiteConnection connection2 = new SQLiteConnection("Data Source=" + filename + "; Version=3;");
      connection2.Open();
      ConnectionSingleton.getInstance().resetDatabase();
      new SQLiteCommand("BEGIN", connection).ExecuteNonQuery();
      SQLiteCommand command2 = new SQLiteCommand("SELECT timeOffset, sender, packet FROM events", connection2);
      SQLiteDataReader reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO events (timeOffset, sender, packet) VALUES (@TIMEOFFSET, @SENDER, @PACKET)", connection);
        command.Parameters.AddWithValue("@TIMEOFFSET", reader2["timeOffset"]);
        command.Parameters.AddWithValue("@SENDER", reader2["sender"]);
        command.Parameters.AddWithValue("@PACKET", reader2["packet"]);
        command.ExecuteNonQuery();
      }
      command2 = new SQLiteCommand("SELECT name, value FROM properties", connection2);
      reader2 = command2.ExecuteReader();
      while (reader2.Read())
      {
        SQLiteCommand command = new SQLiteCommand("INSERT INTO properties(name, value) VALUES(@NAME, @VALUE)", connection);
        command.Parameters.AddWithValue("@NAME", reader2["name"]);
        command.Parameters.AddWithValue("@VALUE", reader2["value"]);
        command.ExecuteNonQuery();
      }
      new SQLiteCommand("COMMIT", connection).ExecuteNonQuery();
      connection2.Close();
    }

    public static void closeConnection()
    {
      ConnectionSingleton.getInstance().connection.Close();
    }
  }

  class ConnectionSingleton
  {
    protected const string FILENAME = EventsRecorder.FILENAME;
    public SQLiteConnection connection;
    protected static ConnectionSingleton instance = null;

    protected ConnectionSingleton() {
      SQLiteConnection.CreateFile(FILENAME);
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE events (timeOffset INTEGER, sender TEXT, packet TEXT)", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("CREATE TABLE properties (name TEXT PRIMARY KEY UNIQUE, value TEXT)", connection);
      command.ExecuteNonQuery();
    }

    public static ConnectionSingleton getInstance()
    {
      if (instance == null)
      {
        instance = new ConnectionSingleton();
      }
      return instance;
    }

    public void resetDatabase()
    {
      SQLiteCommand command = new SQLiteCommand("DELETE FROM events", connection);
      command.ExecuteNonQuery();
      command = new SQLiteCommand("DELETE FROM properties", connection);
      command.ExecuteNonQuery();
    }
  }
}
```

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`:

```cs
using CommandCenter.Model;
using CommandCenter.Model.Events;
using CommandCenter.Model.Protocol;
using CommandCenter.View;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CommandCenter.Controller
{
  class ReplayGameController : AbstractGameController
  {

    Timer eventTimer, heartbeatTimer;
    EventsRecorder player;
    Stopwatch stopwatch;
    Event scheduledEvent;
    long skippedMilliseconds;

    public ReplayGameController(MainWindow parent)
      : base(parent, new ReplaySilentUDPCommunication(parent), new ReplaySilentEventsRecorder())
    {
      stopwatch = new Stopwatch();
      eventTimer = new Timer();
      eventTimer.Elapsed += OnEventTimedEvent;
      heartbeatTimer = new Timer();
      heartbeatTimer.Elapsed += OnHeartbeatTimedEvent;
      player = new EventsRecorder();
    }

    public void startPlayback()
    {
      player.startReplaying();
      stopwatch.Restart();
      parent.updateReplayProgress(0);
      scheduledEvent = null;
      executePacketAndScheduleNext();
      eventTimer.Enabled = true;
      skippedMilliseconds = 0;
      heartbeatTimer.Interval = 1000 / parent.playSpeed;
      heartbeatTimer.Enabled = true;
      parent.setReplayingEnabled(true);
    }

    public void stopPlayback()
    {
      eventTimer.Enabled = false;
      heartbeatTimer.Enabled = false;
      stopwatch.Stop();
      player.stopReplaying();
      scheduledEvent = null;
      this.state = State.IDLE;
      parent.setReplayingEnabled(false);
    }

    private void executePacketAndScheduleNext()
    {
      eventTimer.Enabled = false;
      bool updateUI = !(parent.skipRegistration && state == State.REGISTRATION);
      if (scheduledEvent != null)
      {
        if (updateUI)
        {
          parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
        }
        if (scheduledEvent.packet.Equals(EventsRecorder.REGISTER))
        {
          startRegistration(player.getProperty(EventsRecorder.PROP_GAMEID), Int32.Parse(player.getProperty(EventsRecorder.PROP_AMMO)));
        }
        else if (scheduledEvent.packet.StartsWith(EventsRecorder.START))
        {
          string[] tokens = scheduledEvent.packet.Split('/');
          for (int i = 0; i < tokens[1].Length; i++)
          {
            prajurits[i].group = "" + tokens[1][i];
          }
          startExercise();
        }
        else if (scheduledEvent.packet.Equals(EventsRecorder.STOP))
        {
          stopExercise(true);
        }
        else
        {
          parent.writeLog(LogLevel.Info, "Pura-pura terima dari " + scheduledEvent.sender + ": " + scheduledEvent.packet);
          parent.pesertaDataGrid.Dispatcher.Invoke((Action)(() =>
          {
            this.handlePacket(scheduledEvent.sender, JSONPacket.createFromJSONBytes(Encoding.UTF8.GetBytes(scheduledEvent.packet)), updateUI);
          }));      

        }
      }
      Event nextEvent = player.getNextPlayEvent();
      if (nextEvent != null)
      {
        scheduledEvent = nextEvent;
        long currentTime = (long)((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed);
        long interval = (long)((nextEvent.timeOffset - currentTime) / parent.playSpeed);
        if (parent.skipRegistration && state == State.REGISTRATION)
        {
          skippedMilliseconds += interval;
          interval = 0;
        }
        eventTimer.Interval = interval <= 0 ? 1 : interval;
        eventTimer.Enabled = true;
      }
      else
      {
        stopPlayback();
      }
    }

    private void OnEventTimedEvent(Object source, ElapsedEventArgs e)
    {
      executePacketAndScheduleNext();
    }

    private void OnHeartbeatTimedEvent(Object source, ElapsedEventArgs e)
    {
      if (parent.skipRegistration && state == State.REGISTRATION)
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed), "(>>)");
      }
      else
      {
        parent.updateReplayProgress(1e-3 * ((stopwatch.ElapsedMilliseconds + skippedMilliseconds) * parent.playSpeed));
      }
    }
  }

  class ReplaySilentUDPCommunication : UDPCommunication
  {
    public ReplaySilentUDPCommunication(MainWindow parent) : base(parent)
    {
      this.parent = parent;
    }

    public override void listenAsync(AbstractGameController controller)
    {
      // void
    }

    public override void send(IPAddress address, JSONPacket outPacket)
    {
      string sendString = outPacket.ToString();
      this.parent.writeLog(LogLevel.Info, "Pura-pura kirim ke " + address + ": " + sendString);
    }
  }

  class ReplaySilentEventsRecorder : EventsRecorder
  {
    public override void startRecording()
    {
      // silenced
    }

    public override void record(IPAddress sender, string eventText)
    {
      // silenced
    }

    public override void stopRecording()
    {
      // silenced
    }

    public override void setProperty(string name, string value)
    {
      // silenced
    }
  }
}
```

### Data Interface

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication`:

```cs
using CommandCenter.Controller;
using CommandCenter.Model.Protocol;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.View
{
  public class UDPCommunication
  {
    public const int IN_PORT = 21500;
    public const int OUT_PORT = 21501;

    protected MainWindow parent;
    protected AbstractGameController controller = null;
    private bool softAbort;
    Thread thread = null;

    public UDPCommunication(MainWindow parent)
    {
      this.parent = parent;
    }

    private void listen()
    {
      UdpClient client = null;
      try
      {
        client = new UdpClient(IN_PORT);
        client.Client.ReceiveTimeout = 1000;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        softAbort = false;
        while (!softAbort)
        {
          try
          {
            byte[] receivedBytes = client.Receive(ref endPoint);
            parent.writeLog(LogLevel.Info, "Terima dari " + endPoint + ": " + Encoding.ASCII.GetString(receivedBytes));
            JSONPacket inPacket = JSONPacket.createFromJSONBytes(receivedBytes);
            controller.handlePacket(endPoint.Address, inPacket, true);
          }
          catch (SocketException)
          {
            // void
          }
          catch (JsonReaderException jre)
          {
            parent.writeLog(LogLevel.Error, "Error: " + jre);
          }
        }
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation soft-closed");
      }
      catch (ThreadAbortException)
      {
        client.Close();
        parent.writeLog(LogLevel.Info, "Communcation hard-closed");

        return;
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public virtual void listenAsync(AbstractGameController controller)
    {
      this.controller = controller;
      thread = new Thread(listen);
      thread.Start();
    }

    public virtual void send(IPAddress address, JSONPacket outPacket)
    {
      send(address, outPacket, OUT_PORT);
    }

    public void send(IPAddress address, JSONPacket outPacket, int port)
    {
      UdpClient client = new UdpClient(address + "", port);
      string sendString = outPacket.ToString();
      Byte[] sendBytes = Encoding.UTF8.GetBytes(sendString);
      try
      {
        client.Send(sendBytes, sendBytes.Length);
        parent.writeLog(LogLevel.Info, "Kirim ke " + address + ":" + port + "/" + sendString);
      }
      catch (Exception e)
      {
        parent.writeLog(LogLevel.Error, "Error: " + e);
      }
    }

    public void stopListenAsync(bool force)
    {
      if (force)
      {
        if (thread != null)
        {
          thread.Abort();
        }
      }
      else
      {
        softAbort = true;
      }
    }
  }
}
```

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking. Bagian ini ditangani oleh kelas `PrajuritDatabase`:

```cs
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandCenter.Model
{
  public class PrajuritDatabase
  {
    SQLiteConnection connection;
    public const string FILENAME = "prajurits.internal-sqlite";

    public PrajuritDatabase()
    {
      if (!File.Exists(FILENAME))
      {
        createDatabase();
      }
      connection = new SQLiteConnection("Data Source=" + FILENAME + "; Version=3;");
      connection.Open();
    }

    void createDatabase()
    {
      SQLiteConnection.CreateFile("prajurits.sqlite");
      connection = new SQLiteConnection("Data Source=" + FILENAME +"; Version=3;");
      connection.Open();
      SQLiteCommand command = new SQLiteCommand("CREATE TABLE prajurits (nomerInduk TEXT PRIMARY KEY UNIQUE, name TEXT)", connection);
      command.ExecuteNonQuery();
      connection.Close();
    }

    public bool retrieveNameFromDatabase(Prajurit prajurit)
    {
      SQLiteCommand command = new SQLiteCommand("SELECT name FROM prajurits WHERE nomerInduk=@NOMERINDUK", connection);
      command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
      Object result = command.ExecuteScalar();
      if (result == System.DBNull.Value)
      {
        return false;
      }
      else
      {
        prajurit.nama = (String)result;
        return true;
      }
    }

    public void saveNamesToDatabase(List<Prajurit> prajurits)
    {
      foreach (Prajurit prajurit in prajurits)
      {
        SQLiteCommand command = new SQLiteCommand("INSERT OR REPLACE INTO prajurits(nomerInduk, name) VALUES(@NOMERINDUK, @NAME)", connection);
        command.Parameters.AddWithValue("@NOMERINDUK", prajurit.nomerInduk);
        command.Parameters.AddWithValue("@NAME", prajurit.nama);
        command.ExecuteNonQuery();
      }
    }

    public void closeConnection()
    {
      connection.Close();
    }
  }
}
```