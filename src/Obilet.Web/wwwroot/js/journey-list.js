/*
 * Sefer listesinin istemci tarafı sıralaması.
 *
 * Sıralama neden istemcide: liste zaten tamamen sayfada ve yeniden
 * sorgulamak obilet API'sine gereksiz bir istek olurdu — o API hız sınırı
 * uyguluyor (ölçüldü: HTTP 429, Retry-After 3556), dolayısıyla kaçınılabilir
 * her istek kaçınılmalı.
 *
 * Varsayılan sıra sunucuda kuruluyor ve kalkış anına göre artan; bu bir
 * gereksinim. Bu dosya yalnızca gösterimi değiştiriyor, yani JavaScript
 * kapalıyken liste yine doğru sırada geliyor ve sıralama kutusu tek yaptığı
 * şeyi yapmadan kalıyor.
 *
 * Hem tarayıcıda hem Node'da yüklenebilir: tarayıcıda window.ObiletJourneyList
 * olarak, Node'da module.exports olarak açılır (bkz. tests/js/journey-list.test.mjs).
 */
(function (root, factory) {
    const api = factory();

    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    } else {
        root.ObiletJourneyList = api;
    }
})(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    /** Sunucunun kurduğu ve gereksinim olan varsayılan sıra. */
    const DEFAULT_KEY = 'departure';

    /**
     * Bir kartın sıralama anahtarını okur.
     *
     * Değerler view tarafından kültürden bağımsız biçimde yazılıyor: Türkçe
     * kültürde ondalık ayırıcı virgül olurdu ve parseFloat 500,50 değerini
     * 500 olarak okuyup kuruşu sessizce düşürürdü.
     *
     * @returns Sayısal anahtarlarda sayı, kalkışta ISO dizesi; okunamayan
     * değerde null.
     */
    function readKey(element, key) {
        if (key === 'departure') {
            // ISO 8601 dizeleri sözlük sırasında da kronolojik sıradadır,
            // bu yüzden Date nesnesi kurmaya gerek yok.
            return element.getAttribute('data-departure') || null;
        }

        const raw = element.getAttribute('data-' + key);
        if (raw === null || raw === '') {
            return null;
        }

        const value = Number(raw);
        return Number.isFinite(value) ? value : null;
    }

    /**
     * İki kartı verilen anahtara göre karşılaştırır.
     *
     * Değeri okunamayan kartlar listenin sonuna gider. Sebebi somut: API
     * bazı seferler için süre bildirmiyor ve bu kartlar süreye göre
     * sıralandığında "sıfır dakika" sayılıp en başa çıkardı — yani eksik
     * veri en iyi sonuç gibi görünürdü.
     *
     * Eşitlikte kalkış anına düşülür, böylece sıra her tarayıcıda ve her
     * yeniden sıralamada aynı kalır.
     */
    function compareBy(key) {
        return function (left, right) {
            const a = readKey(left, key);
            const b = readKey(right, key);

            if (a === null && b === null) return compareDeparture(left, right);
            if (a === null) return 1;
            if (b === null) return -1;

            if (a < b) return -1;
            if (a > b) return 1;

            return compareDeparture(left, right);
        };
    }

    function compareDeparture(left, right) {
        const a = readKey(left, 'departure');
        const b = readKey(right, 'departure');

        if (a === null || b === null) return 0;
        if (a < b) return -1;
        if (a > b) return 1;
        return 0;
    }

    /**
     * Sıralama kutusunu listeye bağlar.
     *
     * Kartların ilk hâli saklanıyor: kalkışa geri dönüldüğünde sunucunun
     * kurduğu sıra birebir geri gelir. Yeniden sıralamayla yaklaşmak yerine
     * saklamanın sebebi, sunucu sıralamasının eşitlikte sefer kimliğine
     * düşmesi ve o kimliğin işaretlemede taşınmaması.
     */
    function attachSorting(list, select) {
        const cards = Array.prototype.slice.call(
            list.querySelectorAll('[data-journey]'));

        // Ertesi güne taşan seferleri ayıran başlıklar yalnızca kronolojik
        // sırada anlam taşıyor; başka bir sırada gizleniyorlar.
        const dividers = Array.prototype.slice.call(
            list.querySelectorAll('[data-next-day-divider]'));

        const originalOrder = cards.slice();

        function apply(key) {
            const isDefault = key === DEFAULT_KEY;

            for (const divider of dividers) {
                divider.hidden = !isDefault;
            }

            if (isDefault) {
                // Ayırıcılar kartlarla birlikte yeniden diziliyor: yerleri
                // sunucu tarafında belirlendi ve yalnızca kartları taşımak
                // ayırıcıyı yanlış kartın önünde bırakırdı.
                rebuildDefault(list, originalOrder, dividers);
                return;
            }

            // Tek bir fragment'a toplanıp bir kez ekleniyor: 400 kartlık bir
            // listede tek tek appendChild çağırmak her seferinde yerleşimi
            // yeniden hesaplatıyor.
            const fragment = document.createDocumentFragment();
            for (const card of cards.slice().sort(compareBy(key))) {
                fragment.appendChild(card);
            }

            list.appendChild(fragment);
        }

        select.addEventListener('change', function () {
            apply(select.value);
        });

        // Tarayıcı bir seçimi geri yüklemiş olabilir (geri tuşu, yenileme).
        if (select.value !== DEFAULT_KEY) {
            apply(select.value);
        }
    }

    /**
     * Varsayılan sırayı ayırıcılarla birlikte kurar.
     *
     * Ayırıcının yeri sabit değil: hangi kartın önünde durduğu sunucu
     * tarafında belirlendi. Bu yüzden ilk hâlde ayırıcıdan sonra gelen kart
     * kaydediliyor ve geri dönüşte ayırıcı yine o kartın önüne konuyor.
     */
    function rebuildDefault(list, originalOrder, dividers) {
        const fragment = document.createDocumentFragment();

        for (const card of originalOrder) {
            for (const divider of dividers) {
                if (divider.__obiletAnchor === card) {
                    fragment.appendChild(divider);
                }
            }
            fragment.appendChild(card);
        }

        list.appendChild(fragment);
    }

    /**
     * Ayırıcıların ilk hâlde hangi kartın önünde durduğunu kaydeder.
     *
     * attachSorting içinden bir kez çağrılır; DOM karıştırılmadan önce
     * çalışması gerekiyor.
     */
    function anchorDividers(list) {
        const dividers = list.querySelectorAll('[data-next-day-divider]');

        for (const divider of dividers) {
            let node = divider.nextElementSibling;
            while (node && !node.hasAttribute('data-journey')) {
                node = node.nextElementSibling;
            }
            divider.__obiletAnchor = node;
        }
    }

    return {
        DEFAULT_KEY: DEFAULT_KEY,
        readKey: readKey,
        compareBy: compareBy,
        anchorDividers: anchorDividers,
        attachSorting: function (list, select) {
            anchorDividers(list);
            attachSorting(list, select);
        }
    };
});
