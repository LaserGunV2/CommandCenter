# Gambaran Umum

```
                                             |
  +--+         21500   +--+          21501 +-+
  |  |   ---------->   |  |   -----------> | |
  +--+   <----------   +--+   <----------  +-+
 /____\  21500        /____\  21500         \ \
                                             \_\

Pemantau          CommandCenter           Android
```


Android mengirimkan paket-paket UDP ke Command Center pada port 21500, berisi string JSON yang dienkodekan dalam ASCII. Setiap JSON memiliki paling tidak satu parameter yaitu `type`, yang menyatakan ini type pesan apa.

Sebaliknya, Command Center mengirimkan paket-paket ke Android pada port 21501.

Contoh:

```javascript
{
    type: "event/update",
    ....
}
```

Parameter-parameter lainnya ditentukan oleh isi dari parameter `type`.

# Daftar Tipe

## `register`

Event ini digunakan Android untuk mendaftarkan diri ke server sesuai dengan game id nya. Paket ini di broadcast.

```javascript
{
    type: "register",
    gameid: "245",
    nomerInduk: "2003730013"
}
```

Paket ini akan direply dengan paket `confirm`

## `confirm`

Event ini digunakan server untuk membalas request `register`. Paket dikirimkan ke IP address android yang mengirim request register tadi.

```javascript
{
    type: "confirm",
    androidId: 3
}
```

## `event/update`

Tipe ini digunakan untuk melakukan update posisi prajurit tertentu. Isinya sesuai contoh di bawah ini:

```javascript
{
    type: "event/update",
    gameid: "245",
    androidId: 3,
    location: "-6.87484,107.60588",
    accuracy: 279,
    heading: 180,
    action: "move",
    state: "alive/stand",
    idsenjata: 5, /* jika action == hit */
    counter: 123 /* jika action == hit */
}
```

action bisa berisi:

* `move`: update posisi prajurit
* `hit`: update + prajurit tertembak
* `shoot`: update + prajurit menembak 

state bisa berisi:
* `alive/stand`: artinya prajurit masih hidup dan sedang berdiri
* `alive/crawl`: artinya prajurit masih hidup dan sedang tiarap
* `dead/stand`: artinya prajurit sudah mati dan sedang berdiri
* `dead/crawl`: artinya prajurit sudah mati dan sedang tiarap

### Penjelasan: `hit`

Jika action adalah hit, maka paket akan berisi tiga informasi tambahan:

* `idsenjata`: menunjukkan id senjata
* `counter`: counter dari id senjata tersebut.
* `idsensor`: id sensor pada harness yang terkena laser.

Jika event hit ini dikirimkan saat registrasi, artinya assign senjata (`androidId` akan diasosiasikan sebagai pemilik dari senjata dengan `idsenjata` ini).

Jika event hit dikirimkan pada saat latihan, artinya prajurit ini tertembak. Command Center akan memeriksa counternya, apakah peluru penembak masih tersedia. Jika masih ada peluru, maka Command Center akan mengirimkan pesan `killed` ke prajurit ini.

## ~~`killed`~~

Paket ini _deprecated_, gunakan `setalive` dengan `alive` diset menjadi `false`.

Dikirimkan untuk menyatakan bahwa prajurit yang bersangkutan terbunuh (akibat tertembak).

```javascript
{
    type: "killed"
}
```

## `setalive`

Dikirimkan untuk menyatakan bahwa prajurit yang bersangkutan terbunuh (akibat tertembak)
atau hidup lagi (_revive_).

```javascript
{
    type: "setalive",
    alive: true
}
```

`alive` bisa diisi `true` untuk melakukan _revive_, atau `false` untuk membunuh.

## `endgame`

Perintah ini dikirimkan oleh server untuk memberitahu setiap prajurit bahwa permainan sudah berakhir. Android jika menerima paket ini akan kembali ke state unregistered.

```javascript
{
    type: "endgame"
}
```

## `ping`

Dikirimkan oleh client untuk mengukur kecepatan jaringan. Jika server menerima paket ini, akan mengembalikan paket `pong`

```javascript
{
    type: "ping",
    sentTime: 1412838239,
    packetId: 13
}
```

sentTime berisi nilai unix time, menyatakan kapan paket ini dikirim. packetId dapat diisi dengan bilangan bebas. Kedua nilai ini akan dikembalikan pada paket `pong`.

## `pong`

Digunakan untuk membalas paket `ping`

```javascript
{
    type: "pong",
    sentTime: 1412838239,
    packetId: 13
}
```

sentTime dan packetId dikembalikan sesuai paket ping yang dikirim.

## `pantau/register`

Event ini digunakan pemantau untuk mendaftarkan dirinya. Paket ini dibroadcast, dan jika berhasil akan dibalas dengan paket `pantau/confirm`.

```javascript
{
    type: "pantau/register",
    gameid: "245",
}
```

## `pantau/confirm`

Event ini digunakan server untuk membalas request `pantau/confirm`. Paket dikirimkan ke IP address pemantau yang mengirim request pantau register tadi.

```javascript
{
    type: "pantau/confirm",
    status: "ok", // ok berarti berhasil, selain itu gagal dan atribut ini akan berisi pesan kesalahan.
    gameid: "245",
    ammo: 100
}
```

## `pantau/state`

Event ini digunakan server untuk mengirimkan perubahan state pada pemantau.

```javascript
{
    type: "pantau/state",
    state: "START" // atau "STOP"
}
```
