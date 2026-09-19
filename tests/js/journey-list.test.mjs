/*
 * Sefer listesi sıralamasının testleri.
 *
 * .NET test paketine dâhil değil: test edilen kod JavaScript ve yalnızca bir
 * JavaScript çalıştırıcısı asıl davranışı doğrulayabilir. Çalıştırma:
 *   node --test tests/js/
 *
 * Kapsanan asıl risk, eksik verinin en iyi sonuç gibi görünmesi: API bazı
 * seferler için süre bildirmiyor ve o kartlar "sıfır dakika" sayılırsa
 * süreye göre sıralamada en başa çıkar.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);
const journeyList = require('../../src/Obilet.Web/wwwroot/js/journey-list.js');

/**
 * Bir sefer kartının yerine geçen en küçük nesne.
 *
 * Gerçek bir DOM'a ihtiyaç yok: sıralama yalnızca `getAttribute` üzerinden
 * veri okuyor, dolayısıyla test o tek yüzeyi taklit ediyor.
 */
function card(attributes) {
    return {
        getAttribute(name) {
            return Object.prototype.hasOwnProperty.call(attributes, name)
                ? attributes[name]
                : null;
        },
    };
}

function journey({ departure, price, duration }) {
    const attributes = { 'data-departure': departure };

    if (price !== undefined) attributes['data-price'] = price;
    if (duration !== undefined) attributes['data-duration'] = duration;

    return card(attributes);
}

test('fiyata göre artan sıralar', () => {
    const cards = [
        journey({ departure: '2026-09-21T14:00:00', price: '700.00' }),
        journey({ departure: '2026-09-21T11:30:00', price: '500.00' }),
        journey({ departure: '2026-09-21T18:25:00', price: '345.00' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('price'));

    assert.deepEqual(
        sorted.map((c) => c.getAttribute('data-price')),
        ['345.00', '500.00', '700.00']);
});

test('fiyat ondalıklı olduğunda metin değil sayı olarak karşılaştırılır', () => {
    // Metin karşılaştırması "1000.00" değerini "345.00" öncesine koyardı.
    const cards = [
        journey({ departure: '2026-09-21T11:00:00', price: '1000.00' }),
        journey({ departure: '2026-09-21T12:00:00', price: '345.00' }),
        journey({ departure: '2026-09-21T13:00:00', price: '90.50' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('price'));

    assert.deepEqual(
        sorted.map((c) => c.getAttribute('data-price')),
        ['90.50', '345.00', '1000.00']);
});

test('süreye göre artan sıralar', () => {
    const cards = [
        journey({ departure: '2026-09-21T11:30:00', duration: '100' }),
        journey({ departure: '2026-09-21T14:00:00', duration: '90' }),
        journey({ departure: '2026-09-21T16:00:00', duration: '105' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('duration'));

    assert.deepEqual(
        sorted.map((c) => c.getAttribute('data-duration')),
        ['90', '100', '105']);
});

test('süresi bildirilmeyen sefer en başa değil sona gider', () => {
    const missing = journey({ departure: '2026-09-21T11:30:00', duration: '' });

    const cards = [
        missing,
        journey({ departure: '2026-09-21T14:00:00', duration: '90' }),
        journey({ departure: '2026-09-21T16:00:00', duration: '105' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('duration'));

    assert.equal(sorted[0].getAttribute('data-duration'), '90');
    assert.equal(sorted[2], missing);
});

test('sayıya çevrilemeyen değer eksik sayılır', () => {
    const broken = journey({ departure: '2026-09-21T11:30:00', price: 'bedava' });

    const cards = [
        broken,
        journey({ departure: '2026-09-21T14:00:00', price: '700.00' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('price'));

    assert.equal(sorted[1], broken);
    assert.equal(journeyList.readKey(broken, 'price'), null);
});

test('eşit fiyatta kalkış anına düşülür', () => {
    const cards = [
        journey({ departure: '2026-09-21T16:00:00', price: '500.00' }),
        journey({ departure: '2026-09-21T11:30:00', price: '500.00' }),
        journey({ departure: '2026-09-21T14:30:00', price: '500.00' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('price'));

    assert.deepEqual(
        sorted.map((c) => c.getAttribute('data-departure')),
        [
            '2026-09-21T11:30:00',
            '2026-09-21T14:30:00',
            '2026-09-21T16:00:00',
        ]);
});

test('kalkışa göre sıralamada ertesi güne taşan seferler sonda kalır', () => {
    // Kalkış anahtarı ISO dizesi olarak karşılaştırılıyor; yalnızca saate
    // bakan bir sıralama ertesi günün 00:30 seferini aynı günün 23:00
    // seferinin önüne koyardı.
    const cards = [
        journey({ departure: '2026-09-22T00:30:00' }),
        journey({ departure: '2026-09-21T23:00:00' }),
        journey({ departure: '2026-09-21T06:00:00' }),
    ];

    const sorted = cards.slice().sort(journeyList.compareBy('departure'));

    assert.deepEqual(
        sorted.map((c) => c.getAttribute('data-departure')),
        [
            '2026-09-21T06:00:00',
            '2026-09-21T23:00:00',
            '2026-09-22T00:30:00',
        ]);
});

test('varsayılan sıralama anahtarı kalkış', () => {
    // Sunucunun kurduğu ve gereksinim olan sıra bu; istemci başka bir
    // varsayılana kaymamalı.
    assert.equal(journeyList.DEFAULT_KEY, 'departure');
});
