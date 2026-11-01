import { test, expect } from '../fixtures';

test.describe('CollectionView', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
    await app.navigate('Collection View');
  });

  test('selection raises SelectionChanged', async ({ app }) => {
    await app.button('Item 3 — 🟡 Pending review').click();
    await expect(app.label(/^Selected: Item 3/)).toBeVisible();
  });

  test('filtering rebuilds the list', async ({ app, page }) => {
    await expect(app.label('Showing 50 items')).toBeVisible();
    await page.getByPlaceholder('Filter items...').fill('Item 4');
    await expect(app.label('Showing 11 items')).toBeVisible();
  });

  // CollectionViewHandler realizes every item (no virtualization yet), and 10,000 templated rows
  // exhaust the renderer's memory: the tab crashes. Remove fixme once the handler virtualizes.
  test.fixme('10,000 items are virtualized and ScrollTo reaches the end', async ({ app, page }) => {
    await page.locator('select.maui-picker').first().selectOption('10K Virtual');
    await expect(app.label(/^10,000 items generated/)).toBeVisible();
    // Only the rows near the viewport exist in the DOM.
    await expect.poll(() => page.locator('.maui-collection-item').count()).toBeLessThan(200);
    await app.button('Jump to #10000').click();
    await expect(app.label('Position: #10000')).toBeVisible();
    await expect(app.label('#10000')).toBeInViewport();
  });
});

test.describe('CarouselView & IndicatorView', () => {
  test('indicator dots move the carousel', async ({ app, page }) => {
    await app.open();
    await app.navigate('Carousel & Indicators');
    const dots = page.locator('.maui-indicator-view button').filter({ visible: true });
    await expect.poll(() => dots.count()).toBeGreaterThan(1);
    await dots.nth(2).click();
    await expect(dots.nth(2)).toHaveAttribute('aria-selected', 'true');
  });
});
