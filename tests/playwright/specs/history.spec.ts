import { test, expect, type App } from '../fixtures';
import type { Page } from '@playwright/test';

// Browser Back/Forward drive MAUI navigation (Platform/WebHistory.cs): one history entry per step
// Back can take inside the app, and Back on the root page leaves it.

/** The browser's Back button. Same-document history navigations don't fire a load event. */
async function browserBack(page: Page) {
  await page.goBack({ waitUntil: 'commit' });
}

/** The depth the current history entry stands for. */
function entryDepth(page: Page) {
  return page.evaluate(() => history.state?.mauiDepth ?? null);
}

/** Opens the app with a page before it in history, so leaving the app is observable. */
async function openAfterAnotherPage(app: App, page: Page) {
  await page.goto('about:blank');
  await app.open();
}

test.describe('browser history', () => {
  test('Back pops a NavigationPage, one page at a time', async ({ app, page }) => {
    await app.open();
    await app.navigate('Navigation');
    await expect(app.title).toHaveText('Nav Depth 1');
    await app.button('Push Page (→ Depth 2)').click();
    await expect(app.title).toHaveText('Nav Depth 2');
    await app.button('Push Page (→ Depth 3)').click();
    await expect(app.title).toHaveText('Nav Depth 3');
    await expect.poll(() => entryDepth(page)).toBe(2);

    await browserBack(page);
    await expect(app.title).toHaveText('Nav Depth 2');
    await browserBack(page);
    await expect(app.title).toHaveText('Nav Depth 1');
    await expect.poll(() => entryDepth(page)).toBe(0);
  });

  test('two quick Backs take two steps', async ({ app, page }) => {
    await app.open();
    await app.navigate('Navigation');
    await app.button('Push Page (→ Depth 2)').click();
    await app.button('Push Page (→ Depth 3)').click();
    await expect(app.title).toHaveText('Nav Depth 3');
    await expect.poll(() => entryDepth(page)).toBe(2);

    await browserBack(page);
    await browserBack(page);
    await expect(app.title).toHaveText('Nav Depth 1');
    await expect.poll(() => entryDepth(page)).toBe(0);
  });

  test('in-app back removes the history entry', async ({ app, page }) => {
    await app.open();
    await app.navigate('Navigation');
    await app.button('Push Page (→ Depth 2)').click();
    await expect.poll(() => entryDepth(page)).toBe(1);
    await app.back.click();
    await expect(app.title).toHaveText('Nav Depth 1');
    await expect.poll(() => entryDepth(page)).toBe(0);
  });

  test('Back closes a modal page', async ({ app, page }) => {
    await app.open();
    await app.navigate('Modal Pages');
    await app.button('Full-Window Modal').click();
    await expect(page.locator('.maui-modal-layer')).toBeVisible();
    await expect.poll(() => entryDepth(page)).toBe(1);

    await browserBack(page);
    await expect(page.locator('.maui-modal-layer')).toHaveCount(0);
    await expect(app.label(/was dismissed/)).toBeVisible();
  });

  test('Back closes a popover flyout', async ({ app, page }) => {
    await page.setViewportSize({ width: 600, height: 860 });
    await app.open();
    const menu = page.getByRole('button', { name: 'Open navigation' });
    const backdrop = page.locator('.maui-flyout-backdrop').filter({ visible: true });
    await menu.click();
    await expect(backdrop).toBeVisible();
    await expect.poll(() => entryDepth(page)).toBe(1);

    await browserBack(page);
    await expect(backdrop).toBeHidden();
    await expect(app.title).toHaveText('Overview');
  });

  test('Shell: Back closes the flyout, then pops', async ({ app, page }) => {
    await app.open();
    await app.navigate('Shell Navigation');
    await app.button('Launch Shell App').click();
    await expect(app.label('🏠 Welcome Home')).toBeVisible();
    // The demo Shell starts with its flyout presented.
    const backdrop = page.locator('.maui-flyout-backdrop').filter({ visible: true });
    await expect(backdrop).toBeVisible();
    await expect.poll(() => entryDepth(page)).toBe(1);
    await browserBack(page);
    await expect(backdrop).toBeHidden();
    await expect(app.label('🏠 Welcome Home')).toBeVisible();

    await app.button('GoToAsync → Details?id=42&name=Widget').click();
    await expect(app.label('ID: 42')).toBeVisible();
    await expect.poll(() => entryDepth(page)).toBe(1);
    await browserBack(page);
    await expect(app.label('🏠 Welcome Home')).toBeVisible();
  });

  test('Back on the root page leaves the app', async ({ app, page }) => {
    await openAfterAnotherPage(app, page);
    await app.navigate('Navigation');
    await app.button('Push Page (→ Depth 2)').click();
    await expect(app.title).toHaveText('Nav Depth 2');
    await expect.poll(() => entryDepth(page)).toBe(1);

    await browserBack(page);
    await expect(app.title).toHaveText('Nav Depth 1');
    await expect.poll(() => entryDepth(page)).toBe(0);
    await browserBack(page);
    await expect(page).toHaveURL('about:blank');
  });
});
