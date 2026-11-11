import { test, expect, galleryPages, shinyPages, expectBrokenImageErrors, type App } from '../fixtures';

/** Switches the flyout menu to the Shiny Controls tab and opens one of its pages. */
async function openShiny(app: App, title: string) {
  await app.open();
  await app.page.locator('.maui-flyout-pane').getByText('Shiny Controls', { exact: true }).click();
  await app.navigate(title);
}

test.describe('Shiny Controls tab', () => {
  test('switches the menu between the MAUI and Shiny galleries', async ({ app, page }) => {
    await app.open();
    // The menu is a virtualized CollectionView: rows carry the size of the whole list.
    const first = page.locator('.maui-flyout-pane .maui-collection-item[aria-posinset="1"]:not(.maui-recycled)');
    await expect(first).toHaveAttribute('aria-setsize', String(galleryPages.length));

    await page.locator('.maui-flyout-pane').getByText('Shiny Controls', { exact: true }).click();
    await expect(first).toHaveAttribute('aria-setsize', String(shinyPages.length));
    await expect(first).toContainText(shinyPages[0]);
    await expect(page.locator('.maui-flyout-pane').getByText(`${shinyPages.length} demos`, { exact: true })).toBeVisible();

    // Switching tabs leaves the open page alone.
    await expect(app.title).toHaveText('Overview');
    await page.locator('.maui-flyout-pane').getByText('MAUI', { exact: true }).click();
    await expect(first).toHaveAttribute('aria-setsize', String(galleryPages.length));
    await expect(first).toContainText('Home');
  });

  for (const path of ['_content/Shiny.Maui.Controls.Camera.Web/shiny-camera-web.js', '_content/Shiny.Maui.Controls.MediaElement.Web/shiny-mediaelement-web.js']) {
    test(`serves ${path} as a static web asset`, async ({ request }) => {
      const response = await request.get(path);
      expect(response.status()).toBe(200);
      expect(response.headers()['content-type']).toMatch(/javascript/);
    });
  }

  // CameraView needs a camera; it gets its own tests below with Chromium's fake device.
  for (const title of shinyPages.filter(t => t !== 'CameraView')) {
    test(`Shiny page renders: ${title}`, async ({ app, page, appErrors }) => {
      await openShiny(app, title);
      await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeHidden();
      await expect.poll(() => page.locator('.maui-flyout-page > .maui-view:not(.maui-flyout-pane) .maui-view').evaluateAll(views =>
        views.filter(v => { const r = v.getBoundingClientRect(); return r.width > 0 && r.height > 0; }).length)).toBeGreaterThan(3);
      await expect(page.locator('.maui-unsupported').filter({ visible: true })).toHaveCount(0);

      // The Avatar page loads https://example.invalid/missing.png on purpose to show the initials
      // fallback; WebKit logs that failed load. Drop only those two messages, keep everything else.
      if (title === 'Avatar') {
        const expected = [/hostname could not be found/i, /example\.invalid\/missing\.png/];
        for (let i = appErrors.length - 1; i >= 0; i--)
          if (expected.some(r => r.test(appErrors[i]))) appErrors.splice(i, 1);
      }
      // Image Viewer loads https://example.com/does-not-exist.png on purpose to show its error
      // artwork; WebKit may log the failed (CORS) fetch, even after the test body ends.
      if (title === 'Image Viewer')
        expectBrokenImageErrors();
    });
  }

  test('MediaElement mounts its video element in the surface', async ({ app, page }) => {
    await openShiny(app, 'MediaElement');
    const video = page.locator('.shiny-media-surface video');
    await expect(video).toHaveCount(1);
    await expect.poll(() => video.evaluate(v => (v as HTMLVideoElement).currentSrc || (v as HTMLVideoElement).src)).toMatch(/flower\.mp4$/);
  });
});

test.describe('Shiny VideoText', () => {
  test('masks a looping video with the text', async ({ app, page }) => {
    await openShiny(app, 'VideoText');
    const control = page.locator('.shiny-video-text').first();
    await expect(control).toBeVisible();
    await expect(control.locator('video')).toHaveJSProperty('loop', true);
    await expect.poll(() => control.locator('video').evaluate(v => (v as HTMLVideoElement).src)).toMatch(/flower\.mp4$/);
    // The default black overlay is inline SVG with the text knocked out of it.
    await expect(control.locator('.shiny-video-text-overlay svg mask text').first()).toBeAttached();
    // The fallback label net10.0 VideoText adds is not rendered.
    await expect(control.locator('.maui-label')).toHaveCount(0);
  });
});

test.describe('Shiny CameraView (fake camera)', () => {
  // The chromium project launches with a fake capture device (playwright.config.ts).
  test.skip(({ browserName }) => browserName !== 'chromium', 'Only Chromium has a fake capture device.');
  test.use({ permissions: ['camera', 'microphone'] });

  test('streams the camera and captures a photo', async ({ app, page }) => {
    await openShiny(app, 'CameraView');
    const video = page.locator('.shiny-camera-view video');
    await expect.poll(() => video.evaluate(v => (v as HTMLVideoElement).videoWidth), { timeout: 20_000 }).toBeGreaterThan(0);

    await app.button('Take photo').click();
    await expect(app.label(/^Photo: \d+×\d+, \d+ KB JPEG$/)).toBeVisible();
    await expect.poll(() => page.locator('img.maui-image').evaluateAll(imgs =>
      imgs.filter(i => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0).length)).toBeGreaterThan(0);
  });

  test('records a clip and plays it back in a MediaElement', async ({ app, page }) => {
    await openShiny(app, 'CameraView');
    const video = page.locator('.shiny-camera-view video');
    await expect.poll(() => video.evaluate(v => (v as HTMLVideoElement).videoWidth), { timeout: 20_000 }).toBeGreaterThan(0);

    await app.button('Start recording').click();
    await expect(app.label('Recording…')).toBeVisible();
    await page.waitForTimeout(1500);
    await app.button('Stop recording').click();
    await expect(app.label(/^Recorded \d+\.\ds to /)).toBeVisible();
    await expect.poll(() => page.locator('.shiny-media-surface video').evaluateAll(vs =>
      vs.some(v => (v as HTMLVideoElement).src.startsWith('blob:')))).toBe(true);
  });

  test('releases the camera when leaving the page', async ({ app, page }) => {
    await openShiny(app, 'CameraView');
    const video = page.locator('.shiny-camera-view video');
    await expect.poll(() => video.evaluate(v => (v as HTMLVideoElement).videoWidth), { timeout: 20_000 }).toBeGreaterThan(0);
    const preview = await video.elementHandle();

    await app.navigate('Pills');
    await expect.poll(() => preview!.evaluate(v => (v as HTMLVideoElement).srcObject === null)).toBe(true);
  });
});
