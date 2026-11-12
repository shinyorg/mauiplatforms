import { test, expect, galleryPages } from '../fixtures';

test.describe('startup', () => {
  test('boots into the home page and removes the loading screen', async ({ app, page }) => {
    await app.open();
    await expect(page).toHaveTitle(/MAUI/);
    await expect(page.locator('#maui-loading')).toBeHidden();
    await expect(page.locator('.maui-window')).toHaveCount(1);
  });

  test('lists every gallery page in the flyout menu', async ({ app, page }) => {
    await app.open();
    // The menu is a virtualized CollectionView, so rows past the viewport aren't in the DOM; each
    // row carries the size of the whole list instead.
    const first = page.locator('.maui-flyout-pane .maui-collection-item[aria-posinset="1"]:not(.maui-recycled)');
    await expect(first).toHaveAttribute('aria-setsize', String(galleryPages.length));
  });

  test('loads registered fonts, including variable weights', async ({ app, page }) => {
    await app.open();
    await expect.poll(() => page.evaluate(() =>
      [...document.fonts].some(f => f.family === 'Bricolage' && f.status === 'loaded' && f.weight === '1 1000'))).toBe(true);
  });

  test('runs when hosted below the site root', async ({ app, page }) => {
    await app.open('/sub/');
    await expect(page).toHaveURL(/\/sub\/$/);
    await app.navigate('Controls');
    await expect(app.title).toHaveText('Controls');
  });
});

test.describe('MAUI resources are served as static web assets', () => {
  const assets: [string, RegExp][] = [
    ['dotnet_bot.png', /^image\/png/],             // MauiImage (raster)
    ['dotnet_bot.svg', /^image\/svg\+xml/],        // MauiImage (vector, kept as SVG)
    ['BricolageGrotesque.ttf', /^font\/ttf/],      // MauiFont
    ['about.txt', /^text\/plain/],                 // MauiAsset
    ['favicon.svg', /^image\/svg\+xml/],           // MauiIcon
    ['_content/Platform.Maui.Web/maui-web.js', /javascript/],
    ['_content/Platform.Maui.Web/maui-web.css', /^text\/css/],
  ];
  for (const [path, type] of assets) {
    test(path, async ({ request }) => {
      const response = await request.get(path);
      expect(response.status()).toBe(200);
      expect(response.headers()['content-type']).toMatch(type);
      expect((await response.body()).length).toBeGreaterThan(0);
    });
  }

  test('images decode and render in the app', async ({ app, page }) => {
    await app.open();
    await app.navigate('Image / Font / Icon');
    await expect(page.locator('.maui-image').first()).toBeVisible();
    await expect.poll(() => page.locator('img.maui-image').evaluateAll(imgs =>
      imgs.filter(i => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0).length)).toBeGreaterThan(0);
  });

  test('a linked MauiImage is served under its Link name', async ({ app, page }) => {
    await app.open();
    await app.navigate('Image Showcase');
    const linked = page.locator('[data-automation-id="linkedImage"]');
    await expect.poll(() => linked.evaluate(i => (i as HTMLImageElement).complete && (i as HTMLImageElement).naturalWidth > 0)).toBe(true);
    await expect(linked).toHaveAttribute('src', /app_logo\.svg$/);
  });

  test('an image that fails to load shows nothing', async ({ app, page }) => {
    await app.open();
    await app.navigate('Image Showcase');
    const missing = page.locator('[data-automation-id="missingImage"]');
    await expect(missing).toHaveClass(/maui-image-failed/);
    await expect(missing).toHaveCSS('visibility', 'hidden');
  });
});
