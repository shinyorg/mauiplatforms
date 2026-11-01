import { test, expect } from '../fixtures';

// Runs in the "mobile" project (Pixel 7 viewport, touch) and with a narrow desktop window.
test.describe('narrow screens', () => {
  test.beforeEach(async ({ page }, testInfo) => {
    if (testInfo.project.name !== 'mobile')
      await page.setViewportSize({ width: 420, height: 820 });
  });

  test('the flyout is a popover opened from the menu button', async ({ app, page }) => {
    await app.open();
    const menu = page.getByRole('button', { name: 'Open navigation' });
    await expect(menu).toBeVisible();
    await expect(page.locator('.maui-flyout-pane').getByText('Controls', { exact: true })).not.toBeInViewport();
    await app.navigate('Controls');
    await expect(app.title).toHaveText('Controls');
    // Choosing an item closes the popover again.
    await expect(page.locator('.maui-flyout-pane').getByText('Controls', { exact: true })).not.toBeInViewport();
  });

  test('content is laid out within the viewport', async ({ app, page }) => {
    await app.open();
    await app.navigate('Controls');
    const width = page.viewportSize()!.width;
    const overflow = await page.locator('.maui-nav-content .maui-button').evaluateAll((buttons, w) =>
      buttons.filter(b => { const r = b.getBoundingClientRect(); return r.width > 0 && r.right > w + 1; }).length, width);
    expect(overflow).toBe(0);
  });

  test('tabs move to the bottom', async ({ app, page }) => {
    await app.open();
    await app.navigate('TabbedPage');
    const tab = app.tab('📊 Stats');
    const box = await tab.boundingBox();
    expect(box!.y).toBeGreaterThan(page.viewportSize()!.height / 2);
  });
});
