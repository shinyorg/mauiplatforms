// Serves a published wwwroot for the tests: the right MIME types for wasm/fonts, precompressed
// files left alone, and the whole app mounted a second time under /sub/ so hosting below the site
// root can be tested (index.html gets a matching <base href>, as a real subpath deployment would).
//
//   node server.mjs <wwwroot> <port>
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize, resolve } from 'node:path';

const root = resolve(process.argv[2] ?? '../../artifacts/SampleWeb/wwwroot');
const port = Number(process.argv[3] ?? 5199);
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css',
  '.json': 'application/json', '.wasm': 'application/wasm', '.svg': 'image/svg+xml', '.png': 'image/png',
  '.jpg': 'image/jpeg', '.gif': 'image/gif', '.webp': 'image/webp', '.ico': 'image/x-icon', '.ttf': 'font/ttf',
  '.otf': 'font/otf', '.woff': 'font/woff', '.woff2': 'font/woff2', '.txt': 'text/plain', '.dat': 'application/octet-stream',
};
export const subpath = '/sub/';

createServer(async (req, res) => {
  let path = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  const underSubpath = path.startsWith(subpath);
  if (underSubpath) path = '/' + path.slice(subpath.length);
  path = normalize(path).replace(/^(\.\.[/\\])+/, '');
  const file = join(root, path.endsWith('/') ? path + 'index.html' : path);
  if (!file.startsWith(root)) return res.writeHead(403).end();
  try {
    let body = await readFile(file);
    if (underSubpath && file.endsWith('index.html'))
      body = Buffer.from(body.toString().replace('<head>', `<head><base href="${subpath}">`));
    res.writeHead(200, { 'content-type': types[extname(file).toLowerCase()] ?? 'application/octet-stream', 'cache-control': 'no-store' }).end(body);
  } catch {
    res.writeHead(404).end();
  }
}).listen(port, '127.0.0.1', () => console.log(`serving ${root} on http://127.0.0.1:${port}/`));
