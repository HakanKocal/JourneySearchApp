/*
 * Arama formunun saf istemci mantığı için testler.
 *
 * Çalıştırmak için: node --test tests/js/
 *
 * Bu testler .NET test paketine dâhil değil; ayrı çalıştırılırlar. Sebebi
 * basit: test edilen kod JavaScript ve localStorage üzerinde çalışıyor,
 * dolayısıyla asıl mantığı ancak bir JavaScript çalıştırıcısı doğrulayabilir.
 * Tarih çekme kuralı sessizce bozulabilecek türden olduğu için gerçek bir
 * testle korunması gerekiyordu.
 */
import test from 'node:test';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const helpers = require('../../src/Obilet.Web/wwwroot/js/search-form.js');

// --- Tarih çekme -------------------------------------------------------------

test('geçmişte kalan tarih bugüne çekilir', () => {
    const result = helpers.clampDepartureDate('2026-09-01', '2026-09-15');

    assert.deepEqual(result, { date: '2026-09-15', wasClamped: true });
});

test('bugün olan tarih çekilmez', () => {
    // Bugün geçerli bir tarihtir; sınır bugünün kendisidir.
    const result = helpers.clampDepartureDate('2026-09-15', '2026-09-15');

    assert.deepEqual(result, { date: '2026-09-15', wasClamped: false });
});

test('gelecekteki tarih olduğu gibi korunur', () => {
    const result = helpers.clampDepartureDate('2026-12-31', '2026-09-15');

    assert.deepEqual(result, { date: '2026-12-31', wasClamped: false });
});

test('ay ve yıl sınırları doğru karşılaştırılır', () => {
    // ISO dize karşılaştırması ay/yıl geçişlerinde de doğru sıralar.
    assert.equal(helpers.clampDepartureDate('2025-12-31', '2026-01-01').wasClamped, true);
    assert.equal(helpers.clampDepartureDate('2026-01-02', '2026-01-01').wasClamped, false);
});

test('geçersiz biçimli tarih yok sayılır', () => {
    // date null dönünce sunucunun render ettiği varsayılan korunur.
    for (const invalid of ['15.09.2026', '2026-9-1', 'yarin', '', null, undefined, '2026-09-15T00:00:00']) {
        const result = helpers.clampDepartureDate(invalid, '2026-09-15');

        assert.equal(result.date, null, `beklenmeyen: ${invalid}`);
        assert.equal(result.wasClamped, false);
    }
});

// --- Hızlı seçim çipleri -----------------------------------------------------

test('geçerli tarihe karşılık gelen çip işaretlenir', () => {
    assert.equal(helpers.activeDateChip('2026-09-15', '2026-09-15', '2026-09-16'), 'today');
    assert.equal(helpers.activeDateChip('2026-09-16', '2026-09-15', '2026-09-16'), 'tomorrow');
});

test('ne bugün ne yarın olan tarihte hiçbir çip işaretlenmez', () => {
    assert.equal(helpers.activeDateChip('2026-10-01', '2026-09-15', '2026-09-16'), null);
});

// --- Son aramanın saklanması -------------------------------------------------

/** localStorage'ın bellek içi karşılığı. */
function fakeStorage(initial) {
    const data = new Map(Object.entries(initial || {}));

    return {
        getItem: (key) => (data.has(key) ? data.get(key) : null),
        setItem: (key, value) => data.set(key, value),
        data
    };
}

/** Her işlemde hata veren depolama; gizli sekme davranışını taklit eder. */
function throwingStorage() {
    return {
        getItem: () => { throw new Error('erişim engelli'); },
        setItem: () => { throw new Error('erişim engelli'); }
    };
}

test('kaydedilmiş arama okunur', () => {
    const storage = fakeStorage({
        [helpers.STORAGE_KEY]: JSON.stringify({
            origin: { id: 349, name: 'İstanbul Avrupa' },
            destination: { id: 356, name: 'Ankara' },
            date: '2026-09-20'
        })
    });

    const saved = helpers.readLastSearch(storage);

    assert.deepEqual(saved.origin, { id: '349', name: 'İstanbul Avrupa' });
    assert.deepEqual(saved.destination, { id: '356', name: 'Ankara' });
    assert.equal(saved.date, '2026-09-20');
});

test('lokasyon adı da saklanır', () => {
    // Ad zorunlu: kullanıcı metin aramasıyla varsayılan listede olmayan bir
    // lokasyon seçmiş olabilir ve geri yüklerken seçeneği yeniden oluşturmak
    // için ada ihtiyaç var. Yalnızca kimlik saklamak boş bir seçim üretirdi.
    const storage = fakeStorage();

    helpers.writeLastSearch(storage, {
        origin: { id: '1835', name: 'Kayaş' },
        destination: { id: '356', name: 'Ankara' },
        date: '2026-09-20'
    });

    const saved = helpers.readLastSearch(storage);

    assert.equal(saved.origin.name, 'Kayaş');
});

test('bozuk kayıt sessizce yok sayılır', () => {
    for (const raw of ['{bozuk', 'null', '"metin"', '[]', '{}']) {
        const storage = fakeStorage({ [helpers.STORAGE_KEY]: raw });
        const saved = helpers.readLastSearch(storage);

        // Bozuk kayıt formu düşürmemeli; ya null ya da alanları boş bir nesne.
        if (saved !== null) {
            assert.equal(saved.origin, null);
            assert.equal(saved.destination, null);
        }
    }
});

test('kayıt yoksa null döner', () => {
    assert.equal(helpers.readLastSearch(fakeStorage()), null);
});

test('depolama erişimi engelliyse okuma ve yazma çökmez', () => {
    // Gizli sekmede veya site verisi kapalıyken erişim istisna fırlatabilir
    // ve form o durumda da çalışmaya devam etmeli.
    assert.equal(helpers.readLastSearch(throwingStorage()), null);
    assert.equal(helpers.writeLastSearch(throwingStorage(), { date: '2026-09-20' }), false);
});

test('depolama yoksa okuma ve yazma çökmez', () => {
    assert.equal(helpers.readLastSearch(null), null);
    assert.equal(helpers.writeLastSearch(null, {}), false);
});

test('geçersiz kaydedilmiş tarih yok sayılır', () => {
    const storage = fakeStorage({
        [helpers.STORAGE_KEY]: JSON.stringify({ date: '15.09.2026' })
    });

    assert.equal(helpers.readLastSearch(storage).date, null);
});

// --- Türkçe katlama (sunucu kurallarıyla aynı olmalı) ------------------------

test('i harfinin tüm varyantları aynı değere katlanır', () => {
    const expected = 'izmir';

    for (const input of ['İzmir', 'Izmir', 'ızmir', 'IZMIR', 'İZMİR']) {
        assert.equal(helpers.foldTurkish(input), expected, `beklenmeyen: ${input}`);
    }
});

test('Türkçe harfler ASCII karşılıklarına indirgenir', () => {
    assert.equal(helpers.foldTurkish('Şanlıurfa'), 'sanliurfa');
    assert.equal(helpers.foldTurkish('Kütahya'), 'kutahya');
    assert.equal(helpers.foldTurkish('Çorum'), 'corum');
});

test('noktalama ve fazla boşluk ayırıcı sayılır', () => {
    assert.equal(helpers.foldTurkish('Ankara (Aşti) Otogarı'), 'ankara asti otogari');
    assert.equal(helpers.foldTurkish('İstanbul   Avrupa'), 'istanbul avrupa');
});

test('boş girdi boş dize döner', () => {
    for (const input of [null, undefined, '', '   ']) {
        assert.equal(helpers.foldTurkish(input), '');
    }
});
