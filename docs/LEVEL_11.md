# Bölüm 11 — Rezonans Kilidi

Üç koldaki aynı şekle sahip plakaları eşzamanlı tutarak çıkış kilidinin üç aşamasını tamamla. Her kolda bir üçgen, bir baklava ve bir halka var. Şekillerin koldaki sırası değişiyor. Sol alttaki gösterge baştan itibaren kilidin istediği sırayı ve o anda kaç hedefin tutulduğunu gösterir. Tamamlanan aşama korunur. Son halkadan ayrılınca kapı açık kalır.

Başlangıç merkezde, çıkış hemen güneyde. Üç tur, iki klon ve bölüm geneli için 105 saniye var. Kayıt ilk geçerli adımla başlar. Diğer iki aktöre zaman bırakmak için ilk iki sembolde durakla. Duraklamaların tam uzunluğunu oyuncu seçer.

## Bir çözüm

Yönler oyuncunun hareket eksenlerine göredir. W kuzey, D doğudur. Her satır bir önceki satırın sonundan devam eder.

| Hedef | Batı kolu · ilk kayıt | Doğu kolu · ikinci kayıt | Kuzey kolu · son tur |
|---|---|---|---|
| Merkezden üçgene | A×2 | D×3 S | W×3 D×2 S |
| Üçgenden baklavaya | A W×2 D | W A | W A |
| Baklavadan halkaya | A S | D S×2 A | A S |

İlk iki turda üçgen ve baklavada birkaç saniye bekle, halkada kaydı bitir. Son turda kuzey kolunda her aşamanın geçmesini bekle. Üç halka birlikte tutulunca kuzey halkasından S×4 ile çıkışa in. Oyuncu ve klonlar kolları paylaşırken istedikleri rolü üstlenebilir.

## Doğrulama

`LevelElevenTests` menü girişini ve gerçek sahnede hem klavye hem de kaydırma / CLONE girdisiyle çözümü kapsar. Kapının başlangıçta geçit vermediğini, ilk iki turda ilerleme olmadığını ve üç aşama sonunda kalıcı olarak açıldığını kontrol eder. Normal kampanya geçişi 10 → 11 → ana menüdür.
