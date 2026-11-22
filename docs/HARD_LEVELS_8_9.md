# Zor bölümler: 8 — Görev Devri, 9 — Çift Zamanlı Köprü

Bu bölümler mevcut kayıt, klon, plaka, kapı, rampa ve itilebilir kutu kurallarını kullanır. İkisinde de iki klon hakkı (üç tur) vardır. Zorluk yeni bir kontrol öğrenmekten değil, bir kayıttaki görevin ne zaman tamamlandığını ve sonraki aktörün neyi devralacağını planlamaktan gelir. Geniş bekleme aralıkları hassas refleks gerektirmeden zamanlamayı öğretecek şekilde seçildi. Gerçek oyuncularla zorluk algısı henüz ölçülmedi.

## Bölüm 8 — Görev Devri

**60 saniye.** İlk klon aynı turda iki farklı kapının işini yapmalıdır.

| Kontrol | Yer | Açtığı kapı |
| --- | --- | --- |
| 1, cyan | (-2,-1) | (3,-2), servis odasının girişi |
| 2, magenta | (3,-3) | (1,1), üst yolun ortası |
| 3, amber | (-1,-1) | (3,2), çıkış önü |

Başlangıç (0,-3). Servis odası batıdaki (2,-3) duvarı ve tahta sınırlarıyla çevrilidir; tek giriş kapı 1'dir. Rampa (-3,-2) kuzeye çıkar. Üst yol (-3,-1), (-3,0), z=1 satırında x=-3…3, ardından (3,2) ve (3,3) çıkışıdır. Döşemelerin altında destek duvarları bulunur.

Çözüm; W=+Z, S=-Z, A=-X, D=+X:

1. `A2 W2` ile kontrol 1'e git; yaklaşık **4 saniye bekle**; `D` ile kontrol 3'e geç ve klon oluştur.
2. `W2 D3 S2` ile servis odasındaki kontrol 2'ye git ve ikinci klonu oluştur.
3. `A3 W4 D6 W2` ile üst rotayı geç. Son kapıda ilk klonun kontrol 3'e ulaşması beklenebilir.

İlk kaydı kontrol 1'de bitirmek, kontrol 3'ü boş bırakır ve son kapıyı açmaz. Kontrol 1 çok erken bırakılırsa ikinci oyuncu servis odasına giremez. Dört saniyelik bekleme normal rotada yaklaşık üç saniyelik ek pay bırakır. İkinci klon odaya girdikten sonra servis kapısının kapanması sorun değildir; kalan görev orada durmaktır. Kullanıcı tercihi gereği fiziksel numara işareti eklenmez; mevcut kapı–plaka renkleri kullanılır.

## Bölüm 9 — Çift Zamanlı Köprü

**90 saniye.** Birinci klon önce kutuyla iki geçişi sırayla kurar, sonra servis kapısını açar. İkinci klonun kaydında bilinçli gecikme bulunmalıdır.

- Tek kutu başlangıçta (0,-1). İlk köprü (0,1), ikinci köprü (0,3).
- Kontrol 1 (-2,-1) → servis kapısı (3,-2).
- Kontrol 2 (3,-3), aynı kapalı servis cebi → üst dönüş kapısı (-2,3).
- Rampa (-3,-2). Üst yol (-3,-1), (-3,0), z=1 ve z=3 satırlarında x=-3…3; her iki satırın x=0 karesinde boşluk vardır. Sağ bağlantı (3,2). Çıkış (-3,3).
- Yer seviyesindeki destek duvarları üst yola yan taraftan tırmanılmasını engeller. Kullanıcı tercihi gereği kutu hedefi çerçevesi eklenmez.

Çözüm:

1. `W3` ile kutuyu ilk köprüye it. **7 saniye bekle.** `W2` ile aynı kutuyu ikinci boşluğa taşı. `S2 A2 S` ile kontrol 1'e git ve klon oluştur.
2. Önce `W` ile kayıt başlat. **11 saniye bekle.** Sonra `W D3 S2` ile kontrol 2'ye git ve klon oluştur.
3. `A3 W4 D6 W2 A2` ile sağ platformdan ikinci boşluğun doğu kenarına ulaş. Kutunun (0,3)'e yerleşmesini bekle; `A4` ile çık. (-2,3) kapısı henüz kapalıysa (-1,3) üzerinde ikinci klonu bekle.

İlk geçiş kutu ilk boşluğu terk etmeden yapılmalıdır. Sağ platform güvenli bir bekleme yeridir; ikinci köprü sonradan kalıcı olarak kurulur. İkinci klonun beklemesi ilk geçerli hamleden **sonra** olmalıdır; ilk hamleden önceki düşünme süresi kaydedilmez. Gecikme verilmezse servis kapısına çok erken ulaşılır. Mevcut engellenen klon adımını atlama davranışına güvenilen bir çözüm kullanılmaz.

Alternatif geçerli kayıtlar mümkündür; test rotası tek zorunlu çözüm olarak sunulmaz. Kutunun yanlış yönde ya da fazla itilmesi yeniden deneme gerektirebilir. Mevcut reset akışı kayıtları, kutuyu ve mekanizmaları temizler.

## Giriş ve kabul

- Build sıraları 8 ve 9. Normal akış 7 → 8 → 9 → menü.
- Menüde dokuz bölüm, üç sütun ve üç satır; her iki yeni düğme gerçek menü akışından açılabilir.
- `HardLevelTests`: iki bölümde de gerçek sahneyi temiz başlangıçtan klavye ve Input System dokunmatik kaydırmaları + CLONE düğmesiyle tamamlar.
- 8'de kontrol 1'in bırakılması ve kontrol 3'ün açılması, ikinci klonun kontrol 2'de kalması, üst yol ve bölüm 9'a geçiş doğrulanır.
- 9'da kutunun her iki konumu, iki üst geçişin yüksekliği, ikinci klon gelmeden dönüş kapısının engellemesi ve sonrasında menüye dönüş doğrulanır.
- `LevelSevenTests` çıkış beklentisi bölüm 8 olarak güncellendi; önceki bölümden geçiş ayrıca hedefli doğrulanır.
- Tüm eski bölümler yeniden oynanmaz. Fiziksel Android ve yeni oyuncularla zorluk/ipuçları değerlendirmesi sonraki toplu doğrulamaya kalır.

## Doğrulama sonucu — 22 Kasım 2026

Altı hedefli kontrol tamamlandı: 8 ve 9'un klavye/dokunmatik tam çözümleri, iki yeni menü düğmesi ve 7→8 geçişi. İlk koşuda dört kontrol geçti; 8'in iki kontrolündeki anlık plaka değişimi beklentisi, klonun hareketi tamamlamasını bekleyecek şekilde düzeltildi ve yalnızca bu iki kontrol tekrar çalıştırılarak 2/2 geçti. Kanıtlar: `Logs/HardLevels/initial-results.json`, `handoff-retest-results.json`, `level8.png`, `level9.png`, `menu-nine.png`.

Test yardımcısında geçici InputSettings kopyasının yok edilmesi, sonraki domain reload sırasında Unity'de boş referans bırakıyordu. `GameInputScope` iki ayarı aynı nesnede saklayıp geri yükleyecek şekilde düzeltildi; derleme sonrası girdi durumu ve art arda hedefli koşular doğrulandı. Oynanış kuralları değiştirilmedi. Test tamamlandı bildirimi geldiğinde editör hâlâ Play Mode'dan çıkıyor olabilir; script derlemesi veya başka koşu başlatmadan önce hem `isPlaying` hem `isPlayingOrWillChangePlaymode` false olmalıdır.

Kullanıcının son tercihine göre 7–9'daki bütün fiziksel numara ve kutu hedefi işaretleri kaldırıldı. Bu son değişiklik yalnızca görsel nesneleri etkiledi; sahne farkları ve yeni görüntüler kontrol edildi, çözüm testleri tekrar oynatılmadı. Bölüm 9'un kamerası çıkış ışığını kadraja alacak şekilde yükseltildi.
