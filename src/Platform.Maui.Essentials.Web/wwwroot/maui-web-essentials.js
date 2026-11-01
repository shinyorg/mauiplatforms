// Browser implementations for Microsoft.Maui.Essentials.Web.
// Every export is bound by Interop/EssentialsJs.cs through [JSImport]; keep the two in sync.
// Synchronous browser reads (navigator/screen properties, localStorage) are bound directly
// with globalThis imports instead, so they work before this module finishes loading.

// ---- byte transfer ----------------------------------------------------------
// JS cannot hand a Uint8Array to .NET as a return value, so binary results are parked
// here by id and copied into a .NET-allocated buffer through a MemoryView.

const buffers = new Map();
let nextBufferId = 1;

function park(arrayBuffer) {
    const id = nextBufferId++;
    buffers.set(id, new Uint8Array(arrayBuffer));
    return id;
}

export function bufferLength(id) { return buffers.get(id)?.length ?? 0; }

export function copyBuffer(id, view) {
    const data = buffers.get(id);
    buffers.delete(id);
    if (data) view.set(data);
}

export function releaseBuffer(id) { buffers.delete(id); }

// ---- file & media picking ----------------------------------------------------

const batches = new Map();
let nextBatchId = 1;

// Resolves to a batch id, or 0 when the user cancels. Must run inside a user gesture.
export function pickFiles(accept, multiple, capture) {
    return new Promise(resolve => {
        const input = document.createElement('input');
        input.type = 'file';
        if (accept) input.accept = accept;
        input.multiple = !!multiple;
        if (capture) input.setAttribute('capture', capture);
        input.style.display = 'none';
        let settled = false;
        const done = files => {
            if (settled) return;
            settled = true;
            input.remove();
            if (!files || files.length === 0) { resolve(0); return; }
            const id = nextBatchId++;
            batches.set(id, [...files]);
            resolve(id);
        };
        input.addEventListener('change', () => done(input.files));
        input.addEventListener('cancel', () => done(null));
        document.body.appendChild(input);
        input.click();
    });
}

export function batchCount(batch) { return batches.get(batch)?.length ?? 0; }
export function batchFileName(batch, index) { return batches.get(batch)?.[index]?.name ?? ''; }
export function batchFileType(batch, index) { return batches.get(batch)?.[index]?.type ?? ''; }

export async function batchFileRead(batch, index) {
    const file = batches.get(batch)?.[index];
    return file ? park(await file.arrayBuffer()) : 0;
}

export function releaseBatch(batch) { batches.delete(batch); }

// ---- launching & sharing -----------------------------------------------------

// With noopener the browser returns null even on success, so there is nothing to report.
export function openWindow(url) { window.open(url, '_blank', 'noopener,noreferrer'); }

export function navigate(url) { window.location.href = url; }

export function download(bytes, fileName, mime) {
    const url = URL.createObjectURL(new Blob([bytes.slice()], { type: mime || 'application/octet-stream' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'download';
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(url), 30000);
}

const shareFiles = [];

export function addShareFile(bytes, fileName, mime) {
    shareFiles.push(new File([bytes.slice()], fileName, { type: mime || 'application/octet-stream' }));
}

export function clearShareFiles() { shareFiles.length = 0; }

export function canShare(withFiles) {
    if (!navigator.share) return false;
    if (!withFiles) return true;
    return !!navigator.canShare?.({ files: shareFiles });
}

// Resolves true when shared, false when the user dismissed the sheet.
export async function share(title, text, url, withFiles) {
    const data = {};
    if (title) data.title = title;
    if (text) data.text = text;
    if (url) data.url = url;
    if (withFiles) data.files = [...shareFiles];
    shareFiles.length = 0;
    try {
        await navigator.share(data);
        return true;
    } catch (e) {
        if (e?.name === 'AbortError') return false;
        throw e;
    }
}

// ---- device -----------------------------------------------------------------

let wakeLock = null;
let wantWakeLock = false;

async function acquireWakeLock() {
    try { wakeLock = await navigator.wakeLock.request('screen'); }
    catch { wakeLock = null; }
}

document.addEventListener('visibilitychange', () => {
    // The browser drops wake locks when the tab is hidden; take it back when visible.
    if (wantWakeLock && document.visibilityState === 'visible' && !wakeLock) acquireWakeLock();
});

export function setKeepScreenOn(on) {
    wantWakeLock = on;
    if (!('wakeLock' in navigator)) return false;
    if (on) { if (!wakeLock) acquireWakeLock(); }
    else { wakeLock?.release(); wakeLock = null; }
    return true;
}

export function observeDisplay(callback) {
    const notify = () => callback();
    window.addEventListener('resize', notify);
    screen.orientation?.addEventListener('change', notify);
    let mq = matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`);
    const onDpr = () => { notify(); mq.removeEventListener('change', onDpr); mq = matchMedia(`(resolution: ${window.devicePixelRatio}dppx)`); mq.addEventListener('change', onDpr); };
    mq.addEventListener('change', onDpr);
}

let battery = null;

// Resolves false when the Battery Status API is unavailable.
export async function startBattery(callback) {
    if (!navigator.getBattery) return false;
    try {
        battery = await navigator.getBattery();
    } catch {
        return false;
    }
    const notify = () => callback(battery.level, battery.charging, battery.chargingTime === Infinity ? -1 : battery.chargingTime);
    battery.addEventListener('levelchange', notify);
    battery.addEventListener('chargingchange', notify);
    notify();
    return true;
}

export function observeConnectivity(callback) {
    const notify = () => callback();
    window.addEventListener('online', notify);
    window.addEventListener('offline', notify);
    navigator.connection?.addEventListener?.('change', notify);
}

export function connectionType() { return navigator.connection?.type ?? ''; }

export function vibrate(pattern) { return navigator.vibrate ? navigator.vibrate(pattern) : false; }

// ---- clipboard ----------------------------------------------------------------

export function observeClipboard(callback) {
    document.addEventListener('copy', () => callback());
    document.addEventListener('cut', () => callback());
}

// ---- sensors -------------------------------------------------------------------

const sensors = new Map();
let nextSensorId = 1;
const G = 9.80665;
const DEG = Math.PI / 180;

// iOS requires a permission prompt, which only works inside a user gesture.
async function requestMotionPermission(type) {
    const ctor = type === 'orientation' ? window.DeviceOrientationEvent : window.DeviceMotionEvent;
    if (typeof ctor?.requestPermission === 'function') {
        try { return (await ctor.requestPermission()) === 'granted'; } catch { return false; }
    }
    return true;
}

export function sensorSupported(kind) {
    switch (kind) {
        case 'accelerometer':
        case 'gyroscope':
            return 'DeviceMotionEvent' in window;
        case 'orientation':
        case 'compass':
            return 'DeviceOrientationEvent' in window;
        case 'magnetometer':
            return typeof window.Magnetometer === 'function';
        default:
            return false;
    }
}

function throttle(intervalMs, fn) {
    let last = 0;
    return (...args) => {
        const now = performance.now();
        if (now - last < intervalMs) return;
        last = now;
        fn(...args);
    };
}

// The callback receives an object { x, y, z, w } in MAUI units.
export async function startSensor(kind, intervalMs, callback) {
    const id = nextSensorId++;
    const emit = throttle(intervalMs, callback);
    let stop;

    if (kind === 'accelerometer' || kind === 'gyroscope') {
        if (!(await requestMotionPermission('motion'))) return 0;
        const handler = e => {
            if (kind === 'accelerometer') {
                const a = e.accelerationIncludingGravity;
                if (!a || a.x === null) return;
                emit({ x: a.x / G, y: a.y / G, z: a.z / G, w: 0 });
            } else {
                const r = e.rotationRate;
                if (!r || r.alpha === null) return;
                emit({ x: (r.beta ?? 0) * DEG, y: (r.gamma ?? 0) * DEG, z: (r.alpha ?? 0) * DEG, w: 0 });
            }
        };
        window.addEventListener('devicemotion', handler);
        stop = () => window.removeEventListener('devicemotion', handler);
    } else if (kind === 'orientation' || kind === 'compass') {
        if (!(await requestMotionPermission('orientation'))) return 0;
        const eventName = kind === 'compass' && 'ondeviceorientationabsolute' in window ? 'deviceorientationabsolute' : 'deviceorientation';
        const handler = e => {
            if (kind === 'compass') {
                const heading = e.webkitCompassHeading ?? (e.absolute && e.alpha !== null ? (360 - e.alpha) % 360 : null);
                if (heading === null || heading === undefined) return;
                emit({ x: heading, y: 0, z: 0, w: 0 });
            } else {
                if (e.alpha === null) return;
                emit(eulerToQuaternion(e.alpha * DEG, e.beta * DEG, e.gamma * DEG));
            }
        };
        window.addEventListener(eventName, handler);
        stop = () => window.removeEventListener(eventName, handler);
    } else if (kind === 'magnetometer' && typeof window.Magnetometer === 'function') {
        try {
            const sensor = new window.Magnetometer({ frequency: Math.max(1, Math.round(1000 / Math.max(intervalMs, 16))) });
            sensor.addEventListener('reading', () => emit({ x: sensor.x, y: sensor.y, z: sensor.z, w: 0 }));
            sensor.start();
            stop = () => sensor.stop();
        } catch {
            return 0;
        }
    } else {
        return 0;
    }

    sensors.set(id, stop);
    return id;
}

export function stopSensor(id) {
    const stop = sensors.get(id);
    sensors.delete(id);
    stop?.();
}

// W3C device orientation uses intrinsic Z-X'-Y'' angles.
function eulerToQuaternion(alpha, beta, gamma) {
    const cX = Math.cos(beta / 2), cY = Math.cos(gamma / 2), cZ = Math.cos(alpha / 2);
    const sX = Math.sin(beta / 2), sY = Math.sin(gamma / 2), sZ = Math.sin(alpha / 2);
    return {
        w: cX * cY * cZ - sX * sY * sZ,
        x: sX * cY * cZ - cX * sY * sZ,
        y: cX * sY * cZ + sX * cY * sZ,
        z: cX * cY * sZ + sX * sY * cZ,
    };
}

// ---- geolocation ---------------------------------------------------------------

// Position.timestamp is specified as epoch milliseconds, but some engines (WebKit) report it in
// microseconds; anything implausibly far from now is rescaled, or replaced when unusable.
function epochMilliseconds(t) {
    const now = Date.now();
    if (!Number.isFinite(t) || t <= 0) return now;
    if (t > now * 100) return t / 1000;
    return t;
}

function toLocation(p) {
    const c = p.coords;
    return {
        latitude: c.latitude, longitude: c.longitude,
        altitude: c.altitude ?? NaN, accuracy: c.accuracy ?? NaN, altitudeAccuracy: c.altitudeAccuracy ?? NaN,
        heading: c.heading ?? NaN, speed: c.speed ?? NaN, timestamp: epochMilliseconds(p.timestamp),
        error: 0,
    };
}

// Resolves to a location object; on failure its `error` is the GeolocationPositionError code.
export function getLocation(highAccuracy, timeoutMs) {
    return new Promise(resolve => {
        if (!navigator.geolocation) { resolve({ error: 2 }); return; }
        navigator.geolocation.getCurrentPosition(
            p => resolve(toLocation(p)),
            e => resolve({ error: e.code || 2 }),
            { enableHighAccuracy: highAccuracy, timeout: timeoutMs > 0 ? timeoutMs : undefined, maximumAge: 0 });
    });
}

export function watchLocation(highAccuracy, callback) {
    return navigator.geolocation.watchPosition(
        p => callback(toLocation(p)),
        e => callback({ error: e.code || 2 }),
        { enableHighAccuracy: highAccuracy });
}

export function clearWatch(id) { navigator.geolocation?.clearWatch(id); }

// ---- speech ---------------------------------------------------------------------

function loadVoices() {
    const voices = speechSynthesis.getVoices();
    if (voices.length) return Promise.resolve(voices);
    // Chrome populates voices asynchronously.
    return new Promise(resolve => {
        const timer = setTimeout(() => resolve(speechSynthesis.getVoices()), 1500);
        speechSynthesis.addEventListener('voiceschanged', () => { clearTimeout(timer); resolve(speechSynthesis.getVoices()); }, { once: true });
    });
}

// Voices as "lang\u001fname\u001fvoiceURI" records separated by \u001e.
export async function getVoices() {
    if (!('speechSynthesis' in window)) return '';
    return (await loadVoices()).map(v => [v.lang, v.name, v.voiceURI].join('\u001f')).join('\u001e');
}

export async function speak(text, voiceUri, lang, pitch, volume, rate) {
    const utterance = new SpeechSynthesisUtterance(text);
    if (voiceUri) {
        const voice = (await loadVoices()).find(v => v.voiceURI === voiceUri);
        if (voice) utterance.voice = voice;
    }
    if (lang) utterance.lang = lang;
    if (pitch >= 0) utterance.pitch = pitch;
    if (volume >= 0) utterance.volume = volume;
    if (rate >= 0) utterance.rate = rate;
    await new Promise((resolve, reject) => {
        utterance.onend = () => resolve();
        utterance.onerror = e => e.error === 'interrupted' || e.error === 'canceled' ? resolve() : reject(new Error(e.error));
        speechSynthesis.speak(utterance);
    });
}

export function cancelSpeech() { if ('speechSynthesis' in window) speechSynthesis.cancel(); }

// ---- contacts ---------------------------------------------------------------------

export function contactsSupported() { return 'contacts' in navigator && 'ContactsManager' in window; }

// Resolves to "name\u001femails\u001fphones" (lists newline separated) or null when cancelled.
export async function pickContact() {
    const [contact] = await navigator.contacts.select(['name', 'email', 'tel'], { multiple: false });
    if (!contact) return null;
    return [contact.name?.[0] ?? '', (contact.email ?? []).join('\n'), (contact.tel ?? []).join('\n')].join('\u001f');
}
