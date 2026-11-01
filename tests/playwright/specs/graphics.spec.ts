import { test, expect } from '../fixtures';

/** True once a canvas has painted at least one non-transparent pixel. */
const hasPixels = (canvas: HTMLCanvasElement) => {
  if (!canvas.width || !canvas.height) return false;
  const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
  for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
  return false;
};

test('GraphicsView draws on the home page', async ({ app, page }) => {
  await app.open();
  await expect.poll(() => page.locator('canvas.maui-graphics-view').first().evaluate(hasPixels)).toBe(true);
});

test('Graphics (Canvas) page draws every canvas', async ({ app, page }) => {
  await app.open();
  await app.navigate('Graphics (Canvas)');
  const canvases = page.locator('canvas.maui-graphics-view').filter({ visible: true });
  await expect.poll(() => canvases.count()).toBeGreaterThan(0);
  await expect.poll(() => canvases.evaluateAll(all => all.every(c => {
    const canvas = c as HTMLCanvasElement;
    if (!canvas.width || !canvas.height) return false;
    const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
    for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
    return false;
  }))).toBe(true);
});

test('shapes render as SVG geometry', async ({ app, page }) => {
  await app.open();
  await app.navigate('Shapes');
  const paths = page.locator('.maui-shape svg path[d]:not([d=""])').filter({ visible: true });
  await expect.poll(() => paths.count()).toBeGreaterThan(5);
});

test('transforms apply CSS transforms', async ({ app, page }) => {
  await app.open();
  await app.navigate('Transforms & Effects');
  await expect.poll(() => page.locator('.maui-nav-content .maui-view').evaluateAll(views =>
    views.filter(v => (v as HTMLElement).style.transform && (v as HTMLElement).style.transform !== 'none').length)).toBeGreaterThan(0);
});

test('FontImageSource glyphs wider than 1em are not clipped', async ({ app, page }) => {
  await app.open();
  const result = await page.evaluate(async () => {
    const { renderGlyph } = await import(new URL('_content/Platform.Maui.Web/maui-web.js', document.baseURI).href);
    const size = 20, scale = window.devicePixelRatio || 1;
    const decode = async (url: string) => {
      const img = new Image();
      img.src = url;
      await img.decode();
      return [img.naturalWidth, img.naturalHeight];
    };
    const ctx = document.createElement('canvas').getContext('2d')!;
    ctx.font = `${size * scale}px sans-serif`;
    const wide = ctx.measureText('WW');
    return {
      square: Math.ceil(size * scale),
      inkWidth: wide.actualBoundingBoxLeft + wide.actualBoundingBoxRight,
      wide: await decode(await renderGlyph('WW', 'sans-serif', size, '#000')),
      narrow: await decode(await renderGlyph('i', 'sans-serif', size, '#000')),
    };
  });
  // A glyph wider than the requested size gets a wider bitmap instead of being cut off...
  expect(result.inkWidth).toBeGreaterThan(result.square);
  expect(result.wide[0]).toBeGreaterThanOrEqual(Math.floor(result.inkWidth));
  // ...while one that fits still renders into the size x size square.
  expect(result.narrow).toEqual([result.square, result.square]);
});
