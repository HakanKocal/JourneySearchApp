/*
 * Arama alanlarının DOM bağlantısı: aranabilir lokasyon alanları, tarih
 * çipleri, takas butonu, istemci tarafı doğrulama ve son aramanın
 * saklanması.
 *
 * Neden ayrı bir dosya: aynı alanlar iki sayfada var. Ana sayfanın hero
 * kartında ve sefer listesi sayfasının sorgu özeti kartında. Bu kod bir
 * zamanlar ana sayfanın view'ı içinde gömülüydü; sefer sayfasına da arama
 * eklenirken iki yüz satırı kopyalamak yerine buraya taşındı.
 *
 * `search-form.js` ile ilişkisi: o dosya saf mantığı taşıyor (Türkçe
 * katlama, tarih çekme, depolama okuma/yazma) ve Node ile test ediliyor.
 * Bu dosya ise DOM'a dokunan katman; tarayıcıda doğrulanıyor.
 *
 * İki sayfanın tek farkı başlangıç değerlerinin nereden geldiği:
 * - Ana sayfa `restore: true` ile son aramayı depodan geri yükler.
 * - Sefer sayfasının başlangıç değeri adresin kendisidir, dolayısıyla
 *   `restore: false`; depodan okumak adresle çelişen bir form üretirdi.
 * İkisi de gönderim anında depoya yazar.
 */
(function (root, factory) {
    const api = factory();

    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    } else {
        root.ObiletSearchFields = api;
    }
})(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    /**
     * Depoya güvenli erişim.
     *
     * Gizli sekmede veya site verisi kapalıyken `window.localStorage`
     * erişimin kendisi istisna fırlatabiliyor, bu yüzden okuma denemesi
     * try içinde. Depo yoksa form yine çalışır.
     */
    function openStorage() {
        try {
            return window.localStorage;
        } catch (error) {
            return null;
        }
    }

    /**
     * Aranabilir bir lokasyon alanı kurar.
     */
    function createLocationSelect(element, config, helpers) {
        // Sunucunun geçerli terim için döndürdüğü kimlikler. Ayrı
        // tutulması zorunlu: sunucu eşleşmelerinin bir kısmı API'nin
        // anahtar kelime alanından geliyor ve istemci o alanı görmüyor.
        // Örneğin "esenler" araması "İstanbul Avrupa" döndürüyor;
        // yalnızca ada bakan bir süzgeç bu sonucu elerdi.
        //
        // Küme, geldiği terimle birlikte tutuluyor. Bunu bir kod incelemesi
        // ortaya çıkardı: yalnızca kimlikleri saklamak, önceki aramanın
        // sonuçlarının sonraki terimde de eşleşme sayılmasına yol açıyordu.
        let remoteMatches = { query: null, ids: new Set() };

        return new TomSelect(element, {
            valueField: 'id',
            labelField: 'name',
            searchField: ['name'],
            maxOptions: 20,
            create: false,
            placeholder: config.messages.placeholder,
            // Tom Select'in yerleşik gecikmesi; her tuşta istek atmaz.
            loadThrottle: 300,

            // Kullanıcı yazmaya başladığında seçili değer gizleniyor.
            //
            // Sebebi: Tom Select tek seçimli alanlarda seçili öğeyi
            // kontrolde bırakıyor ve yazılan metni onun yanına ekliyor,
            // dolayısıyla "Ankara" ile "rize" yan yana görünüyordu.
            // Gizleme CSS'te, `has-typed` sınıfına bağlı.
            //
            // Neden odaklanmada değil de yazmaya başlayınca: alana dokunan
            // kullanıcı henüz bir şey yazmadıysa mevcut seçimini görmeye
            // devam etmeli. Yer tutucuya yazmak denendi ve işe yaramadı —
            // Tom Select odaklanmada kendi `updatePlaceholder` çağrısıyla
            // üzerine yazıyor.
            //
            // Değer gizlenirken silinmiyor: silmek daha basit olurdu ama
            // seçim yapmadan alandan çıkan kullanıcıyı boş bir alanla
            // bırakırdı.
            onType: function (text) {
                this.wrapper.classList.toggle(
                    'has-typed', String(text || '').length > 0);
            },

            onBlur: function () {
                this.wrapper.classList.remove('has-typed');
            },

            onChange: function () {
                this.wrapper.classList.remove('has-typed');
            },

            score: function (search) {
                const needle = helpers.foldTurkish(search);

                // Sunucu sonuçları yalnızca geldikleri terim için geçerli;
                // terim değiştiyse yok sayılırlar.
                const trusted = remoteMatches.query === needle
                    ? remoteMatches.ids
                    : null;

                return function (item) {
                    if (!needle) return 1;
                    if (trusted !== null && trusted.has(String(item.id))) return 1;

                    return helpers.foldTurkish(item.name).indexOf(needle) !== -1 ? 1 : 0;
                };
            },

            shouldLoad: function (query) {
                return query.trim().length >= config.minQueryLength;
            },

            load: function (query, callback) {
                fetch(config.searchUrl + '?q=' + encodeURIComponent(query), {
                    headers: { 'Accept': 'application/json' }
                })
                    .then(function (response) {
                        return response.ok ? response.json() : [];
                    })
                    .then(function (results) {
                        remoteMatches = {
                            query: helpers.foldTurkish(query),
                            ids: new Set(results.map(function (item) {
                                return String(item.id);
                            }))
                        };
                        callback(results);
                    })
                    // Ağ hatası açılır listeyi kilitlememeli.
                    .catch(function () {
                        remoteMatches = { query: null, ids: new Set() };
                        callback();
                    });
            },

            render: {
                no_results: function (data) {
                    const text = data.input.trim().length < config.minQueryLength
                        ? config.messages.typeMore
                        : config.messages.noResults;
                    const div = document.createElement('div');
                    div.className = 'no-results px-2 py-1 text-muted small';
                    div.textContent = text;
                    return div;
                }
            }
        });
    }

    /**
     * Verilen yapılandırmaya göre arama alanlarını hazırlar.
     *
     * Gerekli öğeler bulunamazsa sessizce çıkar: sayfada arama formu
     * olmayabilir (örneğin lokasyon listesi boş geldiğinde view uyarı
     * gösteriyor ve formu hiç çizmiyor).
     */
    function init(config) {
        const helpers = window.ObiletSearchForm;
        const form = document.getElementById(config.formId);
        const origin = document.getElementById(config.originId);
        const destination = document.getElementById(config.destinationId);
        const date = document.getElementById(config.dateId);

        if (!helpers || !form || !origin || !destination || !date) {
            return null;
        }

        const summary = config.summaryId
            ? document.getElementById(config.summaryId)
            : null;
        const notice = config.noticeId
            ? document.getElementById(config.noticeId)
            : null;
        const swap = config.swapId ? document.getElementById(config.swapId) : null;

        const chips = {};
        for (const [name, id] of Object.entries(config.chipIds || {})) {
            const element = document.getElementById(id);
            if (element) chips[name] = element;
        }

        const selects = {
            origin: createLocationSelect(origin, config, helpers),
            destination: createLocationSelect(destination, config, helpers)
        };

        const storage = openStorage();

        // --- Tarih çipleri ----------------------------------------------
        function refreshChips() {
            const active = helpers.activeDateChip(
                date.value, config.today, config.tomorrow);

            for (const [name, button] of Object.entries(chips)) {
                const isActive = name === active;
                button.classList.toggle('is-active', isActive);
                button.setAttribute('aria-pressed', String(isActive));
            }
        }

        for (const button of Object.values(chips)) {
            button.addEventListener('click', function () {
                date.value = button.dataset.date;
                refreshChips();
                showErrors(collectErrors());
            });
        }

        date.addEventListener('change', refreshChips);

        // --- Takas -------------------------------------------------------
        if (swap) {
            swap.addEventListener('click', function () {
                const originItem = selects.origin.options[selects.origin.getValue()];
                const destinationItem =
                    selects.destination.options[selects.destination.getValue()];

                if (!originItem || !destinationItem) return;

                // Seçenek karşı alanda mevcut olmayabilir (metin aramasıyla
                // seçilmiş olabilir), bu yüzden önce eklenir.
                selects.origin.addOption(destinationItem);
                selects.destination.addOption(originItem);

                selects.origin.setValue(destinationItem.id, true);
                selects.destination.setValue(originItem.id, true);

                showErrors(collectErrors());
            });
        }

        // --- Son aramanın geri yüklenmesi --------------------------------
        function restoreLastSearch() {
            const saved = helpers.readLastSearch(storage);
            if (!saved) return;

            for (const [field, select] of Object.entries(selects)) {
                const location = saved[field];
                if (!location) continue;

                // Ad da saklanıyor, çünkü kullanıcı varsayılan listede
                // olmayan bir lokasyon seçmiş olabilir ve seçeneğin
                // yeniden oluşturulması gerekir.
                select.addOption({ id: location.id, name: location.name || location.id });
                select.setValue(location.id, true);
            }

            const clamped = helpers.clampDepartureDate(saved.date, config.today);
            if (clamped.date) {
                date.value = clamped.date;
            }
            if (clamped.wasClamped && notice) {
                notice.classList.remove('d-none');
            }
        }

        function persistLastSearch() {
            const originItem = selects.origin.options[selects.origin.getValue()];
            const destinationItem =
                selects.destination.options[selects.destination.getValue()];

            helpers.writeLastSearch(storage, {
                origin: originItem
                    ? { id: String(originItem.id), name: originItem.name }
                    : null,
                destination: destinationItem
                    ? { id: String(destinationItem.id), name: destinationItem.name }
                    : null,
                date: date.value
            });
        }

        // --- İstemci tarafı ayna doğrulama -------------------------------
        // Anında geri bildirim sağlar, ama bir koruma değildir. Aynı iki
        // kural sunucuda da uygulanıyor, çünkü sefer sayfası doğrudan
        // adresle açılabiliyor ve obilet API'si kuralların hiçbirini
        // güvenilir biçimde uygulamıyor.
        function collectErrors() {
            const errors = [];

            if (origin.value === destination.value) {
                errors.push(config.messages.sameLocation);
            }
            // Karşılaştırma ISO dizeleri üzerinden; yerel saat diliminde
            // Date nesnesi kurmak gün kaymasına yol açabilir.
            if (date.value && date.value < date.min) {
                errors.push(config.messages.pastDate);
            }

            return errors;
        }

        function showErrors(errors) {
            if (!summary) return;

            summary.innerHTML = '';

            if (errors.length === 0) {
                summary.classList.add('d-none');
                return;
            }

            const list = document.createElement('ul');
            list.className = 'mb-0';
            for (const message of errors) {
                const item = document.createElement('li');
                item.textContent = message;
                list.appendChild(item);
            }
            summary.appendChild(list);
            summary.classList.remove('d-none');
        }

        form.addEventListener('submit', function (event) {
            const errors = collectErrors();
            if (errors.length > 0) {
                event.preventDefault();
                showErrors(errors);
                return;
            }

            persistLastSearch();
        });

        for (const field of [origin, destination, date]) {
            field.addEventListener('change', function () {
                showErrors(collectErrors());
            });
        }

        if (config.restore) {
            restoreLastSearch();
        }

        refreshChips();

        return selects;
    }

    return { init: init };
});
