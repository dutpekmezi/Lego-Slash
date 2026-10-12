# Dijital oyun masası: katalog temelli AR-GE ve satın alma çalışması

Araştırma tarihi: 8 Ekim 2026. Hedef: Bambu Games benzeri, aynı masa etrafında oynanan çok oyunlu dokunmatik sistem. Donanım değerlendirmesi mevcut Unity projesine değil, piyasadaki ürünlerin oyun kataloğuna dayanır. İlk çalışma varsayımı bir prototip, kapalı mekân, dört oyuncu ve gerektiğinde altı oyuncudur. Bütçe, adet ve kullanım yeri henüz kesinleşmedi.

## 1. Araştırmanın sonucu

Önerilen başlangıç: yatay kullanımına üretici tarafından izin verilen 43 veya 55 inç 4K PCAP ekran; en az 20, tercihen 40 eşzamanlı temas; modern entegre grafikli bilgisayar; 16–32 GB çift kanallı RAM ve 1 TB NVMe SSD. İlk prototipte harici ekran kartına bütçe ayırmak yerine dokunma kalitesi, kasa, soğutma ve gizli el gösterme çözümünü doğrulamak daha anlamlıdır.

Bu bir performans garantisi değildir. Rakibin yazılımı, render maliyeti, gerçek işlemci modeli ve kare süreleri kamuya açık değil. Aşağıdaki donanım, benzer katalog için tasarım adayıdır; kabulü gerçek oyunlarla yapılan test belirler.

## 2. Referans üründe hangi oyunlar bulunuyor?

Bambu'nun Türkçe sitesinde başlık 75+ oyun derken filtre toplamı araştırma sırasında 73 gösteriyordu. Mağaza başlıklarında başka sayılar bulunması nedeniyle satın alınan paketin sürümü ve tam listesi ayrıca istenmeli. Örnekler: Okey, 101 Okey, Batak, Pişti; tavla, satranç, dama, domino; Metropoly; AirHokey, Pong, 8 Ball Pool, BasketGo, Football; Parchis 3D; 2048, hafıza ve bulmaca oyunları. Bu katalog hem sıra tabanlı hem hızlı eşzamanlı oyunlar içeriyor. Metropoly adını lisanslı Monopoly olarak yorumlamıyoruz. [Bambu resmi katalog](https://bambugames.com.tr/)

| Oyun ailesi | Donanım açısından anlamı | Doğrulama senaryosu |
|---|---|---|
| Taş/kart ve sıra tabanlı masa oyunları | Grafik yükü genellikle sınırlı; küçük yazı, doğru sürükleme ve gizli bilgi önemli | Dört kişinin aynı anda taşlarını düzenlemesi; yanlış oyuncunun alanına giriş |
| Açık tahta ve strateji oyunları | Görüş açısı, ulaşılabilirlik ve koltuğa göre arayüz yönü önemli | Karşılıklı oturanların metinleri okuyabilmesi |
| Hızlı arcade ve spor oyunları | Kare süresi, giriş gecikmesi ve temas kimliği sürekliliği önemli | Karşılıklı hızlı sürükleme; parmaklar yakınlaşırken kontrolün karışmaması |
| Hafif 3D ve fizik oyunları | Entegre GPU'nun gerçek uygulamayla sınanması gerekir | En ağır sahnede uzun süreli 60 FPS hedefi |
| Çocuk, çizim ve bulmaca | Avuç, birden çok parmak, yanlış basış ve temizlik koşulları önemli | Çoklu temas, el kaldırma, ekranın kenarlarında çizim |

Bu tablodaki yük değerlendirmeleri oyun türlerinden çıkarılan mühendislik tahminleridir. Rakibin oyunları üzerinde ölçüm yapılmadı. Aynı anda onlarca oyunun çalışması gerekmez: normal mimari bir oyun oturumu ve hafif bir yönetim kabuğu çalıştırır. Kütüphanenin büyüklüğü esas olarak depolama, güncelleme ve kalite kontrol yükünü artırır.

## 3. Pazardaki seçenekler

| Seçenek | Araştırmada doğrulanan bilgi | Karar açısından anlamı |
|---|---|---|
| Bambu Games hazır masa | 4K, altı kişiye kadar kullanım, oyun kütüphanesi ve gizli el gösterme özelliği üretici tarafından sunuluyor | Hazır ürün ve kendi geliştirme maliyetini karşılaştırırken yazılım ve servis de hesaba katılmalı |
| Fink Games | Üretici sitesinde 51 oyun, çoklu dokunma ve kare tasarım belirtiliyor | Türkiye'de demo ve teklif istenebilecek ikinci hazır ürün adayı |
| iiyama + ayrı PC + özel kasa | Teknik özellikleri açık, değiştirilebilir parçalar | Kendi platformunu geliştirmek için güçlü aday |
| Elo + ayrı PC + özel kasa | Masa üstü kullanım için tasarlanan ekran seçenekleri | Alternatif profesyonel ekran teklifi |
| TV + IR çerçeve | Parçalar ayrı temin edilebilir | Yatay çalışma, yüzey dayanımı ve yanlış temas davranışı doğrulanmadan ticari ürün temeli sayılmamalı |

Kaynaklar: [Bambu](https://bambugames.com.tr/about), [Fink](https://fink.games/), [iiyama](https://iiyama.com/gl_en/products/prolite-tf4339as-b1ag/), [Elo 5554L](https://www.elotouch.com/touchscreen-signage-5554l.html).

Türkiye'deki Bambu ilanında 299.000 TL liste fiyatı ve 16 GB RAM/256 GB SSD bilgisi görüldü. İşlemcinin tam nesli, ekranın teknik modeli ve donanım revizyonu bu bilgilerle belirlenemiyor. Bu tutar kesin satış teklifi değildir. Avrupa'daki Kickerkult sayfasında 7.200 avro, vergi dahil ve kargo hariç fiyat görülüyor; ürün o anda siparişe açık değildi. Türkiye maliyetine doğrudan kur çarpımıyla çevrilmemeli. [Türkiye ilanı](https://www.trendyol.com/bambu-games/dijital-oyun-masasi-hayalet-ekranli-55-oyun-4k-kiosk-p-1059610527), [Avrupa ilanı](https://www.kickerkult.de/Bambu-Games-Spieltisch/11740)

Bu tarama pazar payı veya toplam talep ölçümü değildir. Gerçek ticari potansiyel için 5–10 hedef işletmede oyuncu ilgisi, doluluk, servis ihtiyacı ve ücretli kullanım isteği ölçülmeli; üreticilerin gelir artışı iddiaları iş planında doğrulanmış sonuç gibi kullanılmamalı.

## 4. Ekran seçim şartnamesi

- Boyut: önce dört oyuncu için 43 ve 55 inç tam boy maket karşılaştırması.
- Çözünürlük: 3840 × 2160; 60 Hz. Küçük kart yazıları ve taşlar için faydalı.
- Panel: geniş görüş açılı IPS veya eşdeğeri. Mat/anti-glare yüzey.
- Dokunma: PCAP, gerçek çoklu temas; en az 20, tercihen 40 temas.
- Üretici onayı: face-up/tabletop, yani ekran yüzeyi tavana bakarken çalışma. Landscape/portrait ibaresi tek başına yeterli değil.
- Bağlantı: 4K 60 Hz destekleyen HDMI/DisplayPort ve ayrı USB touch; tüm kablo zinciri doğrulanmalı.
- Kullanım: uzun günlük çalışma için uygunluk; kapalı masa kasasında sıcaklık sınırı ve garanti kapsamı yazılı alınmalı.
- Cam: ek koruyucu cam kullanılacaksa panel üreticisinin izin verdiği kalınlık, boşluk ve temas sayısıyla test edilmeli.
- Sıvı: ekranın ön yüz koruma derecesi ile tüm masanın sızdırmazlığı ayrı değerlendirilir.

### Somut ekran adayları

**iiyama ProLite TF4339AS-B1AG:** 43 sınıfı 4K IPS, 40 nokta PCAP, face-up kullanım, 24/7 çalışma, ön yüz IP65; touch yüzeyinden yaklaşık 435 nit parlaklık. İlk prototip için güçlü aday. Dahili Android birimi mevcut olsa da harici PC kullanımı tercih edilebilir. [Üretici](https://iiyama.com/gl_en/products/prolite-tf4339as-b1ag/)

**iiyama ProLite TF5539AS-B1AG:** 55 sınıfı 4K IPS, 40 nokta PCAP ve face-up kullanım. Dört kişilik daha geniş oyun alanı ve altı kişilik bazı oyunlar için değerlendirilir. [Üretici](https://iiyama.com/gl_en/products/prolite-tf5539as-b1ag/)

**Elo 5554L:** 55 inç 4K profesyonel ekran; PCAP konfigürasyonunda 40 temasa kadar ve masa kullanım desteği. Sipariş edilen touch varyantı ayrıca kontrol edilmeli. Masa kullanımında ortam sıcaklığı sınırı teknik dokümanda 0–35 °C; kapalı kasanın içi bu sınırı aşabilir. [Teknik föy](https://docs.elotouch.com/Elo_5554L_DS.pdf), [Montaj/kullanım kılavuzu](https://docs.elotouch.com/UM600721.pdf)

Türkiye'de iiyama için idDisplay/İES'in ilgili ürün sayfası mevcut; açık fiyat olmadığı için yazılı teklif gerekir. [Türkiye ürün sayfası](https://www.iddisplay.com.tr/akillisite/iiyama-prolite-tf4339as-b1ag-43-dokunmatik-monitor-493)

Model son eklerine dikkat edilmeli: eski TF4339MSC ile yeni TF4339AS aynı ürün değildir. Boyut ve marka eşleşmesi teknik eşdeğerlik anlamına gelmez.

### PCAP mı IR mı?

PCAP ilk tercih. Düz yüzeye entegrasyonu ve masa tasarımına uygunluğu iyi bir başlangıç sağlar. IR çözüm prototipte maliyet alternatifi olabilir; algılama düzlemine giren nesneler, kir ve çerçeve geometrisi gerçek kullanımda test edilmeli. PCAP da su ve avuç davranışında kusursuz kabul edilmez. Teknoloji adı tek başına performans garantisi değildir. [Elo teknoloji karşılaştırması](https://docs.elotouch.com/ELO_Touch_Technology_Comparison_19121AEB00023.pdf)

## 5. Gizli taş ve kartların gösterilmesi

Okey/101 ve bazı kart oyunlarında oyuncuya özel bilgi gerekir. Normal geniş görüş açılı ekran bunun için yeterli değildir. Ortak ekranın dört kenarına dört el çizmek yalnızca yerleşim sağlar; diğer oyuncunun görmesini engellemez.

Bambu bunu 'Hayalet Ekran' adıyla pazarlıyor. Patent ve performans ifadeleri üreticinin iddialarıdır; mekanizma, patent kapsamı ve farklı oturma açılarındaki başarısı bağımsız olarak doğrulanmadı. Bu çözüm sıradan dokunmatik ekran özelliklerinden ayrı bir AR-GE başlığıdır. [Üretici açıklaması](https://bambugames.com.tr/en)

Üç uygulanabilir geliştirme yolu:

1. **Ana ekran + oyuncu başına küçük özel ekran:** Ana oyun tahtası ortada, gizli el oyuncuya dönük küçük ekranda. İlave ekran, kablo, ekran çıkışı ve bakım maliyeti vardır. Dört 4K özel ekran gerekmiyor; düşük çözünürlüklü küçük ekranlar yeterli adaydır.
2. **Ana ekran + telefon üzerinden özel el:** QR ile yerel oturuma katılma. Optik AR-GE maliyetini düşürür; telefonsuz müşteri, bağlantı ve kullanım kolaylığı sorunları pilotta ölçülmeli.
3. **Özel optik/mekanik gizlilik:** Tedarikçi ortaklığı veya kendi çözümün. Oturma konumları, yansıma, ayağa kalkma ve yan bakışla test edilmeden tasarım dondurulmamalı.

İlk yazılım prototipi açık bilgili oyunlarla başlayabilir. Ancak hedef ticari katalogda Okey önemliyse, gizlilik çözümü projenin sonuna bırakılamaz. Dört mini ekran seçilirse aşağıdaki ana ekranlı bütçeye ek maliyet gelir.

## 6. İşlemci ve ekran kartı

**Önerilen geliştirme bilgisayarı:** AMD Ryzen 5 8600G, entegre Radeon 760M, servis edilebilir AM5 anakart ve çift kanallı DDR5. 8600G'nin 6 çekirdek/12 iş parçacığı ve entegre grafik yapısı üretici tarafından belirtiliyor. Bu sınıf, katalogdaki masa oyunları ve hafif arcade/3D içerik için ölçülmeye değer başlangıçtır. [AMD](https://www.amd.com/en/products/processors/desktops/ryzen/8000-series/amd-ryzen-5-8600g.html)

**Hazır alternatif:** Garantili, fanlı ve sürdürülebilir tedariki olan Core i5/Ryzen mini PC. Örneğin Türkiye'de ASUS PB63 i5-13400, UHD730, 16 GB, 512 GB ve Windows 11 Pro konfigürasyonu listeleniyor. Bu örnek hazır tedarik seçeneğidir; her oyun için 4K 60 FPS ölçülmüş sonuç değildir. [Türkiye ürün ilanı](https://www.vatanbilgisayar.com/asus-mini-pc-pb63-intel-core-i5-13400-3-3ghz-16gb-512gb-ssd-win11-pro.html)

Harici ekran kartı ilk satın alma listesinde zorunlu değil. En ağır oyun prototipinde 60 FPS hedefi karşılanmıyorsa önce profiler ile GPU/CPU darboğazı ayrılır. GPU sınırlaması doğrulanırsa kart eklenir. Ana arayüzü 4K tutup ağır 3D sahneyi daha düşük iç çözünürlükle render etmek de seçenek. Sırf 4K görüntü çıkışı var diye RTX gerekmiyor; video çıkışı ve oyun render performansı farklı konular.

Fanless bilgisayar sessiz olabilir ama kapalı kasada sürekli yük ve yüksek ortam sıcaklığında termal kapasitesi doğrulanmalı. İlk prototip için kaliteli düşük devirli fan daha kolay yönetilebilir.

## 7. RAM, SSD ve diğer parçalar

| Parça | Başlangıç şartı | Satın alma tercihi / gerekçe |
|---|---|---|
| RAM | 16 GB çift kanal | 32 GB, geliştirme ve gelecek içerikler için rahatlık; şart değil |
| SSD | 512 GB NVMe | 1 TB NVMe; güncelleme geri dönüşü, log ve kütüphane payı |
| HDD | Gerekmiyor | Mekanik disk kullanılmaz |
| Anakart | 4K 60 Hz çıkış, yeterli USB | Ethernet, erişilebilir servis ve uygun kasa ölçüsü |
| PSU | Gerçek tüketim ve genişleme planına uygun kaliteli model | APU sisteminde yüksek watt oyun PSU'su gereksiz; ekran çoğunlukla kendi beslemesini kullanır |
| Ses | 2 küçük hoparlör ve gerekirse amfi | Ayarlanabilir ve işletmede sınırlandırılabilir ses |
| Ağ | Ethernet | Wi-Fi yedek; yerel oyun internet kesilince sürmeli |
| UPS | Ölçülen sistem tüketimine göre | Kontrollü kapanma için; VA ile watt aynı kabul edilmez |
| Bakım | Kilitli servis bölümü | PC ve fanlar ekran sökülmeden değişebilmeli |

SSD kapasitesi için örnek plan: işletim sistemi/yardımcılar 100 GB, oyunlar 100–200 GB varsayımı, güncelleme ve önceki sürüm 100 GB, kalan boş alan. Gerçek oyun paketleri alınmadan bunlar ölçüm değildir. SSD'nin yüksek sıralı hız etiketi oyun FPS'sini belirlemez. Gen5 yerine güvenilir TLC NVMe ve makul sıcaklık daha anlamlıdır. Örnek olarak Samsung 990 EVO Plus 1 TB garanti sınırı 5 yıl veya 600 TBW'dir; bölgesel koşullar teyit edilmeli. [Samsung garanti bilgisi](https://semiconductor.samsung.com/consumer-storage/support/warranty/)

## 8. Windows mu Android mi?

Kendi çok oyunlu platformunu geliştirmek için ilk prototipte x86 Windows pratik seçimdir: PCAP USB girişini test etmek, profiler kullanmak ve servis bilgisayarıyla bakım yapmak kolaydır. Android de uygulanabilir; ancak ekranın dahili signage bilgisayarı otomatik olarak yeterli oyun donanımı sayılmaz. Güncelleme ömrü, grafik sürücüleri, giriş davranışı ve ticari lisans modeli değerlendirilir.

Üretimde otomatik açılan bir uygulama, oyun seçici, yönetici PIN'i, oturum kaydı ve çöken uygulamayı yeniden başlatan servis gerekir. Standart Windows Pro ile Enterprise/IoT kiosk yetenekleri aynı değildir. Microsoft Shell Launcher, Enterprise/Education/IoT Enterprise sürümlerinde desteklenir; Pro için doğrudan bu özelliği varsaymayın. [Microsoft](https://learn.microsoft.com/windows/configuration/shell-launcher/)

## 9. Çoklu dokunma yazılımı

40 nokta ekran, her oyunun 40 parmağı doğru yönettiği anlamına gelmez. Panel, USB denetleyici, işletim sistemi, oyun motorunun giriş katmanı ve uygulama birlikte doğrulanır. Her temasın kimliği korunmalı; oyuncu alanı, sürüklenen taş ve parmak eşlemesi uygulamada yönetilmeli. Donanım oyuncunun hangi kişi olduğunu kendiliğinden söylemez. [Microsoft HID gereksinimleri](https://learn.microsoft.com/en-us/windows-hardware/design/component-guidelines/supporting-usages-in-multitouch-digitizer-drivers-win8)

Kritik senaryolar: iki oyuncunun birbirine yakın noktaları tutması, aynı taşı iki kişinin seçmesi, elini kaldırınca yarım kalan hareket, ekran kenarı, Windows hareketleri ve avuç reddetmenin gerçek parmağı silmesi. Bunlar sıra tabanlı oyunda bile kullanıcı deneyimini bozar.

## 10. Masa kasası, boyut ve soğutma

16:9 oranında 43 inç aktif alan kabaca 95 × 54 cm, 55 inç 122 × 69 cm'dir; çerçeveyle dış ölçü büyür. Önerilen ilk maket ölçüleri 43 için yaklaşık 110–120 × 70–80 cm, 55 için 140–150 × 85–95 cm. Bunlar ergonomi denemesi için tasarım aralıklarıdır. Sandalye yüksekliği, diz boşluğu ve oyunların kenarlara taşınması gerçek kullanıcıyla belirlenmeli.

55 inç daha gösterişli ve geniştir; orta noktaya ulaşmak zorlaşabilir. Dört kişinin günlük Okey kullanımı için 43 inç iyi bir prototip adayı, altı oyunculu parti içerikleri için 55 ayrıca denenmeli. Gerekirse ortak alan merkezde, sık kullanılan kontroller oyuncuya yakın kenarlarda olur.

Cam yükünü ekran paneline bindirmeyen taşıyıcı tasarım gerekir. 7H/9H çizilme ifadesi masanın yük taşıma belgesi değildir. Güvenli cam ve taşıyıcı konstrüksiyon mekanik tasarımla seçilmeli. İlave cam touch hassasiyetini, yansımayı ve parallaxı değiştirebilir.

Panel ve PC için hava giriş/çıkışları ayrılmalı; değiştirilebilir filtre, düşük devirli fan ve servis kapağı kullanılmalı. Sıvı giriş yolları elektrik bölmesinden uzaklaştırılmalı. Ön yüz IP65, USB girişleri ve kasanın tamamının IP65 olduğu anlamına gelmez. Tüm ürün için üretim öncesi elektrik/EMC ve ilgili uygunluk değerlendirmesi planlanmalı. [EMC çerçevesi](https://single-market-economy.ec.europa.eu/sectors/electrical-and-electronic-engineering-industries-eei/electromagnetic-compatibility-emc-directive_en)

## 11. Prototip bütçesi

**Aşağıdaki rakamlar tedarikçi teklifi veya doğrulanmış güncel parça fiyatı değildir.** Türkiye'de profesyonel ekranların açık fiyatları sınırlı olduğu için yalnızca ilk bütçe rezervi oluşturur. KDV, teslimat, montaj ve garanti aynı kapsamda yazılı tekliflerle karşılaştırılmalı. PC kalemi işletim sistemi dahil varsayılmıştır; ayrı lisans gerekirse ilave edilir.

| Kalem | 43 inç planlama payı | 55 inç planlama payı |
|---|---:|---:|
| Profesyonel tabletop PCAP ekran | 55–100 bin TL | 90–170 bin TL |
| Entegre grafikli PC, RAM, SSD, OS | 25–45 bin TL | 25–45 bin TL |
| Özel kasa/taşıyıcı/servis kapakları | 20–35 bin TL | 25–45 bin TL |
| Soğutma, kablo, ses, elektrik, UPS | 10–18 bin TL | 12–22 bin TL |
| Montaj ve prototip testi | 8–15 bin TL | 10–18 bin TL |
| Ara toplam | 118–213 bin TL | 162–300 bin TL |
| %15 belirsizlik rezerviyle | yaklaşık 136–245 bin TL | yaklaşık 186–345 bin TL |

**Dahil olmayanlar:** oyun yazılımını geliştirme/satın alma, gizli el ekranları veya optik çözüm, oyun/marka lisansları, sunucu, ödeme sistemi, ürün sertifikasyon giderleri, seri üretim kalıpları ve satış sonrası servis organizasyonu. Bu yüzden bu tablo hazır Bambu ürününe doğrudan fiyat üstünlüğü göstermez. Kendi yazılım emeği de ürün maliyetidir.

Seri üretimde ekran ve PC teklifini 1/10/50 adet için ayrı isteyin. Birim maliyetle tek seferlik mühendislik giderini ayrı tutun. Tek masa satın alma ile kendi ürün platformunu kurma ekonomik olarak farklı kararlardır.

## 12. Yazılımın gerçek kapsamı

Bir launcher yapmakla çok oyunlu ticari masa tamamlanmaz. Ortak altyapı: koltuk/oyuncu yönetimi, çoklu giriş, dil ve ses, oturum kaydı, kesintiden dönüş, güncelleme ve yönetici arayüzü. Her oyun ayrıca kural motoru, görseller, animasyon, kullanıcı testi ve hata düzeltme gerektirir.

Önerilen ilk kapsam: bir açık tahta oyunu, bir gizli el oyunu, bir hızlı eşzamanlı arcade ve bir hafif 3D oyun. Bunlar donanımın dört farklı gereksinimini temsil eder. Testler geçince katalog genişletilir. Hazır mobil oyunların lisansı ve masa çevresindeki çok oyunculu arayüzü kendiliğinden uygun hale gelmez.

Monopoly markası ve görsellerini kullanmak ayrıca değerlendirilmelidir. Hasbro'nun kullanım başvuru sayfası, izin verilmiş sayılmak için yazılı yetkilendirme gerektiğini belirtiyor. Kendi markalı bir emlak oyunu geliştirmek tasarım alternatifidir; isim değiştirmek tek başına her fikrî hak sorununu çözdüğü anlamına gelmez. [Hasbro izin başvurusu](https://docs.hasbro.com/en-gb/legal/request-form)

## 13. Satın almadan önce kabul testleri

1. **Ekran ve cam:** Gerçek kasa ve son cam katmanıyla dört kenarda dokunma. Cam eklenmeden yapılan demo yeterli değil.
2. **Çoklu temas:** En az 20 temas, hedef panelde 40; kaybolan temas ve yanlış kimlik sayısı kayıt altına alınır.
3. **Hızlı arcade:** İki/dört oyuncu eşzamanlı hareket; hareketin sonuna kadar aynı temas kimliği.
4. **Performans:** En ağır oyunda 60 FPS tasarım hedefi; ortalama FPS yanında yüzde 99 kare süresi ve takılmalar ölçülür. 60 FPS için kare bütçesi 16,7 ms.
5. **Gecikme:** Yüksek hızlı kamerayla parmak hareketinden görünür tepkiye kadar ölçüm. İlk hedef <50 ms, değerlendirme üst sınırı 80 ms; bunlar tasarım hedefidir, katalog vaadi değil.
6. **Gizlilik:** Dört koltuk, yana eğilme, ayağa kalkma, ışık yansıması. Kullanılacak gizlilik çözümünün sınırları kaydedilir.
7. **Isı:** Son kasa içinde 8 saat, ardından 72 saat dayanıklılık çalışması; panel sınırları, PC sıcaklığı ve hız düşmesi izlenir.
8. **Kesintiler:** Elektrik kesilmesi, USB touch sökülmesi, ağ kopması, uygulama çökmesi ve yeniden açılma.
9. **Güncelleme:** Başarısız güncellemeden çalışan önceki sürüme dönüş.
10. **İşletme kullanımı:** Temizlik, ses seviyesi, diz boşluğu, içecek konumu ve servis parçasının değiştirilmesi.

Elektrik ve sıvı güvenliği testleri uygun uzmanlık ve düzenekle yapılmalı; rastgele çalışan masaya su dökmek kabul testi değildir.

## 14. Tedarikçiye gönderilecek teklif metni

“Dört/altı oyunculu dijital oyun masası prototipi için 43 ve 55 inç 4K PCAP ekran teklifinizi rica ederim. Ekran yüzeyi tavana bakacak şekilde yatay kullanılacaktır. Tam model kodu, face-up kullanım onayı, kapalı kasa sıcaklık sınırı, 20/40 eşzamanlı temas desteği, ilave cam koşulları, garanti kapsamı ve servis süresini belirtiniz. Dokunmatik USB protokolü, 4K 60 Hz girişler, teknik çizim ve yedek parça erişimini paylaşınız. 1/10/50 adet için KDV, teslimat, montaj ve garanti kapsamı açık fiyat sununuz. Gerçek kasa ve camla çoklu temas demosu talep ediyorum.”

Hazır masa üreticisine ayrıca: oyunların tam listesi ve sürümü; internet kesilince çalışan oyunlar; gizli el demosu; güncelleme ücreti; ticari kullanım lisansı; süre/kredi sistemi; ekran ve PC'nin tam modeli; servis SLA'sı; garanti dışı ekran değişim fiyatı sorulmalı.

## 15. Uygulama sırası ve karar kapıları

- **Aşama 1:** Bambu ve ikinci hazır ürün demosu; oyun kataloğu, gizlilik ve kullanıcı deneyimi tutanağı.
- **Aşama 2:** 43/55 maket, ekran numunesi ve dört temsili oyun prototipi.
- **Aşama 3:** Tek ekranlı kasa, termal ve giriş testleri; gizli el mimarisi seçimi.
- **Aşama 4:** Kontrollü işletme pilotu; arıza, oturum süresi ve kullanım oranı ölçümü.
- **Aşama 5:** Parça listesi ve kasa dondurma; seri üretim ve servis planı.

İlk donanım ve az oyunlu pilot için 8–12 haftalık çalışma penceresi yalnızca başlangıç planıdır; ekran tedariki, ekip ve gizlilik çözümüne bağlıdır. 75 oyunluk ticari yazılım kütüphanesi bu süreye dahil değildir.

**Satın alma önerisi:** Önce ekran ve gizlilik demosunu doğrulayın; ardından 8600G/32 GB/1 TB sınıfında tek bilgisayar alın. Harici GPU kararını hızlı arcade ve 3D prototip ölçümünden sonra verin. Bir adet kullanım için hazır ürünü, kendi markalı çok adet üretim için modüler geliştirmeyi aynı yazılım ve servis kapsamıyla karşılaştırın.
