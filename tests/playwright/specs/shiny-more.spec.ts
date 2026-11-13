import { test, expect, expectBrokenImageErrors, type App } from '../fixtures';

// Shiny controls with no web handler of their own: floating panels, the signature pad, the
// scheduler, Gantt, the diagram and Mermaid views (all GraphicsView), and the image viewer.

async function openShiny(app: App, title: string) {
  await app.open();
  await app.page.locator('.maui-flyout-pane').getByText('Shiny Controls', { exact: true }).click();
  await app.navigate(title);
}

const painted = (canvas: Element) => {
  const c = canvas as HTMLCanvasElement;
  if (!c.width || !c.height) return false;
  const data = c.getContext('2d')!.getImageData(0, 0, c.width, c.height).data;
  for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
  return false;
};

test.describe('Shiny floating panels', () => {
  test('Dual Panels opens the top panel', async ({ app }) => {
    await openShiny(app, 'Dual Panels');
    const notice = app.label('Your PR #142 passed all checks and is ready to merge.');
    await expect(notice).toBeHidden();
    await app.button('Open Top').click();
    await expect(notice).toBeVisible();
    await app.button('Close Top').click();
    await expect(notice).toBeHidden();
  });
});

test.describe('Shiny SignaturePad', () => {
  test('draws a signature and enables Sign', async ({ app, page }) => {
    await openShiny(app, 'Signature Pad');
    await app.button('Capture Signature').click();
    const pad = page.locator('canvas.maui-graphics-view').filter({ visible: true }).last();
    await expect(pad).toBeVisible();
    const sign = app.button('Sign');
    await expect(sign).toBeDisabled();

    const box = (await pad.boundingBox())!;
    await page.mouse.move(box.x + 30, box.y + box.height / 2);
    await page.mouse.down();
    for (let i = 1; i <= 12; i++)
      await page.mouse.move(box.x + 30 + i * (box.width - 60) / 12, box.y + box.height / 2 + (i % 2 ? 25 : -25));
    await page.mouse.up();
    await expect.poll(() => pad.evaluate(painted)).toBe(true);

    await expect(sign).toBeEnabled();
    await sign.click();
    await expect(app.label('Signature captured!')).toBeVisible();

    // The PNG is drawn through UseSkiaSharpWeb's IBitmapExportService and shown by the page's Image.
    const signature = app.content.locator('img.maui-image').filter({ visible: true });
    await expect.poll(() => signature.evaluate(i => (i as HTMLImageElement).naturalWidth)).toBeGreaterThan(0);
  });
});

test.describe('Shiny scheduler', () => {
  for (const title of ['Scheduler Calendar', 'Scheduler Agenda', 'Scheduler List']) {
    test(`${title} shows the sample events`, async ({ app }) => {
      await openShiny(app, title);
      await expect(app.content.getByText(/Team Standup|Lunch Break|Design Review/).first()).toBeVisible();
    });
  }
});

test.describe('Shiny canvas controls', () => {
  for (const title of ['Gantt', 'Diagram', 'Mermaid Flowchart', 'Mermaid Subgraphs', 'Mermaid Editor']) {
    test(`${title} paints its GraphicsView`, async ({ app }) => {
      await openShiny(app, title);
      const canvases = app.content.locator('canvas.maui-graphics-view').filter({ visible: true });
      await expect.poll(() => canvases.count()).toBeGreaterThan(0);
      await expect.poll(() => canvases.evaluateAll(all => all.some(c => {
        const canvas = c as HTMLCanvasElement;
        if (!canvas.width || !canvas.height) return false;
        const data = canvas.getContext('2d')!.getImageData(0, 0, canvas.width, canvas.height).data;
        for (let i = 3; i < data.length; i += 4) if (data[i]) return true;
        return false;
      }))).toBe(true);
    });
  }
});

test.describe('Shiny ImageViewer', () => {
  test('loads its images', async ({ app }) => {
    expectBrokenImageErrors();
    await openShiny(app, 'Image Viewer');
    // The remote images come from picsum.photos, so only the bundled one is required here.
    const images = app.content.locator('img').filter({ visible: true });
    await expect.poll(() => images.evaluateAll(all => all.filter(i => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0).length)).toBeGreaterThan(0);
  });
});

test.describe('Shiny TableView', () => {
  test('a switch cell toggles', async ({ app }) => {
    await openShiny(app, 'TableView Settings');
    const bluetooth = app.content.getByRole('switch').nth(1);
    await expect(bluetooth).not.toBeChecked();
    await bluetooth.click({ force: true });
    await expect(bluetooth).toBeChecked();
  });

  test('drag sort moves a row by its handle', async ({ app, page }) => {
    await openShiny(app, 'TableView Drag Sort');
    const first = app.label('First Item');
    const third = app.label('Third Item');
    await expect(first).toBeVisible();
    const handle = app.content.getByText('☰', { exact: true }).first();
    const from = (await handle.boundingBox())!;
    const to = (await third.boundingBox())!;
    await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2);
    await page.mouse.down();
    for (let i = 1; i <= 12; i++)
      await page.mouse.move(from.x + from.width / 2, from.y + from.height / 2 + i * (to.y + to.height - from.y) / 12);
    await page.mouse.up();
    // First Item now sits below Second Item.
    await expect.poll(async () => (await first.boundingBox())!.y > (await app.label('Second Item').boundingBox())!.y).toBe(true);
  });
});

test.describe('Shiny Overlay, Walkthrough and Keyframe', () => {
  test('Overlay shows and dismisses custom content', async ({ app }) => {
    await openShiny(app, 'Overlay');
    await app.button('Show Custom Overlay').click();
    await expect(app.label('Custom Overlay Content')).toBeVisible();
    await app.button('Dismiss').click();
    await expect(app.label('Custom Overlay Content')).toBeHidden();
  });

  test('Walkthrough steps forward', async ({ app, page }) => {
    await openShiny(app, 'Walkthrough');
    await expect(app.label('1 of 5')).toBeVisible();
    // The step card's Next is a tappable label, not a button.
    await page.locator('.maui-label').getByText('Next', { exact: true }).filter({ visible: true }).click();
    await expect(app.label('2 of 5')).toBeVisible();
  });

  test('Keyframe animates a real view', async ({ app }) => {
    await openShiny(app, 'Keyframe Animation');
    const views = app.content.locator('.maui-view');
    const transforms = () => views.evaluateAll(all => all.map(v => (v as HTMLElement).style.transform).join('|'));
    const before = await transforms();
    await expect.poll(transforms).not.toBe(before);
  });
});

test.describe('Shiny Kanban', () => {
  test('a card drags to another column', async ({ app, page }) => {
    await openShiny(app, 'Kanban');
    const card = app.label('Audit the theme tokens');
    const target = app.label('Pull to refresh');
    await expect(card).toBeVisible();
    await expect(app.button('Undo')).toBeDisabled();
    const from = (await card.boundingBox())!;
    const to = (await target.boundingBox())!;
    await page.mouse.move(from.x + 20, from.y + from.height / 2);
    await page.mouse.down();
    for (let i = 1; i <= 15; i++)
      await page.mouse.move(from.x + 20 + i * (to.x - from.x) / 15, from.y + from.height / 2 + i * (to.y + 40 - from.y) / 15);
    await page.mouse.up();
    // Moving a card is undoable.
    await expect(app.button('Undo')).toBeEnabled();
  });
});
