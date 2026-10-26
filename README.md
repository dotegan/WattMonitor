# WattMonitor

Aplikasi desktop Windows ringan buat mantau konsumsi daya (Watt), suhu CPU/GPU, dan estimasi biaya listrik PLN secara real-time langsung di layar. Cocok buat yang mau ngecek PC lagi nyedot berapa watt pas idle, gaming, atau rendering tanpa perlu buka software monitoring yang berat.

---

### 🔥 Fitur

- **Real-Time Watt & Suhu**: Baca konsumsi daya dan temperatur hardware (CPU, GPU) secara langsung.
- **Kalkulasi Biaya PLN**: Hitung estimasi tagihan listrik harian sampai bulanan berdasarkan tarif per kWh yang bisa disetel sendiri.
- **Support Peripheral**: Bisa nambahin estimasi watt perangkat eksternal (monitor, fan, motherboard, RAM) biar hitungan total makin akurat.
- **Mode Minimalis / Overlay**: Tampilan ringkas di layar, warna indikator otomatis berubah (Normal / Oranye / Merah) kalau suhu atau watt naik.
- **Auto-Log & Riwayat**: Catatan pemakaian tersimpan otomatis buat evaluasi tagihan listrik.

---

### 💻 Cara Pakai

1. Download atau clone repository ini.
2. Jalankan `WattMonitor.exe` (klik kanan **Run as administrator** biar sensor hardware bisa kebaca penuh).
3. Sesuaikan tarif listrik (Rp/kWh), peripheral, atau tampilan di file `config.txt`.

---

### 🔨 Build dari Source

Tinggal jalankan file build bawaan:

```cmd
cd system
build.bat
```

Compiler memakai `csc.exe` bawaan .NET Framework Windows, jadi tidak butuh install tooling tambahan yang ribet.

---

### 📜 Lisensi

Software ini gratis untuk penggunaan personal.

- Bebas dipakai untuk kebutuhan pribadi, riset, atau belajar.
- **Dilarang keras** menjual, memonetisasi, atau membundel software ini (baik versi asli maupun hasil modifikasi) ke dalam produk/layanan komersial berbayar.

Ketentuan lengkap cek file [LICENSE](LICENSE).
