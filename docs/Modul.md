# Modul-Modul Command Center

Dokumen ini berisi pemetaan antara modul-modul yang direncanakan di awal dengan
implementasinya.

## Software Server Group A (PC)

Bagian ini berfungsi menampilkan prajurit-prajurit regu A.
Server Group A dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.

### Data Selector A

Kelas `MainWindow` memiliki _checkbox_ untuk memilih apakah tim A akan
ditampilkan di modul Map Retrieval atau tidak. Kelas `MapDrawer` kemudian akan
menyaring apakah prajurit pada regu A akan ditampilkan.

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata` yang mencatat sisa peluru serta pemilik senjata.

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, direpresentasikan
dalam kelas `Event`.

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama, yang direpresentasikan dalam kelas
`MainWindow`. Selain itu, kelas `MapDrawer` bertugas mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut.

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `AbstractGameController` dan `LiveGameController`.

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

### Player

Pada software, prajurit direpresentasikan pada kelas `Prajurit`. Modul ini
mencatat koordinat prajurit, arah pandang, status (mati/hidup, tiarap/berdiri),
nomer urut, nomer induk, nama, regu, dll. Terdapat pula kelas `PrajuritDatabase`
yang berfungsi menyimpan informasi-informasi tersebut dalam modul database.

### Recorder

Modul ini direpresentasikan dalam kelas `EventsRecorder`, yang bertugas
menyimpan `Event` ke dalam modul database.

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`.

### Data Interface

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication` serta `JSONacket`.

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking.

## Software Server Group B (PC)

Bagian ini berfungsi menampilkan prajurit-prajurit regu B.
Server Group B dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.


### Data Selector B

Kelas `MainWindow` memiliki _checkbox_ untuk memilih apakah tim B akan
ditampilkan di modul Map Retrieval atau tidak. Kelas `MapDrawer` kemudian akan
menyaring apakah prajurit pada regu B akan ditampilkan.

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata` yang mencatat sisa peluru serta pemilik senjata.

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, direpresentasikan
dalam kelas `Event`.

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama, yang direpresentasikan dalam kelas
`MainWindow`. Selain itu, kelas `MapDrawer` bertugas mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut.

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `AbstractGameController` dan `LiveGameController`.

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

### Player

Pada software, prajurit direpresentasikan pada kelas `Prajurit`. Modul ini
mencatat koordinat prajurit, arah pandang, status (mati/hidup, tiarap/berdiri),
nomer urut, nomer induk, nama, regu, dll. Terdapat pula kelas `PrajuritDatabase`
yang berfungsi menyimpan informasi-informasi tersebut dalam modul database.

### Recorder

Modul ini direpresentasikan dalam kelas `EventsRecorder`, yang bertugas
menyimpan `Event` ke dalam modul database.

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`.

### Data Interface

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication` serta `JSONacket`.

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking.

## Software Server Utama (PC)

Server utama dibangun di atas sistem operasi Microsoft Windows, dengan
menggunakan Microsoft Visual Studio Express 2013 for Desktop.

### Gun Receiver

Software menerima informasi dari senjata melalui software android, yang
ditangani oleh modul Data Interface. Sedangkan senjata itu sendiri dimodelkan
dalam kelas `Senjata` yang mencatat sisa peluru serta pemilik senjata.

### Personal Receiver

Modul ini menangani hal-hal yang terjadi sepanjang permainan, direpresentasikan
dalam kelas `Event`.

### Map Retrieval

Software memanfaatkan Bing Maps API yang disediakan oleh Microsoft. Peta
tersebut ditampilkan pada tampilan utama, yang direpresentasikan dalam kelas
`MainWindow`. Selain itu, kelas `MapDrawer` bertugas mentranslasikan
gerakan-gerakan prajurit ke dalam peta tersebut.

### Tracking Modul

Software menerima informasi dari modul Data Interface, dan mencatat koordinat
prajurit, yang ditangani pada modul Player. Pada software, modul ini
direpresentasikan pada kelas `AbstractGameController` dan `LiveGameController`.

### Shooting Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian menembak akan diteruskan ke modul Player untuk digambarkan pada
modul Map Retrieval.

### Hit Event

Software menerima informasi dari modul Data Interface, dan jika ditemukan
kejadian tertembak akan diteruskan ke modul Player. Software juga
menghitung sisa peluru yang dimiliki senjata. Jika mencukupi, maka prajurit
yang tertembak akan dianggap mati.

### Player

Pada software, prajurit direpresentasikan pada kelas `Prajurit`. Modul ini
mencatat koordinat prajurit, arah pandang, status (mati/hidup, tiarap/berdiri),
nomer urut, nomer induk, nama, regu, dll. Terdapat pula kelas `PrajuritDatabase`
yang berfungsi menyimpan informasi-informasi tersebut dalam modul database.

### Recorder

Modul ini direpresentasikan dalam kelas `EventsRecorder`, yang bertugas
menyimpan `Event` ke dalam modul database.

### Replay

Modul ini berfungsi untuk memutar kembali hasil perekaman yang dilakukan oleh
modul Tracking. Modul ini direpresentasikan oleh kelas `ReplayGameController`.

### Data Interface

Modul ini berfungsi sebagai jalur komunikasi dengan android, maupun dengan
CommandCenter lainnya. Modul ini direpresentasikan dalam kelas
`UDPCommunication` serta `JSONacket`.

Software menggunakan format JSON untuk mengirim/menerima data, di atas protokol
UDP (User Datagram Protocol) yang lebih cepat dibandingkan TCP.

### Database

Software menggunakan basis data SQLite untuk menyimpan informasi pada modul
Player dan Tracking.