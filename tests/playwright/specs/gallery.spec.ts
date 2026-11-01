import { test, expect, galleryPages } from '../fixtures';

// Opens every page in the gallery: each must render content and log nothing (the fixture fails a
// test on any console error, which is where a missing handler or a mapper exception shows up).
for (const title of galleryPages) {
  test(`gallery page renders: ${title}`, async ({ app, page }) => {
    await app.open();
    await app.navigate(title);
    // Every page but Home replaces the detail; most show their own title in the navigation bar.
    if (title !== 'Home')
      await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeHidden();
    await expect.poll(() => page.locator('.maui-flyout-page > .maui-view:not(.maui-flyout-pane) .maui-view').evaluateAll(views =>
      views.filter(v => { const r = v.getBoundingClientRect(); return r.width > 0 && r.height > 0; }).length)).toBeGreaterThan(3);
    await expect(page.locator('.maui-unsupported').filter({ visible: true })).toHaveCount(0);
  });
}
