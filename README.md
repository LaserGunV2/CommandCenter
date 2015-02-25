CommandCenter
=============

Bagian PC dari proyek LaserGunV2, yang mengkoordinasikan semua.

Development
-----------

Requirements:
* Visual Studio Express 2013 for Windows Desktop (http://www.visualstudio.com/downloads/download-visual-studio-vs#d-express-windows-desktop)
* Bing Maps Windows Presentation Foundation (WPF) Control (http://www.microsoft.com/en-us/download/details.aspx?id=27165), jangan lupa _add reference_ juga di projectnya, seperti di http://msdn.microsoft.com/en-us/library/hh830433.aspx.
* Json.NET (Download di https://www.nuget.org/packages/newtonsoft.json/)
* SQLite (Download di https://www.nuget.org/packages/system.data.sqlite)

Langkah membuat installer:

1. Install Inno Setup (http://www.jrsoftware.org/isinfo.php)
2. Buka file `CommandCenter/CommandCenter/build.iss` dengan Inno Setup
3. Ubah nilai `MyAppVersion` dengan versi yang akan dirilis
4. Pilih compile. Installer akan berada di direktori `Output`.
5. Jangan lupa memasukkan file installer tersebut saat rilis.

Informasi Lain:
* Untuk Bing Map License Key, menggunakan account Windows Live dengan email: commandcenter@outlook.sg dan password: (nama solution command center pada project pertama)
* Untuk pengujian UDP packet, bisa memanfaatkan PacketSender (http://packetsender.com/)
* Untuk menganalisa log, dapat menggunakan BareTail (http://www.baremetalsoft.com/baretail/)

Deployment
----------

### Cara Otomatis

Installer CommandCenter bisa didapat dari paket rilis (`CommandCenterInstaller.exe`).
Silahkan mengikuti langkah-langkah yang ditunjukkan pada wizard installer.

### Cara Manual

Untuk menjalankan program jadi, bisa download/copy dari direktori `CommandCenter/CommandCenter/bin/Release`:

* File `CommandCenter.exe`
* Semua file berekstensi `.dll`

Debugging
---------

Untuk membaca log file `CommandCenter.log` bisa menggunakan aplikasi [BareTail](http://www.baremetalsoft.com/baretail/)
