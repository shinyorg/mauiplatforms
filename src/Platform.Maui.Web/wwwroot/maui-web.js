// DOM bridge for Microsoft.Maui.Platform.Web.
// Every export is bound by Interop/Dom.cs through [JSImport]; keep the two in sync.
// The .NET side owns all layout: elements are absolutely positioned and sized by
// MAUI's cross-platform layout, so nothing here should rely on CSS flow layout.

const listeners = new Map();
let nextListenerId = 1;
let measureHost = null;

export function init() {
    if (!document.querySelector('link[data-maui-web]')) {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = new URL('./maui-web.css', import.meta.url).href;
        link.dataset.mauiWeb = '';
        document.head.appendChild(link);
    }
    return document.baseURI;
}

// ---- elements -------------------------------------------------------------

export function createElement(tag, className) {
    const el = document.createElement(tag);
    if (className) el.className = className;
    return el;
}

export function createSvgElement(tag) {
    return document.createElementNS('http://www.w3.org/2000/svg', tag);
}

export function getBody() { return document.body; }
export function getDocument() { return document; }
export function querySelector(selector) { return document.querySelector(selector); }

export function setStyle(el, name, value) {
    if (value === null || value === undefined || value === '') el.style.removeProperty(name);
    else el.style.setProperty(name, value);
}

export function setAttribute(el, name, value) {
    if (value === null || value === undefined) el.removeAttribute(name);
    else el.setAttribute(name, value);
}

export function getAttribute(el, name) { return el.getAttribute(name); }

export function setPropString(el, name, value) { if (el[name] !== value) el[name] = value ?? ''; }
export function setPropBool(el, name, value) { el[name] = value; }
export function setPropNumber(el, name, value) { el[name] = value; }
export function getPropString(el, name) { const v = el[name]; return v === null || v === undefined ? null : String(v); }
export function getPropNumber(el, name) { return Number(el[name]) || 0; }
export function getPropBool(el, name) { return !!el[name]; }

export function setText(el, text) { if (el.textContent !== text) el.textContent = text ?? ''; }
export function setHtml(el, html) { el.innerHTML = html ?? ''; }

export function toggleClass(el, name, on) { el.classList.toggle(name, on); }

export function appendChild(parent, child) { if (child.parentNode !== parent || parent.lastChild !== child) parent.appendChild(child); }

export function insertChild(parent, child, index) {
    const ref = parent.children[index] ?? null;
    if (ref !== child) parent.insertBefore(child, ref);
}

export function removeChild(parent, child) { if (child.parentNode === parent) parent.removeChild(child); }
export function detach(el) { el.remove(); }
export function clearChildren(el) { el.replaceChildren(); }

export function setFrame(el, x, y, w, h) {
    const s = el.style;
    s.left = x + 'px';
    s.top = y + 'px';
    s.width = w + 'px';
    s.height = h + 'px';
}

// Measures the element's natural size within the given constraints (-1 = unbounded).
// Detached subtrees are measured inside an invisible host so styles still apply.
export function measure(el, widthConstraint, heightConstraint) {
    let attachedRoot = null;
    if (!el.isConnected) {
        attachedRoot = el;
        while (attachedRoot.parentNode) attachedRoot = attachedRoot.parentNode;
        getMeasureHost().appendChild(attachedRoot);
    }

    const s = el.style;
    const saved = [s.width, s.height, s.maxWidth, s.maxHeight, s.minWidth, s.minHeight, s.transform];
    s.width = el.dataset.mauiMeasureWidth || 'max-content';
    s.height = 'auto';
    s.minWidth = '';
    s.minHeight = '';
    s.maxWidth = widthConstraint >= 0 ? widthConstraint + 'px' : 'none';
    s.maxHeight = heightConstraint >= 0 ? heightConstraint + 'px' : 'none';
    s.transform = 'none';

    // The used size from computed style ignores transforms on ancestors (an animating or scaled
    // container would otherwise shrink the measurement) and keeps sub-pixel precision.
    const cs = getComputedStyle(el);
    let width = parseFloat(cs.width), height = parseFloat(cs.height);
    if (!Number.isFinite(width) || !Number.isFinite(height)) {
        const r = el.getBoundingClientRect();
        width = r.width;
        height = r.height;
    }

    [s.width, s.height, s.maxWidth, s.maxHeight, s.minWidth, s.minHeight, s.transform] = saved;
    if (attachedRoot) attachedRoot.remove();
    return [width, height];
}

// Content height of a textarea laid out at the given width (Editor AutoSize).
export function measureScrollHeight(el, width) {
    const s = el.style;
    const saved = [s.width, s.height];
    s.width = width + 'px';
    s.height = '0px';
    const cs = getComputedStyle(el);
    const height = el.scrollHeight + parseFloat(cs.borderTopWidth) + parseFloat(cs.borderBottomWidth);
    [s.width, s.height] = saved;
    return height;
}

function getMeasureHost() {
    if (!measureHost || !measureHost.isConnected) {
        measureHost = document.createElement('div');
        measureHost.className = 'maui-measure-host';
        document.body.appendChild(measureHost);
    }
    return measureHost;
}

// ---- events ---------------------------------------------------------------

function addListener(el, type, fn, options) {
    el.addEventListener(type, fn, options);
    const id = nextListenerId++;
    listeners.set(id, { el, type, fn, options });
    return id;
}

export function listen(el, type, callback, preventDefault) {
    return addListener(el, type, e => { if (preventDefault) e.preventDefault(); callback(); });
}

export function listenStopped(el, type, callback) {
    return addListener(el, type, e => { e.stopPropagation(); callback(); });
}

// Passes the control's current value; checkable inputs report "true"/"false".
export function listenValue(el, type, callback) {
    return addListener(el, type, () => {
        const t = el.type;
        callback(t === 'checkbox' || t === 'radio' ? String(el.checked) : String(el.value ?? ''));
    });
}

export function listenKey(el, type, callback) {
    return addListener(el, type, e => callback(e.key));
}

// Pointer coordinates are relative to the element's border box.
export function listenPointer(el, type, callback, capture) {
    return addListener(el, type, e => {
        const r = el.getBoundingClientRect();
        if (capture && type === 'pointerdown' && e.pointerId !== undefined) {
            try { el.setPointerCapture(e.pointerId); } catch { /* not capturable */ }
        }
        const flags = (e.button ?? 0) + (e.pointerType === 'touch' ? 16 : 0) + (e.detail ? e.detail * 32 : 0);
        callback(e.clientX - r.left, e.clientY - r.top, flags);
    });
}

// Drag tracking for SwipeView and RefreshView. axes: 'h' | 'v' | 'hv' | 'down' (a pull that only
// starts when every scrollable ancestor of the target, up to el, is at the top).
// callback(phase, dx, dy): 0 = started (after an axis-locked slop), 1 = moved, 2 = released, 3 = cancelled.
// Mouse and pen use pointer events with capture; touch uses touch events so the pull can
// preventDefault native scrolling. The click that ends a drag is swallowed.
export function listenDrag(el, axes, callback) {
    const slop = 6;
    let start = null;   // { x, y, touch }
    let engaged = false;

    const atTop = target => {
        for (let n = target; n && n !== el; n = n.parentElement)
            if (n.scrollTop > 0) return false;
        return true;
    };
    const begin = (x, y, target, touch) => {
        if (axes === 'down' && !atTop(target)) return;
        start = { x, y, touch };
        engaged = false;
    };
    // Returns true when the event should be prevented (the drag owns it).
    const move = (x, y) => {
        if (!start) return false;
        const dx = x - start.x, dy = y - start.y;
        if (!engaged) {
            if (Math.abs(dx) < slop && Math.abs(dy) < slop) return false;
            const horizontal = Math.abs(dx) > Math.abs(dy);
            const ok = axes === 'hv'
                || (axes === 'h' && horizontal)
                || (axes === 'v' && !horizontal)
                || (axes === 'down' && !horizontal && dy > 0);
            if (!ok) { start = null; return false; }
            engaged = true;
            // A mouse drag over text starts a selection on pointerdown; drop it once this is a drag.
            if (!start.touch) window.getSelection()?.removeAllRanges();
            callback(0, dx, dy);
        }
        callback(1, dx, dy);
        return true;
    };
    const finish = (x, y, cancelled) => {
        if (!start) return;
        const wasEngaged = engaged;
        const dx = x - start.x, dy = y - start.y;
        start = null;
        engaged = false;
        if (!wasEngaged) return;
        callback(cancelled ? 3 : 2, dx, dy);
        if (!cancelled) {
            const swallow = e => { e.stopPropagation(); e.preventDefault(); };
            window.addEventListener('click', swallow, { capture: true, once: true });
            setTimeout(() => window.removeEventListener('click', swallow, { capture: true }), 0);
        }
    };

    const ids = [
        addListener(el, 'pointerdown', e => {
            if (e.pointerType === 'touch' || e.button !== 0) return;
            begin(e.clientX, e.clientY, e.target, false);
        }),
        addListener(el, 'pointermove', e => {
            if (e.pointerType === 'touch' || !start) return;
            const wasEngaged = engaged;
            if (move(e.clientX, e.clientY) && !wasEngaged) {
                try { el.setPointerCapture(e.pointerId); } catch { /* not capturable */ }
            }
        }),
        addListener(el, 'pointerup', e => { if (e.pointerType !== 'touch') finish(e.clientX, e.clientY, false); }),
        addListener(el, 'pointercancel', e => { if (e.pointerType !== 'touch') finish(e.clientX, e.clientY, true); }),
        addListener(el, 'touchstart', e => {
            if (e.touches.length !== 1) { start = null; return; }
            const t = e.touches[0];
            begin(t.clientX, t.clientY, e.target, true);
        }, { passive: true }),
        addListener(el, 'touchmove', e => {
            const t = e.touches[0];
            if (t && move(t.clientX, t.clientY) && e.cancelable) e.preventDefault();
        }, { passive: false }),
        addListener(el, 'touchend', e => {
            const t = e.changedTouches[0];
            if (t) finish(t.clientX, t.clientY, false);
        }),
        addListener(el, 'touchcancel', e => {
            const t = e.changedTouches[0];
            finish(t ? t.clientX : 0, t ? t.clientY : 0, true);
        }),
    ];
    // One id releases the whole group (see unlistenGroup).
    const id = nextListenerId++;
    groups.set(id, ids);
    return id;
}

const groups = new Map();

export function listenScroll(el, callback) {
    return addListener(el, 'scroll', () => callback(el.scrollLeft, el.scrollTop), { passive: true });
}

export function listenWheel(el, callback) {
    return addListener(el, 'wheel', e => { if (e.ctrlKey) { e.preventDefault(); callback(e.deltaY); } }, { passive: false });
}

export function unlisten(id) {
    const group = groups.get(id);
    if (group) {
        groups.delete(id);
        group.forEach(unlisten);
        return;
    }
    const l = listeners.get(id);
    if (!l) return;
    listeners.delete(id);
    l.el.removeEventListener(l.type, l.fn, l.options);
}

export function observeResize(el, callback) {
    const ro = new ResizeObserver(entries => {
        const box = entries[entries.length - 1].contentRect;
        callback(box.width, box.height);
    });
    ro.observe(el);
    const id = nextListenerId++;
    listeners.set(id, { el, type: 'resize', fn: null, ro });
    return id;
}

// Reports whether the element intersects the viewport (true/false) whenever that changes.
export function observeVisibility(el, callback) {
    const io = new IntersectionObserver(entries => callback(entries[entries.length - 1].isIntersecting));
    io.observe(el);
    const id = nextListenerId++;
    listeners.set(id, { el, type: 'visibility', fn: null, ro: io });
    return id;
}

export function unobserve(id) {
    const l = listeners.get(id);
    if (!l) return;
    listeners.delete(id);
    l.ro?.disconnect();
}

export function focus(el) { el.focus({ preventScroll: false }); return document.activeElement === el || el.contains(document.activeElement); }
export function blur(el) { el.blur(); }
export function hasFocus(el) { return el.contains(document.activeElement); }

// ---- scheduling -----------------------------------------------------------

const queueChannel = new MessageChannel();
const queued = [];
queueChannel.port1.onmessage = () => {
    const batch = queued.splice(0, queued.length);
    for (const cb of batch) cb();
};

export function enqueue(callback) {
    queued.push(callback);
    if (queued.length === 1) queueChannel.port2.postMessage(0);
}

export function requestFrame(callback) { return requestAnimationFrame(() => callback()); }
export function startTimeout(callback, ms) { return setTimeout(() => callback(), ms); }
export function stopTimeout(id) { clearTimeout(id); }
export function startInterval(callback, ms) { return setInterval(() => callback(), ms); }
export function stopInterval(id) { clearInterval(id); }

let tickerId = 0;
export function startTicker(callback) {
    stopTicker();
    const loop = () => { tickerId = requestAnimationFrame(loop); callback(); };
    tickerId = requestAnimationFrame(loop);
}
export function stopTicker() { if (tickerId) cancelAnimationFrame(tickerId); tickerId = 0; }
export function prefersReducedMotion() { return matchMedia('(prefers-reduced-motion: reduce)').matches; }

// ---- document & theme -----------------------------------------------------

export function setDocumentTitle(title) { document.title = title ?? ''; }
export function getDocumentTitle() { return document.title; }

export function getColorScheme() { return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'; }

export function observeColorScheme(callback) {
    const mq = matchMedia('(prefers-color-scheme: dark)');
    mq.addEventListener('change', () => callback(mq.matches ? 'dark' : 'light'));
}

export function setRootTheme(theme) {
    if (theme) document.documentElement.dataset.mauiTheme = theme;
    else delete document.documentElement.dataset.mauiTheme;
}

export function viewportWidth() { return window.innerWidth; }

// ---- fonts ----------------------------------------------------------------

// Variable fonts must declare their axis ranges, or the browser only uses them for the
// default weight/width and synthesizes (or ignores) bold. Detect them by their 'fvar' table.
function fontDescriptors(buffer) {
    const view = new DataView(buffer);
    if (view.byteLength < 12) return {};
    const tables = view.getUint16(4);
    for (let i = 0; i < tables && 12 + i * 16 + 4 <= view.byteLength; i++) {
        if (view.getUint32(12 + i * 16) === 0x66766172 /* 'fvar' */)
            return { weight: '1 1000', stretch: '25% 200%' };
    }
    return {};
}

async function addFont(family, buffer) {
    const face = new FontFace(family, buffer, fontDescriptors(buffer));
    document.fonts.add(await face.load());
}

// Glyph rendering waits on these so FontImageSource never rasterizes a fallback font.
const pendingFonts = [];
function track(promise) {
    pendingFonts.push(promise.catch(() => { }));
    return promise;
}

export function loadFontUrl(family, url) {
    return track((async () => {
        const response = await fetch(url);
        if (!response.ok) throw new Error(`${url}: HTTP ${response.status}`);
        await addFont(family, await response.arrayBuffer());
    })());
}

export function loadFontBytes(family, bytes) {
    // Copy now: the .NET memory view is only valid during this call.
    const buffer = bytes.slice().buffer;
    return track(addFont(family, buffer));
}

export function observeFonts(callback) {
    document.fonts.addEventListener('loadingdone', () => callback());
}

// ---- images ---------------------------------------------------------------

export function createObjectUrl(bytes, mime) {
    return URL.createObjectURL(new Blob([bytes.slice()], { type: mime || 'application/octet-stream' }));
}

export function revokeObjectUrl(url) { if (url && url.startsWith('blob:')) URL.revokeObjectURL(url); }

export function naturalSize(img) { return [img.naturalWidth || 0, img.naturalHeight || 0]; }

// Rasterises a font glyph (FontImageSource) into a PNG data URL at device resolution.
export async function renderGlyph(glyph, fontFamily, size, color) {
    const scale = window.devicePixelRatio || 1;
    // Canvas fonts don't resolve CSS variables; substitute the page font stack.
    const family = fontFamily.replace(/var\(--maui-font\)/g, getComputedStyle(document.documentElement).getPropertyValue('--maui-font') || 'sans-serif');
    const font = `${size * scale}px ${family}`;
    await Promise.all(pendingFonts);
    try { await document.fonts.load(font, glyph); } catch { /* fall back to whatever is loaded */ }
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    const setup = () => {
        ctx.font = font;
        ctx.fillStyle = color || 'currentColor';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
    };
    // Icon glyphs can be wider than 1em (Font Awesome draws many at 1.25em), so grow the bitmap
    // around the centre point to fit the glyph's ink instead of clipping it to a size x size square.
    setup();
    const m = ctx.measureText(glyph);
    const square = Math.ceil(size * scale);
    canvas.width = Math.max(square, 2 * Math.ceil(Math.max(m.actualBoundingBoxLeft, m.actualBoundingBoxRight)));
    canvas.height = Math.max(square, 2 * Math.ceil(Math.max(m.actualBoundingBoxAscent, m.actualBoundingBoxDescent)));
    setup(); // resizing a canvas resets its context state
    ctx.fillText(glyph, canvas.width / 2, canvas.height / 2);
    return canvas.toDataURL('image/png');
}

// ---- controls -------------------------------------------------------------

export function setSelectOptions(select, items, title) {
    const selected = select.selectedIndex;
    select.replaceChildren();
    if (title) {
        const placeholder = document.createElement('option');
        placeholder.textContent = title;
        placeholder.disabled = true;
        placeholder.hidden = true;
        placeholder.value = '';
        select.appendChild(placeholder);
    }
    items.forEach((text, i) => {
        const o = document.createElement('option');
        o.textContent = text;
        o.value = String(i);
        select.appendChild(o);
    });
    select.selectedIndex = selected;
}

export function setSelectedOption(select, index) {
    const option = [...select.options].findIndex(o => o.value === String(index));
    if (option >= 0) select.selectedIndex = option;
    else select.value = '';
}

export function setInputSelection(el, start, end) {
    try { el.setSelectionRange(start, end); } catch { /* input type without selection */ }
}

export function getInputSelection(el) {
    try { return [el.selectionStart ?? 0, el.selectionEnd ?? 0]; } catch { return [0, 0]; }
}

export function scrollTo(el, x, y, smooth) {
    el.scrollTo({ left: x, top: y, behavior: smooth ? 'smooth' : 'instant' });
}

export function getScrollExtent(el) { return [el.scrollWidth, el.scrollHeight, el.clientWidth, el.clientHeight]; }

export function setIframeSource(frame, url, html) {
    if (html !== null && html !== undefined) { frame.removeAttribute('src'); frame.srcdoc = html; }
    else { frame.removeAttribute('srcdoc'); frame.src = url || 'about:blank'; }
}

export function evaluateInFrame(frame, script) {
    try { return String(frame.contentWindow.eval(script) ?? ''); }
    catch (e) { return null; }
}

export function frameHistory(frame, delta) {
    try { frame.contentWindow.history.go(delta); return true; } catch { return false; }
}

export function frameUrl(frame) {
    try { return frame.contentWindow.location.href; } catch { return frame.src; }
}

// ---- GraphicsView ---------------------------------------------------------

// GraphicsView images, decoded once by the browser. A canvas that tries to draw one before it has
// decoded is sent 'maui-image-ready' when it has, and redraws.
const canvasImages = new Map();

export function registerCanvasImage(id, bytes) {
    const img = new Image();
    const entry = { img, ready: false, waiting: new Set() };
    canvasImages.set(id, entry);
    img.onload = () => {
        entry.ready = true;
        for (const canvas of entry.waiting) canvas.dispatchEvent(new Event('maui-image-ready'));
        entry.waiting.clear();
    };
    img.src = URL.createObjectURL(new Blob([bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes)]));
}

// Replays the command list recorded by WebCanvas.cs onto a 2D context sized for the device pixel ratio.
export function drawCommands(canvas, width, height, json) {
    const dpr = window.devicePixelRatio || 1;
    const pw = Math.max(1, Math.round(width * dpr)), ph = Math.max(1, Math.round(height * dpr));
    if (canvas.width !== pw) canvas.width = pw;
    if (canvas.height !== ph) canvas.height = ph;
    const ctx = canvas.getContext('2d');
    ctx.reset?.();
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, width, height);
    ctx.font = '12px system-ui, sans-serif';
    let fillStyle = 'transparent';
    for (const [op, ...a] of JSON.parse(json)) {
        switch (op) {
            case 'lineWidth': case 'miterLimit': case 'strokeStyle': case 'lineCap': case 'lineJoin':
            case 'lineDashOffset': case 'globalAlpha': case 'globalCompositeOperation': case 'font':
                ctx[op] = a[0]; break;
            case 'fillStyle': ctx.fillStyle = fillStyle = a[0]; break;
            case 'dash': ctx.setLineDash(a[0] ? a[0].split(' ').map(Number) : []); break;
            case 'save': ctx.save(); break;
            case 'restore': ctx.restore(); break;
            case 'reset': ctx.setTransform(dpr, 0, 0, dpr, 0, 0); break;
            case 'line': ctx.beginPath(); ctx.moveTo(a[0], a[1]); ctx.lineTo(a[2], a[3]); ctx.stroke(); break;
            case 'strokeRect': ctx.strokeRect(...a); break;
            case 'fillRect': ctx.fillRect(...a); break;
            case 'strokeRoundRect': ctx.beginPath(); ctx.roundRect(a[0], a[1], a[2], a[3], a[4]); ctx.stroke(); break;
            case 'fillRoundRect': ctx.beginPath(); ctx.roundRect(a[0], a[1], a[2], a[3], a[4]); ctx.fill(); break;
            case 'strokeEllipse': ctx.beginPath(); ctx.ellipse(a[0] + a[2] / 2, a[1] + a[3] / 2, a[2] / 2, a[3] / 2, 0, 0, Math.PI * 2); ctx.stroke(); break;
            case 'fillEllipse': ctx.beginPath(); ctx.ellipse(a[0] + a[2] / 2, a[1] + a[3] / 2, a[2] / 2, a[3] / 2, 0, 0, Math.PI * 2); ctx.fill(); break;
            case 'stroke': ctx.stroke(new Path2D(a[0])); break;
            case 'fill': ctx.fill(new Path2D(a[0])); break;
            case 'fillEvenOdd': ctx.fill(new Path2D(a[0]), 'evenodd'); break;
            case 'clip': ctx.clip(new Path2D(a[0])); break;
            case 'clipEvenOdd': ctx.clip(new Path2D(a[0]), 'evenodd'); break;
            case 'clipRect': ctx.beginPath(); ctx.rect(...a); ctx.clip(); break;
            case 'subtractClip': {
                const p = new Path2D();
                p.rect(-1e5, -1e5, 2e5, 2e5);
                p.rect(a[0], a[1], a[2], a[3]);
                ctx.clip(p, 'evenodd');
                break;
            }
            case 'text': ctx.textAlign = a[3]; ctx.textBaseline = a[4]; ctx.fillText(a[0], a[1], a[2]); break;
            case 'image': {
                const entry = canvasImages.get(a[0]);
                if (entry?.ready) ctx.drawImage(entry.img, a[1], a[2], a[3], a[4]);
                else entry?.waiting.add(canvas);
                break;
            }
            case 'translate': ctx.translate(a[0], a[1]); break;
            case 'rotate': ctx.rotate(a[0]); break;
            case 'scale': ctx.scale(a[0], a[1]); break;
            case 'transform': ctx.transform(...a); break;
            case 'shadow': ctx.shadowOffsetX = a[0]; ctx.shadowOffsetY = a[1]; ctx.shadowBlur = a[2]; ctx.shadowColor = a[3]; break;
            case 'gradient': {
                const g = a[0] === 'linear' ? ctx.createLinearGradient(a[1], a[2], a[3], a[4]) : ctx.createRadialGradient(a[1], a[2], 0, a[1], a[2], a[5]);
                for (const stop of a[6].split(';')) { const [o, c] = stop.split('|'); g.addColorStop(Number(o), c); }
                ctx.fillStyle = g;
                break;
            }
        }
    }
}

export function measureText(font, text) {
    const ctx = (measureText.ctx ??= document.createElement('canvas').getContext('2d'));
    ctx.font = font;
    const m = ctx.measureText(text);
    return [m.width, (m.actualBoundingBoxAscent ?? 0) + (m.actualBoundingBoxDescent ?? 0)];
}

// ---- dialogs --------------------------------------------------------------

function createDialog(title, message) {
    const dialog = document.createElement('dialog');
    dialog.className = 'maui-dialog';
    if (title) {
        const h = document.createElement('h2');
        h.className = 'maui-dialog-title';
        h.textContent = title;
        dialog.appendChild(h);
    }
    if (message) {
        const p = document.createElement('p');
        p.className = 'maui-dialog-message';
        p.textContent = message;
        dialog.appendChild(p);
    }
    document.body.appendChild(dialog);
    return dialog;
}

function addButton(container, text, kind, onClick) {
    const b = document.createElement('button');
    b.type = 'button';
    b.className = 'maui-dialog-button' + (kind ? ' ' + kind : '');
    b.textContent = text;
    b.addEventListener('click', onClick);
    container.appendChild(b);
    return b;
}

function finish(dialog, resolve, value) {
    if (dialog.dataset.done) return;
    dialog.dataset.done = '1';
    resolve(value);
    dialog.close();
    dialog.remove();
}

export function showAlert(title, message, accept, cancel) {
    return new Promise(resolve => {
        const dialog = createDialog(title, message);
        const actions = document.createElement('div');
        actions.className = 'maui-dialog-actions';
        // DisplayAlert(title, message, cancel) has a single button, which is the cancel one.
        const single = !accept;
        if (cancel && !single) addButton(actions, cancel, '', () => finish(dialog, resolve, false));
        const ok = single
            ? addButton(actions, cancel || 'OK', 'primary', () => finish(dialog, resolve, false))
            : addButton(actions, accept, 'primary', () => finish(dialog, resolve, true));
        dialog.appendChild(actions);
        dialog.addEventListener('cancel', () => finish(dialog, resolve, false));
        dialog.showModal();
        ok.focus();
    });
}

export function showActionSheet(title, cancel, destruction, buttons) {
    return new Promise(resolve => {
        const dialog = createDialog(title, null);
        dialog.classList.add('maui-action-sheet');
        const list = document.createElement('div');
        list.className = 'maui-dialog-list';
        for (const text of buttons) addButton(list, text, '', () => finish(dialog, resolve, text));
        if (destruction) addButton(list, destruction, 'destructive', () => finish(dialog, resolve, destruction));
        dialog.appendChild(list);
        if (cancel) {
            const actions = document.createElement('div');
            actions.className = 'maui-dialog-actions';
            addButton(actions, cancel, '', () => finish(dialog, resolve, cancel));
            dialog.appendChild(actions);
        }
        dialog.addEventListener('cancel', () => finish(dialog, resolve, cancel ?? null));
        dialog.showModal();
    });
}

export function showPrompt(title, message, accept, cancel, placeholder, initialValue, maxLength, inputMode) {
    return new Promise(resolve => {
        const dialog = createDialog(title, message);
        const form = document.createElement('form');
        form.method = 'dialog';
        const input = document.createElement('input');
        input.className = 'maui-dialog-input';
        input.placeholder = placeholder ?? '';
        input.value = initialValue ?? '';
        if (maxLength > 0) input.maxLength = maxLength;
        if (inputMode) input.inputMode = inputMode;
        form.appendChild(input);
        const actions = document.createElement('div');
        actions.className = 'maui-dialog-actions';
        addButton(actions, cancel || 'Cancel', '', () => finish(dialog, resolve, null));
        const ok = addButton(actions, accept || 'OK', 'primary', () => finish(dialog, resolve, input.value));
        ok.type = 'submit';
        form.appendChild(actions);
        form.addEventListener('submit', e => { e.preventDefault(); finish(dialog, resolve, input.value); });
        dialog.appendChild(form);
        dialog.addEventListener('cancel', () => finish(dialog, resolve, null));
        dialog.showModal();
        input.focus();
        input.select();
    });
}

export function log(message) { console.debug('[maui-web]', message); }
