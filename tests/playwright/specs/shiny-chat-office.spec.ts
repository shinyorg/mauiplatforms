import { test, expect, type App } from '../fixtures';

// The Shiny ChatView, and the Shiny Office viewers drawn by Skia through Platform.Maui.Web.SkiaSharp.

async function openShiny(app: App, title: string) {
  await app.open();
  await app.page.locator('.maui-flyout-pane').getByText('Shiny Controls', { exact: true }).click();
  await app.navigate(title);
}

/** True once a canvas has painted at least one non-transparent pixel. */
const hasPixels = (canvas: HTMLCanvasElement) => {
  if (!canvas.width || !canvas.height) return false;
  const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
  for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
  return false;
};

test.describe('Shiny ChatView', () => {
  test('shows the conversation with every bubble tall enough for its text', async ({ app, page }) => {
    await openShiny(app, 'ChatView');
    const content = app.content;
    await expect(content.getByText('Yes! The TableView is really nice.', { exact: true })).toBeVisible();
    // Regression: a Border with MaximumWidthRequest measured its Label as one long line, then
    // arranged it narrower, so the label wrapped to two lines inside a one-line bubble that clipped
    // the second. Every label must end inside the nearest view that clips it.
    await expect(content.locator('.maui-label').filter({ hasText: 'The scheduler component is my favorite so far.' })).toBeVisible();
    const clipped = await content.locator('.maui-label').filter({ visible: true }).evaluateAll(labels =>
      labels.filter(l => {
        const clip = l.parentElement?.closest('.maui-clip');
        return clip && l.getBoundingClientRect().bottom > clip.getBoundingClientRect().bottom + 1;
      }).map(l => l.textContent));
    expect(clipped).toEqual([]);
  });

  test('sends a message', async ({ app, page }) => {
    await openShiny(app, 'ChatView');
    const input = app.content.locator('textarea').filter({ visible: true }).first();
    await input.fill('Hello from Playwright');
    await app.button('Send').click();
    await expect(app.content.getByText('Hello from Playwright', { exact: true })).toBeVisible();
    await expect(input).toHaveValue('');
  });
});

test.describe('Shiny Office viewers (SkiaSharp)', () => {
  test('serves the bundled Office fonts', async ({ request }) => {
    for (const file of ['Carlito-Regular.ttf', 'Caladea-Regular.ttf']) {
      const response = await request.get(`OfficeFonts/${file}`);
      expect(response.status(), file).toBe(200);
    }
  });

  for (const title of ['Spreadsheet', 'Document Viewer', 'Slide Viewer']) {
    test(`${title} draws on a Skia canvas`, async ({ app, page }) => {
      await openShiny(app, title);
      const canvas = app.content.locator('canvas.maui-skia-canvas-view').first();
      await expect(canvas).toBeVisible();
      await expect.poll(() => canvas.evaluate(hasPixels)).toBe(true);
    });
  }

  test('Document Viewer zooms', async ({ app }) => {
    await openShiny(app, 'Document Viewer');
    await expect(app.label('100 %')).toBeVisible();
    await app.button('+').click();
    await expect(app.label('110 %')).toBeVisible();
  });

  test('Slide Viewer pages through the deck', async ({ app }) => {
    await openShiny(app, 'Slide Viewer');
    await expect(app.label('1 / 3')).toBeVisible();
    await app.button('›').click();
    await expect(app.label('2 / 3')).toBeVisible();
  });

  test('Spreadsheet selects the cell under a click', async ({ app, page }) => {
    await openShiny(app, 'Spreadsheet');
    const canvas = app.content.locator('canvas.maui-skia-canvas-view').first();
    await expect.poll(() => canvas.evaluate(hasPixels)).toBe(true);
    // Let the fonts arrive and the grid re-flow first, so the only change left is the click's.
    const frame = () => canvas.evaluate(c => (c as HTMLCanvasElement).toDataURL());
    let last = '';
    await expect.poll(async () => { const now = await frame(); const stable = now === last; last = now; return stable; }, { intervals: [500] }).toBe(true);

    // Well inside the grid, away from the A1 selection it starts with.
    const box = (await canvas.boundingBox())!;
    await page.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.6);
    await expect.poll(frame).not.toBe(last);
  });
});
