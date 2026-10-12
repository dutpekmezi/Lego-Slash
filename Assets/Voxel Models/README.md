# VOX → LEGO

Unity'de **Tools → Trash Rush → VOX to LEGO** menüsünü aç.

1. `VOX dosyası` alanında `Pineapple.vox` varsayılan olarak seçilir. Başka bir `.vox` dosyasını da sürükleyebilirsin.
2. Kendi parça prefabını `LEGO parça prefabı` alanına sürükle. Prefabın kök objesi aktif olmalı ve bir Renderer içermeli.
3. `Parça aralığı` değerlerini parçanın merkezleri arasındaki mesafeye göre ayarla. X genişlik, Y katman yüksekliği, Z derinliktir. `Parça ölçek çarpanı` prefabın mevcut ölçeğini çarpar. Pivot merkezde değilse `Parçanın pivot düzeltmesi` alanını kullan.
4. `LEGO model prefabını oluştur` düğmesine basıp kayıt konumunu seç. Sonuç Project penceresinde seçilir; sahneye sürükle.

Her voxel ayrı bir prefab örneği olur. Kaynak renkler, modelle birlikte kaydedilen paylaşılan materyallerle uygulanır. Renderer'ın bütün materyal yuvaları renklendirilir. İstersen `Renk materyali` alanına düz beyaz bir URP/Lit materyali koy; aksi halde parçanın ilk Renderer materyali şablon alınır.

`Yalnızca dış yüzey` iç voxelleri atlayarak obje sayısını azaltır. Kapalıysa modelin içi de parçalarla doludur. Parçalanırken iç hücreler gerekiyorsa kapalı tut.

Parçalar otomatik olarak `VoxelBrick` bileşeni alır. Mevcut `Assets/Prefabs/lego.prefab` üzerinde de bu bileşen vardır. Trigger Collider'ları bu parçanın üzerinde veya altında tut. Oyun sırasında kesik şeridine denk gelen bütün parçalara Rigidbody eklenir, modelden ayrılırlar, yer çekimleri açılır ve rastgele X/Y sapmasıyla dünya −Z yönüne fırlatılırlar. Aynı parça tekrar fırlatılmaz. Kopan parçalar varsayılan olarak beş saniye sonra temizlenir. Kesik genişliği, fırlatma hızı, sapma, dönüş ve yaşam süresi `SlashConfig` üzerindeki `Voxel LEGO` bölümünden ayarlanır.

`Kinematic Rigidbody hazırla` simülasyonu kapalı Rigidbody'yi önceden ekler; zorunlu değildir. Rigidbody yoksa ilk vuruşta eklenir. Voxel modellerde son vuruş eski `CellFractured` modelini üretmez; kopan parçalar kendi yaşam süreleriyle kalırken tamamlanan model temizlenir.

Araç Goxel'in tek hacimli, RGBA paletli `.vox` çıktısını destekler. Çok modelli veya sahne dönüşümleri içeren VOX dosyalarını açık hata mesajıyla reddeder. Aynı isimle yeniden üretirken mevcut çıktıyı ezmek yerine yeni bir dosya oluşturur. Oluşturma sırasında iptal edersen o işlemdeki geçici model ve materyaller temizlenir.

