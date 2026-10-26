# WattMonitor

Aplikasi desktop Windows ringan berbasis C# / WPF untuk memantau konsumsi daya listrik (Watt), temperatur hardware PC, serta estimasi pengeluaran biaya listrik (Tarif PLN Rp/kWh) secara real-time.

---

## ⚡ Fitur Utama

- **Real-Time Hardware Power Monitoring**: Membaca konsumsi daya CPU, GPU, dan sensor perangkat keras lainnya.
- **Peripherals Power Offset**: Dukungan penambahan daya konstan perangkat eksternal (monitor, kipas, motherboard, SSD) via `config.txt`.
- **Kalkulasi Biaya Listrik**: Estimasi biaya listrik harian, bulanan, dan total pemakaian berdasarkan tarif PLN (Rp/kWh).
- **Mode Minimalis (Overlay)**: Widget overlay ringkas di layar dengan kustomisasi font, opacity, serta batas indikator warna suhu & daya (Normal / Oranye / Merah).
- **Laporan Pemakaian Otomatis**: Riwayat pencatatan sesi pemakaian hardware secara periodik.

---

## 🛠️ Persyaratan Sistem

- Windows 10 / Windows 11 (64-bit)
- .NET Framework 4.7.2 atau lebih baru
- Hak akses Administrator (diperlukan oleh sensor perangkat keras)

---

## 🚀 Cara Menjalankan

1. Ekstrak atau unduh rilis aplikasi.
2. Jalankan `WattMonitor.exe` (klik kanan dan pilih *Run as administrator* jika sensor memerlukan izin elevasi).
3. Untuk mengubah tarif listrik atau batas suhu, sesuaikan pengaturan pada file `config.txt`.

---

## 📄 Lisensi

Proyek ini dirilis di bawah lisensi **WattMonitor Non-Commercial License (WNCL)**.

- ✅ **Gratis** digunakan untuk keperluan pribadi, belajar, dan riset non-komersial.
- ❌ **Dilarang keras** memperjualbelikan, menguangkan, atau menyertakan software ini ke dalam produk berbayar/komersial baik dalam bentuk asli maupun hasil modifikasi.

Lihat file [LICENSE](LICENSE) untuk ketentuan hukum selengkapnya.
