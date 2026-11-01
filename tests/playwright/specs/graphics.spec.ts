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
