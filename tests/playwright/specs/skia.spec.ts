import { test, expect } from '../fixtures';

// SkiaSharp's SKCanvasView through Platform.Maui.Web.SkiaSharp (the "SkiaSharp" gallery page):
// a static drawing, an animated canvas and a finger-paint canvas with EnableTouchEvents.

const canvases = 'canvas.maui-skia-canvas-view';

/** True once a canvas has painted at least one non-transparent pixel. */
const hasPixels = (canvas: HTMLCanvasElement) => {
  if (!canvas.width || !canvas.height) return false;
  const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
  for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
  return false;
};

/** The finger-paint canvas's blue ink pixels (the background is white and the border light gray). */
const inkPixels = (canvas: HTMLCanvasElement) => {
  const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
  let ink = 0;
  for (let i = 0; i < data.length; i += 4) if (data[i + 3] && data[i + 2] > 200 && data[i] < 100) ink++;
  return ink;
};

test.beforeEach(async ({ app, page }) => {
  await app.open();
  await app.navigate('SkiaSharp');
  await expect(page.locator(canvases)).toHaveCount(3);
});

test('every SKCanvasView renders through Skia', async ({ page }) => {
  await expect.poll(() => page.locator(canvases).evaluateAll(all => all.every(c => {
    const canvas = c as HTMLCanvasElement;
    if (!canvas.width || !canvas.height) return false;
    const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
    for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
    return false;
  }))).toBe(true);
});

test('the surface matches the canvas at the device pixel ratio', async ({ page }) => {
  const first = page.locator(canvases).first();
  await expect.poll(() => first.evaluate(hasPixels)).toBe(true);
  const { width, height, cssWidth, cssHeight, ratio } = await first.evaluate(c => {
    const canvas = c as HTMLCanvasElement;
    const box = canvas.getBoundingClientRect();
    return { width: canvas.width, height: canvas.height, cssWidth: box.width, cssHeight: box.height, ratio: window.devicePixelRatio };
  });
  expect(Math.abs(width - cssWidth * ratio)).toBeLessThanOrEqual(1);
  expect(Math.abs(height - cssHeight * ratio)).toBeLessThanOrEqual(1);
});

test('InvalidateSurface repaints the animated canvas', async ({ page }) => {
  const spinner = page.locator(canvases).nth(1);
  await spinner.scrollIntoViewIfNeeded();
  await expect.poll(() => spinner.evaluate(hasPixels)).toBe(true);
  const frame = () => spinner.evaluate(c => (c as HTMLCanvasElement).toDataURL());
  const before = await frame();
  await expect.poll(frame).not.toBe(before);
});

test('touch events draw on the finger-paint canvas, and Clear erases it', async ({ app, page }) => {
  const paint = page.locator(canvases).nth(2);
  await paint.scrollIntoViewIfNeeded();
  await expect.poll(() => paint.evaluate(hasPixels)).toBe(true);
  expect(await paint.evaluate(inkPixels)).toBe(0);

  const box = (await paint.boundingBox())!;
  const y = box.y + box.height / 2;
  await page.mouse.move(box.x + 20, y);
  await page.mouse.down();
  await page.mouse.move(box.x + box.width - 20, y, { steps: 12 });
  await page.mouse.up();
  await expect.poll(() => paint.evaluate(inkPixels)).toBeGreaterThan(100);

  await app.button('Clear').click();
  await expect.poll(() => paint.evaluate(inkPixels)).toBe(0);
});

test('a handled press stops the browser from panning the canvas', async ({ page }) => {
  // EnableTouchEvents turns off browser panning and zooming over the canvas; the others keep it.
  await expect.poll(() => page.locator(canvases).evaluateAll(all => all.map(c => (c as HTMLElement).style.touchAction)))
    .toEqual(['', '', 'none']);
});
