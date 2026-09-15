/*
 * Arama formunun istemci tarafı davranışı.
 *
 * View içine gömülmek yerine ayrı bir dosyada duruyor; böylece tarayıcı
 * önbelleğine girebiliyor ve saf mantık kısmı Node ile test edilebiliyor
 * (bkz. tests/js/search-form.test.mjs).
 *
 * Hem tarayıcıda hem Node'da yüklenebilir: tarayıcıda window.ObiletSearchForm
 * olarak, Node'da module.exports olarak açılır.
 */
(function (root, factory) {
    const api = factory();

    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    } else {
        root.ObiletSearchForm = api;
    }
})(typeof self !== 'undefined' ? self : this, function () {
    'use strict';

    const STORAGE_KEY = 'obilet.lastSearch';
    const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

    // --- Türkçe'ye duyarlı katlama -------------------------------------------
    // Sunucudaki TurkishSearchText ile aynı kuralları uygular. İki yerde
    // bulunmasının sebebi: sunucu API sonucunu süzüyor, istemci ise sayfayla
    // birlikte gelen varsayılan listeyi süzüyor. Kurallar ayrışırsa kullanıcı
    // aynı terim için iki farklı davranış görür.
    const FOLD_MAP = {
        'İ': 'i', 'I': 'i', 'ı': 'i', 'i': 'i',
        'Ş': 's', 'ş': 's', 'Ğ': 'g', 'ğ': 'g',
        'Ç': 'c', 'ç': 'c', 'Ö': 'o', 'ö': 'o',
        'Ü': 'u', 'ü': 'u', 'Â': 'a', 'â': 'a',
        'Û': 'u', 'û': 'u', 'Ê': 'e', 'ê': 'e'
    };

    function foldTurkish(value) {
        if (!value) return '';

        let result = '';
        let previousWasSpace = false;

        for (const character of String(value)) {
            let folded = FOLD_MAP[character];

            if (folded === undefined) {
                folded = /[\p{L}\p{N}]/u.test(character)
                    ? character.toLowerCase()
                    : ' ';
            }

            if (folded === ' ') {
                if (result.length > 0 && !previousWasSpace) {
                    result += ' ';
                    previousWasSpace = true;
                }
                continue;
            }

            result += folded;
            previousWasSpace = false;
        }

        return result.replace(/\s+$/, '');
    }

    // --- Tarih çekme ---------------------------------------------------------
    /**
     * Kaydedilmiş bir kalkış tarihini bugüne göre değerlendirir.
     *
     * Son arama haftalar öncesine ait olabilir ve minimum geçerli tarih bugün
     * olduğu için körlemesine geri yüklemek kullanıcıyı açılışta zaten
     * geçersiz bir formla karşılaştırır. Bu yüzden eskimiş tarih ileriye
     * çekilir ve bunun yapıldığı kullanıcıya söylenir — değer sessizce
     * altından değiştirilmez.
     *
     * Karşılaştırma ISO dizeleri üzerinden yapılıyor; yerel saat diliminde
     * Date nesnesi kurmak gün kaymasına yol açabilir.
     *
     * @returns {{date: string|null, wasClamped: boolean}}
     *   date null ise sunucunun render ettiği varsayılan korunur.
     */
    function clampDepartureDate(savedDate, today) {
        if (!ISO_DATE.test(String(savedDate || '')) || !ISO_DATE.test(String(today || ''))) {
            return { date: null, wasClamped: false };
        }

        if (savedDate < today) {
            return { date: today, wasClamped: true };
        }

        return { date: savedDate, wasClamped: false };
    }

    // --- Son aramanın saklanması ---------------------------------------------
    /**
     * Kaydedilmiş son aramayı okur.
     *
     * Kalkış ve varış için kimliğin yanında ad da saklanır. Bu zorunlu:
     * kullanıcı metin aramasıyla varsayılan listede olmayan bir lokasyon
     * seçmiş olabilir ve geri yüklerken seçeneği yeniden oluşturmak için
     * ada ihtiyaç var.
     *
     * Tüm erişim try/catch içinde: gizli sekmede veya site verisi kapalıyken
     * depolama erişimi istisna fırlatabilir ve form o durumda da çalışmalı.
     */
    function readLastSearch(storage) {
        try {
            const raw = storage && storage.getItem(STORAGE_KEY);
            if (!raw) return null;

            const parsed = JSON.parse(raw);
            if (!parsed || typeof parsed !== 'object') return null;

            return {
                origin: normalizeLocation(parsed.origin),
                destination: normalizeLocation(parsed.destination),
                date: ISO_DATE.test(String(parsed.date || '')) ? parsed.date : null
            };
        } catch (error) {
            return null;
        }
    }

    function writeLastSearch(storage, value) {
        try {
            if (!storage) return false;

            storage.setItem(STORAGE_KEY, JSON.stringify(value));
            return true;
        } catch (error) {
            // Depolama kotası dolu veya erişim engelli. Kaydedememek bir
            // kolaylığın kaybı; aramanın kendisini engellememeli.
            return false;
        }
    }

    function normalizeLocation(candidate) {
        if (!candidate || typeof candidate !== 'object') return null;

        const id = String(candidate.id || '').trim();
        if (!id) return null;

        return { id: id, name: String(candidate.name || '').trim() };
    }

    /** Hangi hızlı seçim çipinin geçerli olduğunu söyler. */
    function activeDateChip(selectedDate, today, tomorrow) {
        if (selectedDate === today) return 'today';
        if (selectedDate === tomorrow) return 'tomorrow';
        return null;
    }

    return {
        STORAGE_KEY: STORAGE_KEY,
        foldTurkish: foldTurkish,
        clampDepartureDate: clampDepartureDate,
        readLastSearch: readLastSearch,
        writeLastSearch: writeLastSearch,
        activeDateChip: activeDateChip
    };
});
