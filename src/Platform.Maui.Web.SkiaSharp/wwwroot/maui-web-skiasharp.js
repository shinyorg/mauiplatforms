// Canvas and pointer bridge for Platform.Maui.Web.SkiaSharp.
// Every export is bound by Interop/SkiaJs.cs through [JSImport]; keep the two in sync.

const states = new WeakMap();

export function requestFrame(callback) {
    requestAnimationFrame(() => callback());
}

export function devicePixelRatio() {
    return window.devicePixelRatio || 1;
}

// Copies a frame Skia rendered (RGBA, unpremultiplied) onto the canvas, resizing its backing store
// when the frame size changed. The view is only valid during this call, so it is copied out here.
export function present(canvas, view, width, height) {
    if (canvas.width !== width) canvas.width = width;
    if (canvas.height !== height) canvas.height = height;
    let state = states.get(canvas);
    if (!state) states.set(canvas, state = {});
    state.ctx ??= canvas.getContext('2d');
    state.ctx.putImageData(new ImageData(new Uint8ClampedArray(view.slice().buffer), width, height), 0, 0);
}

// Calls back when the device pixel ratio changes (browser zoom, or a move to another monitor), which
// changes the canvas resolution without resizing it.
export function watchPixelRatio(canvas, callback) {
    let state = states.get(canvas);
    if (!state) states.set(canvas, state = {});
    unwatchPixelRatio(canvas);
    const arm = () => {
        const query = matchMedia(`(resolution: ${window.devicePixelRatio || 1}dppx)`);
        const onChange = () => { arm(); callback(); };
        query.addEventListener('change', onChange, { once: true });
        state.unwatchRatio = () => query.removeEventListener('change', onChange);
    };
    arm();
}

export function unwatchPixelRatio(canvas) {
    const state = states.get(canvas);
    state?.unwatchRatio?.();
    if (state) state.unwatchRatio = null;
}

// Forwards pointer and wheel events to .NET as SKTouchEventArgs fields. The callback returns whether
// the app handled the event: a handled press captures the pointer and suppresses the browser default.
export function attachTouch(canvas, callback) {
    let state = states.get(canvas);
    if (!state) states.set(canvas, state = {});
    detachTouch(canvas);

    const send = (type, e, wheel) => {
        const r = canvas.getBoundingClientRect();
        return callback({
            type,
            id: e.pointerId ?? 1,
            x: e.clientX - r.left,
            y: e.clientY - r.top,
            // 0 left, 1 middle, 2 right; moves report -1, so fall back to the held buttons.
            button: e.button >= 0 ? e.button : (e.buttons & 1 ? 0 : e.buttons & 4 ? 1 : e.buttons & 2 ? 2 : -1),
            device: e.pointerType || 'mouse',
            inContact: (e.buttons ?? 0) !== 0 || type === 'pressed',
            pressure: e.pressure ?? (e.buttons ? 0.5 : 0),
            wheel,
        });
    };

    const handlers = {
        pointerenter: e => send('entered', e, 0),
        pointerdown: e => {
            if (send('pressed', e, 0)) {
                try { canvas.setPointerCapture(e.pointerId); } catch { /* not capturable */ }
                e.preventDefault();
            }
        },
        pointermove: e => { if (send('moved', e, 0)) e.preventDefault(); },
        pointerup: e => { if (send('released', e, 0)) e.preventDefault(); },
        pointercancel: e => send('cancelled', e, 0),
        pointerleave: e => send('exited', e, 0),
        // Positive away from the user, as SkiaSharp reports it on other platforms; lines become pixels.
        wheel: e => {
            const delta = e.deltaMode === 0 ? -e.deltaY : -e.deltaY * 40;
            if (send('wheel', e, Math.round(delta))) e.preventDefault();
        },
    };
    for (const [type, fn] of Object.entries(handlers))
        canvas.addEventListener(type, fn, { passive: false });
    state.detachTouch = () => {
        for (const [type, fn] of Object.entries(handlers))
            canvas.removeEventListener(type, fn, { passive: false });
    };
}

export function detachTouch(canvas) {
    const state = states.get(canvas);
    state?.detachTouch?.();
    if (state) state.detachTouch = null;
}
