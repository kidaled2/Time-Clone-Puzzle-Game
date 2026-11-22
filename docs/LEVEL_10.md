# Bölüm 10 — Ortak Yük

Bir oyuncunun bütün işi yapamayacağı, iki klonun **aynı kutuyu** birbirine devrettiği bir bulmaca. Kapı, plaka ve rampa yoktur. Oyuncu üst platformda başlar; aşağıdaki istasyonlara iniş geri dönüşsüzdür. Son turdaki oyuncunun görevi yukarıda kalmaktır.

Önceki bölümlerden farkı: klonlar ayrı kontrol noktalarını tutmaz, birinci klonun kutuya uyguladığı değişiklik ikinci klonun işini mümkün kılar. İkinci kayıt, birinci kaydın sonunda oluşan dünya durumuna göre yapılır. İki klon da gerekli işi tamamladığında oyuncu herhangi bir dar zaman penceresi olmadan geçebilir.

## Düzen

- Sahne/build: `Assets/Scenes/Level_10.unity`, indeks 10.
- Başlangıç `(0, 1.6, -3)`; iki klon, üç tur, 60 saniye.
- Tek kutu `(0, 0.5, -1)`.
- İlk istasyon güneyden yaklaşır: `(0,-2) → (0,-1)`; kutuyu `(0,0)` ortak noktasına iter.
- İkinci istasyon batıdan yaklaşır: `(-2,0) → (-1,0)`; ortak noktadaki aynı kutuyu `(1,0)` geçidine iter.
- Üst platform: başlangıçtan batıya `(-1,-3), (-2,-3), (-2,-2), (-2,-1)`; sağ kola `(1,-3), (1,-2), (1,-1)`. Çıkış platformu `(1,1), (1,2)`.
- Çıkış `(1,2)`. `(1,0)` altında üst döşeme yoktur.
- Platformların altında destek duvarları vardır. Ek istasyon duvarları `(-1,-2), (-1,-1), (-3,0), (-2,1), (-1,1), (0,1), (2,0)` konumlarında iş alanlarını çevreler. Dış zemin yürünebilir olsa da istasyonlara yandan giriş veya yeniden üst kata çıkış sağlamaz.
- Kamera aşağıdaki çalışma noktalarını göstermek için daha dik açıyla bakar. Kullanıcı tercihi gereği eşleştirme numarası ve sarı kutu hedefi çerçevesi bulunmaz.

## Çözüm

W kuzey/+Z, S güney/-Z, A batı/-X, D doğu/+X.

1. `W2`: ilk istasyona in, kutuyu `(0,0)` noktasına it; ilk klonu oluştur.
2. `A2 W3 D2`: batı balkonundan ikinci istasyona in; ilk klonun getirdiği kutuyu doğuya it; ikinci klonu oluştur.
3. `D W2`: sağ üst balkonun kenarına git. Klonlar aynı kutuyu `(1,0)` noktasına taşıyana kadar bekle. `W3` ile kutu üzerinden çıkışa geç.

İlk istasyondaki aktör, kutuyu kuzeye fazla itemez; önündeki durdurucu ve yan duvarlar batıdaki itme konumuna ulaşmasını engeller. İkinci istasyonun doğusundaki durdurucu kutunun geçit noktasından fazla itilmesini engeller. Dönüşsüz inişler nedeniyle son oyuncu iki hazırlığı kendi başına yapıp yukarı çıkamaz.

## Öğretilen keşif

“Benim bir önceki turda bıraktığım şey, diğer kaydın başlangıç koşulu olabilir.” İlk kayıt yalnızca hareket tekrarı değil, sonraki aktörün kullanacağı bir dünya durumu üretir. Oyuncu her turda çıkışa yaklaşmaya çalışmak yerine iki kaydı farklı işlere ayırır.

Yanlış kola inmek ya da geçit hazır olmadan aşağıya adım atmak reset gerektirebilir. Sabit kutu durdurucuları fazla itişi affeder. Çözümde özel bir bekleme süresi kaydetmek gerekmez; son oyuncu güvenli üst balkonda bekleyebilir. Gerçek oyuncularla keşif süresi ve okunabilirlik henüz ölçülmedi.

## Kabul ve kapsam

`LevelTenTests` gerçek sahnede klavye ve Input System kaydırma + ekrandaki CLONE düğmesi ile tam çözümü doğrular:

- Aynı kutu nesnesi önce ortak noktaya, sonra geçit noktasına ulaşır.
- İki klon farklı alt istasyonlarda, son oyuncu üst katta kalır.
- Fazla itişler, ilk istasyondan ikinci itme konumuna yandan geçiş ve alt kattan başlangıca geri dönüş engellenir.
- Kutusuz erken geçiş oyuncuyu alt kata düşürür; çıkışa tırmanılamaz.
- Gerçek menü düğmesi sahneyi üst başlangıçla açar; sahnede kapı/plaka/rampa yoktur.

Normal akış 9 → 10 → menü; menüden seçilen bölüm mevcut kural gereği tamamlanınca menüye döner. Menü on düğmeye uyarlanmıştır. Önceki bölümün yalnızca klavye çözümü, değişen 9→10 geçişi için tekrar çalıştırılır. Tüm eski bölümler ve fiziksel Android koşusu bu görevde çalıştırılmaz.

22 Kasım 2026: Beş hedefli PlayMode kontrolü ilk koşuda 5/5 geçti (57,57 sn). Yeni bölümün klavye ve dokunmatik tam çözümleri, istasyon/erken düşüş kontrolleri, gerçek menü girişi ve 9→10 geçişi doğrulandı. Bağımsız statik incelemede sahne referansları ve geometride engelleyici bulgu yok. Kanıt: `Logs/Level10/test-results.json`, `level10.png`, `cooperation.png`, `menu-ten.png`. Fiziksel Android ve yeni oyuncularla keşif testi bekliyor.
