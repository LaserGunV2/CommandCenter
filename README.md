CommandCenter
=============

Bagian PC dari proyek LaserGunV2, yang mengkoordinasikan semua.

Development
-----------

Requirements:

* Visual Studio Express 2013 for Windows Desktop (http://www.visualstudio.com/downloads/download-visual-studio-vs#d-express-windows-desktop)
* Bing Maps Windows Presentation Foundation (WPF) Control (http://www.microsoft.com/en-us/download/details.aspx?id=27165), jangan lupa _add reference_ juga di projectnya, seperti di http://msdn.microsoft.com/en-us/library/hh830433.aspx.
* Json.NET (https://www.nuget.org/packages/newtonsoft.json/)

Informasi Lain:
* Untuk Bing Map License Key, menggunakan account Windows Live dengan email: commandcenter@outlook.sg dan password: (nama solution command center pada project pertama)
* Untuk pengujian UDP packet, bisa memanfaatkan PacketSender (http://packetsender.com/)

Deployment
----------

Untuk menjalankan program jadi, bisa download/copy dari direktori `CommandCenter/CommandCenter/bin/Release`:

* File `CommandCenter.exe`
* Semua file berekstensi `.dll`
