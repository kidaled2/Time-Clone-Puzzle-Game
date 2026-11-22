# Bölüm 7 — Servis Odası

İki klon kontrol noktalarını tutarken son turdaki oyuncu kutuyu bir köprüye dönüştürür. Bölüm 3'ün iç içe kapı bağımlılığını, 5–6'nın kutu ve yükseklik kuralıyla birleştirir. Yeni bir mekanik eklemez. Önceki kutulu bölümlerin temel çözümünden farklı olarak burada köprüyü son turdaki oyuncu hazırlayabilir; klonun köprüyü önceden hazırlaması da geçerli bir alternatif çözümdür.

## Düzen

- Sahne: `Assets/Scenes/Level_7.unity`; build sırası 7.
- Başlangıç: `(0,-3)`, yer seviyesi `Y=0.5`.
- İki klon hakkı, üç tur, bölüm genelinde 60 saniye.
- Kontrol 1: `(-2,-1)` → servis kapısı `(3,-2)`.
- Kontrol 2: `(3,-3)` → üst çıkış kapısı `(3,2)`.
- Servis cebinin batısı `(2,-3)` duvar, doğusu ve güneyi tahta sınırı. Tek girişi servis kapısıdır.
- Kutu: `(2,0)`. Kuzeye bir adım itilince `(2,1)` boşluğunda köprü olur.
- Rampa: `(-3,-2)`, kuzeye çıkış. Üst yol `(-3,-1)`, `(-3,0)`, `(-3,1)` üzerinden doğuya uzanır. `(2,1)` karede üst döşeme yoktur. Karşı yakada `(3,1) → (3,2) → (3,3)` çıkışı vardır.
- Kullanıcı tercihi gereği kapı/plaka numarası ve kutu hedefi çerçevesi bulunmaz. Mevcut kapı–plaka renkleri kullanılır.

```text
                 x: -3 -2 -1  0  1  2  3
z= 3                 .  .  .  .  .  .  E
z= 2                 .  .  .  .  .  .  D2
z= 1                 U  U  U  U  U  ~  U
z= 0                 U  .  .  .  .  B  .
z=-1                 U  P1 .  .  .  .  .
z=-2                 R  .  .  .  .  .  D1
z=-3                 .  .  .  S  .  #  P2

U üst döşeme + altta destek duvarı; ~ boş köprü karesi;
P plaka; D kapı; B kutu; R rampa; # servis cebi duvarı.
```

## Çözüm

W kuzey/+Z, S güney/-Z, A batı/-X, D doğu/+X. Sayı aynı yöndeki adım sayısıdır.

1. İlk kayıt: `A2 W2`. Kontrol 1'e bas, klon oluştur.
2. İkinci kayıt: `W2 D3 S2`. Birinci klon servis kapısını açarken kontrol 2'ye ulaş, klon oluştur.
3. Son tur: `W2 D2 W` ile kutuyu köprü karesine it. `S A2 S2 A3 W4` ile rampadan çık. `D6 W2` ile köprü ve üst kapı üzerinden çıkışa ulaş.

Kapı önüne erken gelinirse birinci klonun plakaya ulaşması beklenebilir. Plakalarda kalan klonlar kapıları sürekli açık tutar; son geçiş dar bir zaman penceresi gerektirmez. Başlangıçtan doğrudan `D2` servis cebinin duvarına çarpar; kutuya merkez koridordan yaklaşılır.

## Keşif ve hata noktaları

- Oyuncu servis cebine kendi başına giremez. İlk klonun görevi ikinci kaydı mümkün kılmaktır.
- Üst yola erken çıkan oyuncu, boşluğu geçerken alt kata düşer ve karşı desteğe tırmanamaz. Köprü için kutunun üst yüzeyi gerekir.
- Üst yoldan aşağı atlamak, güneydeki kapalı servis cebine doğrudan erişim sağlamaz.
- Kutu fazladan itilirse köprü bozulabilir; mevcut reset komutuyla hızlı yeniden deneme yapılır. Kayıtlar kutunun itildiği bir çözümü de destekler.
- Klonlar kendi başlarına çıkışı tetiklemez; üçüncü turdaki oyuncunun çıkışa ulaşması gerekir.

## Kabul ve doğrulama

`LevelSevenTests` gerçek sahneyi temiz başlangıçtan açar:

- Klavye ile iki kayıt, iki sabit klon, son turda kutu itme, üst köprü ve Bölüm 8'e geçiş.
- Input System dokunmatik kaydırmaları ve ekrandaki CLONE düğmesine dokunarak aynı tam çözüm.
- Servis kapısı kapalıyken cebe girilememesi, yan duvarın kestirmeyi engellemesi, kutusuz üst geçişte alt kata düşülmesi.
- Tur değişiminde kutunun başlangıç konumu ve son turda klon haklarının bitmesi. Güncel bölüm menüsü dokuz düğme içerir.
- Gerçek menü düğmesinin tıklanmasıyla Bölüm 7'nin yüklenmesi ve bölüm seçimi modunun kurulması.
- Bölüm 6'nın mevcut tam çözüm testinde sonraki sahne beklentisi 7 olarak güncellendi; geçiş ayrıca hedefli test edilir.

İlk doğrulamada yalnızca Bölüm 6–7 ile ilgili testler çalıştırıldı. Fiziksel Android koşusu bu teslimin kapsamına dahil değildir. Bölüm ana menü seçiminden açılabilir; Start akışında bölüm 6'dan devam eder ve tamamlanınca artık Bölüm 8'e geçer. Bölüm seçiminden açıldığında mevcut kural gereği menüye döner. Yeni devam bölümleri için [8–9 tasarım notları](HARD_LEVELS_8_9.md).

22 Kasım 2026 doğrulaması: dört hedefli PlayMode testi 4/4 geçti (55,85 sn); ek menü giriş testi 1/1 geçti (0,69 sn). Sonraki numara konumu ve menü aralığı ayarları görsel olarak kontrol edildi. Kanıtlar: `Logs/Level7/test-results.json`, `menu-test-results.json`, `level7-layout.png`. Tam bölüm paketi ve fiziksel Android koşusu çalıştırılmadı.
