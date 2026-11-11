// End-to-end smoke test for the web backend: publishes nothing itself; point it at a published
// wwwroot. It serves the folder, drives headless Chrome over the DevTools protocol and asserts on
// the live DOM. No npm dependencies (Node 22+).
//
//   dotnet publish samples/SampleWeb -c Release -o artifacts/sample
//   node tests/browser/smoke.mjs artifacts/sample/wwwroot
//
// CHROME may point at a Chrome/Chromium binary; the default is the macOS install location.

import { spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { readFile, mkdtemp } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { tmpdir } from 'node:os';

const root = process.argv[2];
if (!root) { console.error('usage: node smoke.mjs <published wwwroot>'); process.exit(2); }
const chromePath = process.env.CHROME ?? '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const types = { '.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.json': 'application/json',
  '.wasm': 'application/wasm', '.svg': 'image/svg+xml', '.png': 'image/png', '.ttf': 'font/ttf', '.txt': 'text/plain', '.dat': 'application/octet-stream' };

const server = createServer(async (req, res) => {
  if (process.env.SMOKE_DEBUG) console.log(`GET ${req.url}`);
  const path = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^(\.\.[/\\])+/, '');
  const file = join(root, path.endsWith('/') ? path + 'index.html' : path);
  try {
    const body = await readFile(file);
    res.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream' }).end(body);
  } catch { console.log(`404 ${req.url}`); res.writeHead(404).end(); }
}).listen(0);
const url = `http://127.0.0.1:${server.address().port}/`;

const sleep = ms => new Promise(r => setTimeout(r, ms));
const profile = await mkdtemp(join(tmpdir(), 'maui-web-smoke-'));
const port = 9300 + Math.floor(Math.random() * 500);
const chrome = spawn(chromePath, ['--headless=new', '--disable-gpu', `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, 'about:blank'], { stdio: 'ignore' });

let failures = 0;
const errors = [];
try {
  let targets;
  for (let i = 0; i < 100 && !targets; i++) { try { targets = await (await fetch(`http://127.0.0.1:${port}/json`)).json(); } catch { await sleep(100); } }
  const ws = new WebSocket(targets.find(t => t.type === 'page').webSocketDebuggerUrl);
  await new Promise(r => ws.onopen = r);
  let id = 0; const pending = new Map();
  ws.onmessage = m => {
    const d = JSON.parse(m.data);
    if (d.id) { pending.get(d.id)?.(d); pending.delete(d.id); }
    else if (d.method === 'Runtime.exceptionThrown') errors.push(d.params.exceptionDetails.exception?.description ?? d.params.exceptionDetails.text);
    else if (d.method === 'Runtime.consoleAPICalled' && d.params.type === 'error') errors.push(d.params.args.map(a => a.value).join(' '));
  };
  const send = (method, params = {}) => new Promise(r => { const i = ++id; pending.set(i, r); ws.send(JSON.stringify({ id: i, method, params })); });
  const evaluate = async expr => (await send('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true })).result?.result?.value;
  const waitFor = async (expr, timeout = 15000) => { for (const end = Date.now() + timeout; Date.now() < end; await sleep(100)) if (await evaluate(expr)) return true; return false; };
  const byText = text => `[...document.querySelectorAll('button,span')].filter(e => e.offsetParent !== null && e.textContent.trim() === ${JSON.stringify(text)}).pop()`;
  const click = async expr => {
    const pt = await evaluate(`(() => { const el = ${expr}; if (!el) return null; el.scrollIntoView({ block: 'center' }); const r = el.getBoundingClientRect(); return [r.left + r.width / 2, r.top + r.height / 2]; })()`);
    if (!pt) throw new Error(`not found: ${expr}`);
    for (const type of ['mouseMoved', 'mousePressed', 'mouseReleased'])
      await send('Input.dispatchMouseEvent', { type, x: pt[0], y: pt[1], button: 'left', clickCount: 1 });
    await sleep(350);
  };
  const labels = pattern => evaluate(`[...document.querySelectorAll('.maui-label span')].map(s => s.textContent).filter(t => ${pattern}.test(t))`);
  const check = async (name, fn) => {
    try { const ok = await fn(); console.log(`${ok ? 'PASS' : 'FAIL'} ${name}`); if (!ok) failures++; }
    catch (e) { console.log(`FAIL ${name}: ${e.message}`); failures++; }
  };
  // Wide viewports dock the flyout; narrow ones open it from the menu button first.
  const navigate = async title => {
    try {
      const menuItem = `[...document.querySelectorAll('.maui-flyout-pane .maui-label span')].find(s => s.textContent === ${JSON.stringify(title)})`;
      if (!await evaluate(`!!(${menuItem})?.offsetParent`)) await click(`document.querySelector("button[aria-label='Open navigation']")`);
      await click(menuItem);
      await sleep(500);
    }
    catch (e) { console.log(`FAIL navigate to ${title}: ${e.message}`); failures++; }
  };

  await send('Runtime.enable');
  await send('Emulation.setDeviceMetricsOverride', { width: 1100, height: 800, deviceScaleFactor: 1, mobile: false });
  await send('Page.navigate', { url });

  await check('app starts on the home page', () => waitFor(`document.querySelector('.maui-navbar .title')?.textContent === 'Overview'`));
  await check('menu lists every demo with an icon', async () => (await evaluate(`document.querySelectorAll('.maui-flyout-pane .maui-collection-item').length`)) >= 30);
  await check('registered fonts load (variable weights)', () => waitFor(`[...document.fonts].some(f => f.family === 'Bricolage' && f.status === 'loaded' && f.weight === '1 1000')`));
  await check('GraphicsView draws', () => waitFor(`(() => { const c = document.querySelector('canvas'); if (!c || !c.width) return false; const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data; for (let i = 3; i < d.length; i += 4) if (d[i]) return true; return false; })()`));
  await check('tap gesture navigates from the home page', async () => {
    await click(`[...document.querySelectorAll('.maui-label span')].find(s => s.textContent.startsWith('Grid and stack layouts'))`);
    return waitFor(`document.querySelector('.maui-navbar .title')?.textContent === 'Layouts'`, 3000);
  });

  await navigate('Controls');
  await check('button click reaches MAUI', async () => { await click(byText('Click me!')); return (await labels('/^Clicks: 1$/')).length === 1; });
  await check('entry text reaches MAUI', async () => {
    await click(`document.querySelector("input[placeholder='Type here...']")`);
    await send('Input.insertText', { text: 'Web' });
    await sleep(300);
    return (await labels('/^Echo: Web$/')).length === 1;
  });

  await navigate('SkiaSharp');
  // Counts the opaque blue-ish ink pixels of the n-th Skia canvas.
  const skiaInk = n => evaluate(`(() => { const c = document.querySelectorAll('canvas.maui-skia-canvas-view')[${n}]; if (!c || !c.width) return -1; const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data; let ink = 0; for (let i = 0; i < d.length; i += 4) if (d[i + 3] && d[i + 2] > 200 && d[i] < 100) ink++; return ink; })()`);
  await check('SKCanvasView renders through Skia', () => waitFor(`(() => { const c = document.querySelector('canvas.maui-skia-canvas-view'); if (!c || !c.width) return false; const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data; for (let i = 3; i < d.length; i += 4) if (d[i]) return true; return false; })()`));
  await check('SKCanvasView touch events reach the app', async () => {
    const before = await skiaInk(2);
    const box = await evaluate(`(() => { const c = document.querySelectorAll('canvas.maui-skia-canvas-view')[2]; c.scrollIntoView({ block: 'center' }); const r = c.getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; })()`);
    const [x, y, w, h] = box;
    await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: x + 20, y: y + h / 2 });
    await send('Input.dispatchMouseEvent', { type: 'mousePressed', x: x + 20, y: y + h / 2, button: 'left', buttons: 1, clickCount: 1 });
    for (let i = 1; i <= 10; i++)
      await send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: x + 20 + i * (w - 40) / 10, y: y + h / 2, button: 'left', buttons: 1 });
    await send('Input.dispatchMouseEvent', { type: 'mouseReleased', x: x + w - 20, y: y + h / 2, button: 'left', buttons: 0, clickCount: 1 });
    await sleep(300);
    return (await skiaInk(2)) > before + 100;
  });

  await navigate('Alerts & Dialogs');
  await check('DisplayAlert shows a dialog and returns the choice', async () => {
    await click(byText('Confirm Alert (Yes / No)'));
    const shown = await waitFor(`document.querySelector('dialog.maui-dialog')?.open`, 3000);
    await click(byText('Yes'));
    return shown && (await labels('/Confirmed = True/')).length === 1;
  });

  await navigate('Navigation');
  await check('NavigationPage push and back', async () => {
    await click(byText('Push Page (→ Depth 2)'));
    const pushed = await waitFor(`document.querySelector('.maui-navbar .title')?.textContent === 'Nav Depth 2'`, 3000);
    await click(`document.querySelector("button[aria-label='Back']:not([hidden])")`);
    return pushed && await waitFor(`document.querySelector('.maui-navbar .title')?.textContent === 'Nav Depth 1'`, 3000);
  });

  await navigate('Modal Pages');
  await check('dialog-style modal opens and dismisses from the backdrop', async () => {
    await click(byText('Dialog Modal (Default)'));
    const shown = await waitFor(`!!document.querySelector('.maui-modal-dialog')`, 3000);
    await evaluate(`(() => { const b = document.querySelector('.maui-modal-backdrop'); const r = b.getBoundingClientRect(); b.dispatchEvent(new MouseEvent('click', { bubbles: true, clientX: r.left + 4, clientY: r.top + 4 })); })()`);
    return shown && await waitFor(`!document.querySelector('.maui-modal-backdrop')`, 3000);
  });

  await navigate('ControlTemplate');
  await check('ControlTemplate and ContentPresenter render', () => waitFor(`[...document.querySelectorAll('.maui-label span')].some(s => s.textContent === 'This content is provided by the ContentView.')`, 3000));

  await navigate('Collection View');
  await check('collection item selection', async () => { await click(byText('Item 3 — 🟡 Pending review')); return (await labels('/^Selected: Item 3/')).length === 1; });

  await check('switching theme restyles MAUI views', async () => {
    const before = await evaluate(`getComputedStyle(document.querySelector('.maui-nav-content > .maui-page')).backgroundColor`);
    await click(`document.querySelector('.maui-flyout-pane .maui-switch')`);
    await sleep(400);
    const after = await evaluate(`getComputedStyle(document.querySelector('.maui-nav-content > .maui-page')).backgroundColor`);
    return before !== after && await evaluate(`!!document.documentElement.dataset.mauiTheme`);
  });

  await check('no uncaught errors', async () => { if (errors.length) console.log(errors.join('\n')); return errors.length === 0; });
} finally {
  chrome.kill();
  server.close();
}
console.log(failures ? `${failures} check(s) failed` : 'All checks passed');
process.exit(failures ? 1 : 0);
