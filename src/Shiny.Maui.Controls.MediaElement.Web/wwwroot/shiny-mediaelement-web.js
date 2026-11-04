// HTML <video> bridge for Shiny.Maui.Controls.MediaElement.Web.
// Every export is bound by Interop/MediaJs.cs through [JSImport]; keep the two in sync.
//
// The <video> element is the player, so it belongs to the backend rather than to any one surface.
// A surface hands the backend its container and the video is moved into it; with no surface, the
// video waits in a hidden parking div so playback carries on. Moving an element with a single
// appendChild never leaves it out of the document, so the browser doesn't pause it in between.

let parking = null;

function park() {
    if (!parking) {
        parking = document.createElement('div');
        parking.dataset.shinyMediaParking = '';
        parking.style.cssText = 'position:fixed;width:0;height:0;overflow:hidden;visibility:hidden;pointer-events:none';
        document.body.appendChild(parking);
    }
    return parking;
}

const states = new WeakMap();
const objectUrls = new WeakMap();

export function create(onEvent) {
    const video = document.createElement('video');
    video.playsInline = true;
    video.preload = 'auto';
    video.style.cssText = 'position:absolute;inset:0;width:100%;height:100%;display:block;object-fit:contain;background:transparent';
    park().appendChild(video);

    const state = { onEvent, wakeLock: null, keepScreenOn: false };
    states.set(video, state);

    const send = (name, value) => state.onEvent?.(name, value ?? 0);
    const duration = () => Number.isFinite(video.duration) ? video.duration : 0;

    video.addEventListener('loadedmetadata', () => send('opened', duration()));
    video.addEventListener('durationchange', () => send('duration', duration()));
    video.addEventListener('resize', () => send('size', 0));
    video.addEventListener('playing', () => { send('playing'); syncWakeLock(video); });
    video.addEventListener('pause', () => { if (!video.ended) send('paused'); syncWakeLock(video); });
    video.addEventListener('waiting', () => send('buffering'));
    video.addEventListener('ended', () => { send('ended'); syncWakeLock(video); });
    video.addEventListener('progress', () => send('progress', buffered(video)));
    video.addEventListener('error', () => send('error', video.error?.code ?? 0));
    video.addEventListener('enterpictureinpicture', () => send('pip', 1));
    video.addEventListener('leavepictureinpicture', () => send('pip', 0));
    return video;
}

function buffered(video) {
    const d = video.duration;
    if (!Number.isFinite(d) || d <= 0 || video.buffered.length === 0) return 0;
    return Math.min(1, video.buffered.end(video.buffered.length - 1) / d);
}

export function errorMessage(video) {
    const e = video.error;
    if (!e) return 'Playback failed';
    const kind = ['', 'aborted', 'network error', 'decode error', 'source not supported'][e.code] ?? 'error';
    return e.message ? `Media ${kind}: ${e.message}` : `Media ${kind}`;
}

export function attach(video, container) {
    (container ?? park()).appendChild(video);
}

function revokeObjectUrl(video) {
    const url = objectUrls.get(video);
    if (url) { URL.revokeObjectURL(url); objectUrls.delete(video); }
}

export function setSource(video, url) {
    revokeObjectUrl(video);
    if (url) { video.src = url; video.load(); }
    else { video.removeAttribute('src'); video.load(); }
}

export function setBytesSource(video, bytes, mime) {
    // MemoryView is only valid during the call; slice() copies it out of the wasm heap.
    const blob = new Blob([bytes.slice()], mime ? { type: mime } : undefined);
    const url = URL.createObjectURL(blob);
    setSource(video, url);
    objectUrls.set(video, url);
}

export function play(video) {
    // play() rejects when the browser's autoplay policy wants a user gesture first. Report it as a
    // pause instead of an unhandled rejection; the transport bar then offers play again.
    const state = states.get(video);
    video.play()?.catch(err => state?.onEvent?.(err?.name === 'NotAllowedError' ? 'blocked' : 'paused', 0));
}

export function pause(video) { video.pause(); }

export function stop(video) {
    video.pause();
    if (video.readyState > 0) video.currentTime = 0;
}

export function currentTime(video) { return video.currentTime || 0; }
export function videoWidth(video) { return video.videoWidth || 0; }
export function videoHeight(video) { return video.videoHeight || 0; }

export function seek(video, seconds) {
    if (!Number.isFinite(seconds)) return;
    const d = video.duration;
    video.currentTime = Number.isFinite(d) ? Math.max(0, Math.min(d, seconds)) : Math.max(0, seconds);
}

export function setVolume(video, volume) { video.volume = Math.max(0, Math.min(1, volume)); }
export function setMuted(video, muted) { video.muted = !!muted; }
export function setRate(video, rate) { if (rate > 0) video.playbackRate = rate; }
export function setLoop(video, loop) { video.loop = !!loop; }
export function setObjectFit(video, fit) { video.style.objectFit = fit; }

export function supportsPictureInPicture() {
    return !!document.pictureInPictureEnabled;
}

export async function enterPictureInPicture(video) {
    if (!document.pictureInPictureEnabled || video.disablePictureInPicture) return false;
    try { await video.requestPictureInPicture(); return true; }
    catch { return false; }
}

export async function exitPictureInPicture(video) {
    if (document.pictureInPictureElement === video) {
        try { await document.exitPictureInPicture(); } catch { }
    }
}

// ---- screen wake lock -----------------------------------------------------

export function setKeepScreenOn(video, on) {
    const state = states.get(video);
    if (!state) return;
    state.keepScreenOn = !!on;
    syncWakeLock(video);
}

async function syncWakeLock(video) {
    const state = states.get(video);
    if (!state || !('wakeLock' in navigator)) return;
    const want = state.keepScreenOn && !video.paused && !video.ended;
    if (want && !state.wakeLock) {
        try {
            state.wakeLock = await navigator.wakeLock.request('screen');
            state.wakeLock.addEventListener('release', () => { state.wakeLock = null; });
        } catch { state.wakeLock = null; }
    } else if (!want && state.wakeLock) {
        const lock = state.wakeLock;
        state.wakeLock = null;
        try { await lock.release(); } catch { }
    }
}

// ---- Media Session (OS transport controls) --------------------------------

export function supportsMediaSession() {
    return 'mediaSession' in navigator;
}

let sessionOwner = null;

export function setMediaSession(video, enabled, title, artist, album, artwork) {
    if (!('mediaSession' in navigator)) return;
    const session = navigator.mediaSession;
    const actions = ['play', 'pause', 'stop', 'seekto'];

    if (!enabled) {
        if (sessionOwner !== video) return;
        sessionOwner = null;
        session.metadata = null;
        for (const a of actions) { try { session.setActionHandler(a, null); } catch { } }
        return;
    }

    sessionOwner = video;
    session.metadata = new MediaMetadata({
        title: title ?? '',
        artist: artist ?? '',
        album: album ?? '',
        artwork: artwork ? [{ src: artwork }] : [],
    });

    const send = (name, value) => states.get(video)?.onEvent?.(name, value ?? 0);
    const handlers = {
        play: () => send('remote-play'),
        pause: () => send('remote-pause'),
        stop: () => send('remote-stop'),
        seekto: d => { if (Number.isFinite(d.seekTime)) { video.currentTime = d.seekTime; send('remote-seek', d.seekTime); } },
    };
    for (const a of actions) { try { session.setActionHandler(a, handlers[a]); } catch { } }
}

export function dispose(video) {
    const state = states.get(video);
    if (state) {
        state.onEvent = null;
        state.keepScreenOn = false;
        state.wakeLock?.release().catch(() => { });
        state.wakeLock = null;
    }
    revokeObjectUrl(video);
    if (sessionOwner === video) setMediaSession(video, false);
    video.pause();
    video.removeAttribute('src');
    video.load();
    video.remove();
    states.delete(video);
}

// ---- VideoText --------------------------------------------------------------
// The mask and overlay are SVG built in C# (VideoTextLayout). This only does what markup can't: set the
// muted *property* (the attribute is just defaultMuted), keep a decorative loop playing when audible
// autoplay is refused, and measure glyphs so auto-fit and the marquee use real widths.

export function videoTextSetMuted(video, muted) {
    video.defaultMuted = !!muted;
    video.muted = !!muted;
    video.play()?.catch(() => {
        // Audible autoplay needs a user gesture first. A frozen decorative loop is worse than a silent one.
        if (!video.muted) {
            video.muted = true;
            video.play()?.catch(() => { });
        }
    });
}

export function videoTextStop(video) {
    // A removed <video> is paused, but some engines keep its decoder and connection until collected.
    try {
        video.pause();
        setSource(video, null);
    } catch { }
}

// Widths of each line at 100px, as JSON. Loads the face first: a web font nothing on the page uses yet
// is never fetched, and the fit would be measured in the fallback font.
let measureCanvas = null;
export async function measureLines(lines, family, weight, style) {
    const font = `${style || 'normal'} ${weight || 'normal'} 100px ${family || 'sans-serif'}`;
    try {
        if (document.fonts) {
            await document.fonts.load(font, lines.join(''));
            await document.fonts.ready;
        }
    } catch { }
    measureCanvas ??= document.createElement('canvas');
    const ctx = measureCanvas.getContext('2d');
    ctx.font = font;
    return JSON.stringify(lines.map(l => ctx.measureText(l).width));
}
