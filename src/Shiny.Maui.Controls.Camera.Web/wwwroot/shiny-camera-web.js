// getUserMedia bridge for Shiny.Maui.Controls.Camera.Web.
// Every export is bound by Interop/CameraJs.cs through [JSImport]; keep the two in sync.

const states = new WeakMap();
const buffers = new Map();
let nextBuffer = 1;

function requireMediaDevices() {
    if (!navigator.mediaDevices?.getUserMedia)
        throw new Error('Camera access needs a secure context (HTTPS or localhost) and a browser with getUserMedia.');
}

function stopStream(stream) {
    stream?.getTracks().forEach(t => t.stop());
}

// ---- buffers handed to .NET -------------------------------------------------

function park(bytes) {
    const id = nextBuffer++;
    buffers.set(id, bytes);
    return id;
}

export function bufferLength(id) { return buffers.get(id)?.length ?? 0; }

export function copyBuffer(id, view) {
    const bytes = buffers.get(id);
    buffers.delete(id);
    if (bytes) view.set(bytes);
}

// ---- devices ----------------------------------------------------------------

export async function requestPermission(includeMicrophone) {
    requireMediaDevices();
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: !!includeMicrophone });
        stopStream(stream);
        return true;
    } catch {
        return false;
    }
}

// Labels are empty until the page has camera permission, so the caller falls back to "Camera n".
export async function listCameras() {
    if (!navigator.mediaDevices?.enumerateDevices) return '[]';
    const devices = await navigator.mediaDevices.enumerateDevices();
    return JSON.stringify(devices
        .filter(d => d.kind === 'videoinput')
        .map(d => ({ id: d.deviceId, label: d.label || '' })));
}

// ---- session ----------------------------------------------------------------

// Resolves with the opened stream's facts as JSON. A stop() or a newer start() while the
// permission prompt is open cancels this one and releases the camera it eventually gets.
export async function start(video, deviceId, facingMode) {
    requireMediaDevices();
    stop(video);

    const token = {};
    states.set(video, { token, stream: null, recorder: null });

    // The ideal size is a preference; without one Chrome opens most cameras at 640x480.
    const constraints = { width: { ideal: 1920 }, height: { ideal: 1080 } };
    if (deviceId) constraints.deviceId = { exact: deviceId };
    else if (facingMode) constraints.facingMode = { ideal: facingMode };

    let stream;
    try {
        stream = await navigator.mediaDevices.getUserMedia({ video: constraints, audio: false });
    } catch (err) {
        if (states.get(video)?.token === token) states.delete(video);
        throw err;
    }

    const state = states.get(video);
    if (state?.token !== token) {
        stopStream(stream);
        return null;
    }

    state.stream = stream;
    video.srcObject = stream;
    try {
        await video.play();
    } catch {
        // A muted, inline video is allowed to autoplay; any rejection here means the element was
        // detached mid-start, and the frames still arrive once it is shown again.
    }

    const track = stream.getVideoTracks()[0];
    const caps = track?.getCapabilities?.() ?? {};
    const settings = track?.getSettings?.() ?? {};
    return JSON.stringify({
        deviceId: settings.deviceId ?? '',
        facingMode: settings.facingMode ?? '',
        width: settings.width ?? video.videoWidth ?? 0,
        height: settings.height ?? video.videoHeight ?? 0,
        zoomMin: caps.zoom?.min ?? 1,
        zoomMax: caps.zoom?.max ?? 1,
        torch: !!caps.torch,
    });
}

export function stop(video) {
    const state = states.get(video);
    if (!state) return;
    states.delete(video);
    if (state.recorder && state.recorder.state !== 'inactive') {
        try { state.recorder.stop(); } catch { }
    }
    stopStream(state.recorderAudio);
    stopStream(state.stream);
    if (video.srcObject === state.stream) video.srcObject = null;
}

function track(video) {
    return states.get(video)?.stream?.getVideoTracks()[0] ?? null;
}

export async function setZoom(video, zoom) {
    const t = track(video);
    if (!t?.getCapabilities?.().zoom) return false;
    try { await t.applyConstraints({ advanced: [{ zoom }] }); return true; }
    catch { return false; }
}

export async function setTorch(video, on) {
    const t = track(video);
    if (!t?.getCapabilities?.().torch) return false;
    try { await t.applyConstraints({ advanced: [{ torch: !!on }] }); return true; }
    catch { return false; }
}

// ---- stills -----------------------------------------------------------------

// Draws the current frame (with the preview's CSS filter baked in) and JPEG-encodes it.
// Resolves with JSON { id, width, height }; the bytes are fetched with bufferLength/copyBuffer.
export async function capture(video, filterCss, quality) {
    const width = video.videoWidth, height = video.videoHeight;
    if (!width || !height) throw new Error('The camera has not produced a frame yet.');

    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');
    if (filterCss) ctx.filter = filterCss;
    ctx.drawImage(video, 0, 0, width, height);

    const blob = await new Promise((resolve, reject) =>
        canvas.toBlob(b => b ? resolve(b) : reject(new Error('Could not encode the photo.')), 'image/jpeg', quality));
    const id = park(new Uint8Array(await blob.arrayBuffer()));
    return JSON.stringify({ id, width, height });
}

// ---- video ------------------------------------------------------------------

export async function startRecording(video, includeAudio) {
    const state = states.get(video);
    if (!state?.stream) throw new Error('The camera is not started.');
    if (state.recorder) throw new Error('A recording is already in progress.');

    let stream = state.stream;
    let audio = null;
    if (includeAudio) {
        try {
            audio = await navigator.mediaDevices.getUserMedia({ audio: true });
            stream = new MediaStream([...state.stream.getVideoTracks(), ...audio.getAudioTracks()]);
        } catch {
            audio = null; // microphone refused: record video only
        }
    }

    // The microphone prompt can stay open indefinitely; don't start on a camera stopped meanwhile.
    if (states.get(video) !== state) {
        stopStream(audio);
        throw new Error('The camera stopped before recording could start.');
    }

    const chunks = [];
    const recorder = new MediaRecorder(stream);
    recorder.ondataavailable = e => { if (e.data.size) chunks.push(e.data); };
    state.recorder = recorder;
    state.recorderAudio = audio;
    state.chunks = chunks;
    state.startedAt = performance.now();
    recorder.start(1000);
}

// Resolves with JSON { id, mime, durationMs }.
export function stopRecording(video) {
    return new Promise((resolve, reject) => {
        const state = states.get(video);
        const recorder = state?.recorder;
        if (!recorder) { reject(new Error('Not recording.')); return; }

        recorder.onstop = async () => {
            const mime = recorder.mimeType || 'video/webm';
            const blob = new Blob(state.chunks, { type: mime });
            const durationMs = performance.now() - state.startedAt;
            stopStream(state.recorderAudio);
            state.recorder = null;
            state.recorderAudio = null;
            state.chunks = null;
            const id = park(new Uint8Array(await blob.arrayBuffer()));
            resolve(JSON.stringify({ id, mime, durationMs }));
        };
        recorder.stop();
    });
}
