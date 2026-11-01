import { test, expect } from '../fixtures';

const pageBackground = (page: import('@playwright/test').Page) =>
  page.locator('.maui-nav-content > .maui-page').first().evaluate(e => getComputedStyle(e).backgroundColor);

/** Relative luminance of an rgb()/rgba() string, 0 (black) to 1 (white). */
const luminance = (rgb: string) => {
  const [r, g, b] = rgb.match(/[\d.]+/g)!.slice(0, 3).map(Number).map(v => v / 255);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};

test('follows the system color scheme', async ({ app, page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await app.open();
  expect(luminance(await pageBackground(page))).toBeGreaterThan(0.5);
  await page.emulateMedia({ colorScheme: 'dark' });
  await expect.poll(async () => luminance(await pageBackground(page))).toBeLessThan(0.5);
});

test('UserAppTheme overrides the system scheme', async ({ app, page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await app.open();
  await app.navigate('Theme');
  await app.button('🌙 Force Dark').click();
  await expect(page.locator('html')).toHaveAttribute('data-maui-theme', 'dark');
  await expect(app.label(/Effective: Dark/)).toBeVisible();
  await app.button('🖥️ Follow System').click();
  await expect(app.label(/Effective: Light/)).toBeVisible();
});

test('the flyout dark-mode switch restyles MAUI views', async ({ app, page }) => {
  await page.emulateMedia({ colorScheme: 'light' });
  await app.open();
  const before = await pageBackground(page);
  await page.locator('.maui-flyout-pane').getByRole('switch').click();
  await expect.poll(() => pageBackground(page)).not.toBe(before);
});
