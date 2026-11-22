# Bölüm 12 — Dar Viraj

İlk klon turkuaz plakayı tutar ve dar koridorun girişini açar. İkinci kayıtta oyuncu, ilk kasayı bir dönüş cebine ve ikinci kasayı koridorun ucuna iterek mor plakaya ulaşır. Son turda ikinci klon itişleri tekrarlar ve mor plakayı tutar. Oyuncu açılan koridordan çıkışa gider.

İlk kasa düz devam ederse ikinci kasaya dayanır. Alt döşemeler, ilk kasanın arkasına geçip onu kuzeye itmek için kullanılır. İki klon hakkı ve bölüm genelinde 90 saniye vardır. Kamera ilk altı bölümdeki 45° eğimi ve −45° yatay dönüşü kullanır.

## Bir çözüm

W kuzey, S güney, A batı, D doğudur. Örneğin `D×2` doğuya iki adım demektir.

1. `A S` ile turkuaz plakaya git ve birinci klonu oluştur.
2. İlk kapı açılınca `W A W×2 D×2` ile ilk kasayı iki kare doğuya it. `S D W` ile etrafından dolaşıp kuzeye it. `D W×2` ile ikinci kasayı yoldan çıkar, mor plakaya bas ve ikinci klonu oluştur.
3. İki klon plakalarına ulaştıktan sonra `W A W×2 D×4 W D×2` ile temizlenen koridordan çık.

## Doğrulama

`LevelTwelveTests` menü girişini, turkuaz kapının plaka bırakılınca kapanmasını ve klavye ile dokunmatik girdilerde tam çözümü kapsar. İki kasa itildikten sonra mor plaka tutulana kadar çıkış kapısı kapalı kalır. Kampanya akışı 11 → 12 → menüdür.
