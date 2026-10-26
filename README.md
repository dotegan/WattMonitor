# ⚡ WattMonitor

> *"Daripada jaga lilin tiap malam buat bayar tagihan PLN, mending pantau watt PC lu dari awal."*

<p align="center">
  <img src="assets/meme-lilin.jpg" alt="Meme Jaga Lilin vs Tagihan Listrik" width="360" />
</p>

Aplikasi desktop Windows ringan berbasis C# WPF Native buat mantau konsumsi daya listrik (Watt), suhu CPU & GPU, serta langsung mengonversinya jadi estimasi **Rupiah tagihan PLN secara real-time**. 

Gak perlu buka software monitoring berat nan ribet kalau cuma mau ngecek PC lagi nyedot berapa ratus watt pas lu tinggal render, compiling, atau maraton game 4K.

---

## 📸 Tampilan Antarmuka

<p align="center">
  <img src="assets/ss-app-1.png" alt="WattMonitor Minimalist HUD" width="680" />
</p>

<p align="center">
  <img src="assets/ss-app-2.png" alt="WattMonitor Full Dashboard & Context Menu" width="680" />
</p>

<p align="center">
  <img src="assets/ss-app-3.png" alt="WattMonitor Laporan Pemakaian & Statistik" width="680" />
</p>

---

## 🔥 Kenapa Harus Pakai WattMonitor?

- **Real-Time Watt & Suhu**: Baca konsumsi daya dan temperatur hardware (CPU & GPU) langsung dari sensor tanpa delay.
- **Kalkulator Tagihan PLN Instan**: Otomatis hitung estimasi pengeluaran per jam, per hari (misal aktif 10-12 jam), sampai akumulasi sebulan penuh berdasarkan tarif resmi Rp/kWh rumah lu.
- **Support Beban Peripheral**: Kipas RGB 7 biji, monitor 27 inch 165Hz, sampai RAM kelap-kelip bisa lu daftarkan watt statisnya biar hitungan total gak meleset.
- **Dua Mode Tampilan**:
  - **Minimalist HUD**: Widget tipis tembus pandang di pojok layar. Gak bakal nutupin game atau IDE coding.
  - **Full Dashboard**: Statistik lengkap dengan indikator batas warna (Normal ➔ Oranye ➔ Merah kalau PC mulai kesurupan panas).
- **Laporan Pemakaian & Riwayat Bulanan**: Tersimpan otomatis dalam format rapi, cocok buat evaluasi kenapa tagihan listrik bulan ini mendadak bengkak.
- **GUI Pengaturan Modern**: Atur tarif, slider transparansi, batas threshold, dan peripheral langsung lewat jendela setting tanpa perlu buka Notepad kayak jaman purba.
- **Bukan Bloatware**: Native WPF, startup instan, konsumsi RAM tipis, dan jalan mulus di Windows 10/11.

---

## 💻 Cara Pakai

1. Download rilisan terbaru atau clone repo ini.
2. Klik kanan **`WattMonitor.exe`** ➔ **Run as administrator** *(Wajib hukumnya, biar sensor hardware Windows ngasih data jujur)*.
3. Klik tombol ikon gear ⚙️ atau klik kanan widget untuk atur tarif listrik PLN rumah lu (default: Rp 1.444,70 / kWh).
4. Nikmati hidup sambil memantau detik-detik PC lu menguras dompet secara visual.

---

## 🔨 Build dari Source (Tanpa Tooling Ribet)

Project ini tidak butuh `node_modules` 2GB atau compiler aneh-aneh. Cukup pakai compiler `csc.exe` bawaan Windows:

```cmd
cd system
build.bat
```

Selesai dalam hitungan 2 detik dan menghasilkan file biner siap jalan.

---

## 📜 Lisensi & Aturan Main

Proyek ini dirilis di bawah **WattMonitor Non-Commercial License (WNCL)**.

- ✅ **Bebas & Gratis** dipakai untuk kebutuhan pribadi, riset, hobi, atau belajar.
- ❌ **DILARANG KERAS** memperjualbelikan, menguangkan, atau membundel software ini (baik versi asli maupun hasil modifikasi) ke dalam produk/layanan komersial berbayar dalam bentuk apa pun.

Ketentuan hukum lengkap bisa dibaca di file [LICENSE](LICENSE).
