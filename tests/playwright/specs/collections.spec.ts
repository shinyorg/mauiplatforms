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

  test('10,000 items are virtualized and ScrollTo reaches either end', async ({ app, page }) => {
    await page.locator('select.maui-picker').first().selectOption('10K Virtual');
    await expect(app.label(/^10,000 items generated/)).toBeVisible();
    await expect(app.label('Item #1')).toBeInViewport();
    // Only the rows near the viewport exist in the DOM.
    await expect.poll(() => app.content.locator('.maui-collection-item').count()).toBeLessThan(200);
    const list = app.content.locator('.maui-collection');
    await expect(list.locator('[aria-posinset="1"]')).toHaveAttribute('aria-setsize', '10000');

    await app.button('Jump to #10000').click();
    await expect(app.label('Position: #10000')).toBeVisible();
    await expect(app.label('Item #10000')).toBeInViewport();
    await expect(app.label('Total: 10,000 items')).toBeAttached();
    await expect.poll(() => app.content.locator('.maui-collection-item').count()).toBeLessThan(200);

    // A recycled row still selects the item it now shows.
    await app.label('Item #9990').click();
    await expect(app.label('Selected: Item #9990')).toBeVisible();

    await app.button('Jump to #1').click();
    await expect(app.label('Item #1')).toBeInViewport();
  });

  test('scrolling through 10,000 items keeps rows contiguous', async ({ app, page }) => {
    await page.locator('select.maui-picker').first().selectOption('10K Virtual');
    await expect(app.label('Item #1')).toBeInViewport();
    const list = app.content.locator('.maui-collection');
    for (const fraction of [0.25, 0.5, 0.75, 1]) {
      await list.evaluate((el, f) => { el.scrollTop = (el.scrollHeight - el.clientHeight) * f; }, fraction);
      // Whatever is on screen is a run of consecutive items with no gaps or overlaps.
      await expect.poll(() => list.evaluate(el => {
        const box = el.getBoundingClientRect();
        const rows = [...el.querySelectorAll(':scope > .maui-collection-item')]
          .map(r => ({ r: r.getBoundingClientRect(), i: Number(r.getAttribute('aria-posinset')) }))
          .filter(({ r }) => r.height > 0 && r.bottom > box.top && r.top < box.bottom)
          .sort((a, b) => a.i - b.i);
        if (rows.length < 5) return 'too few rows: ' + rows.length;
        for (let k = 1; k < rows.length; k++) {
          if (rows[k].i !== rows[k - 1].i + 1) return `gap between #${rows[k - 1].i} and #${rows[k].i}`;
          if (Math.abs(rows[k].r.top - rows[k - 1].r.bottom) > 1.5) return `rows #${rows[k - 1].i}/#${rows[k].i} don't meet`;
        }
        return 'ok';
      })).toBe('ok');
    }
  });

  test('grouped lists show group headers and the footer', async ({ app, page }) => {
    await page.locator('select.maui-picker').first().selectOption('Grouped');
    await expect(app.label('🐱 Cats')).toBeVisible();
    await app.label('Siamese').click();
    await expect(app.label('Selected: Siamese')).toBeVisible();
    const list = app.content.locator('.maui-collection');
    await list.evaluate(el => { el.scrollTop = el.scrollHeight; });
    await expect(app.label('🐠 Fish')).toBeAttached();
    await expect(app.label('17 animals in 4 groups')).toBeInViewport();
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

  test('swiping updates Position, and Loop wraps past either end', async ({ app, page }) => {
    await app.open();
    await app.navigate('Carousel & Indicators');
    const carousel = app.content.locator('.maui-carousel');
    const swipe = (pages: number) => carousel.evaluate((el, n) => el.scrollBy({ left: el.clientWidth * n, behavior: 'instant' }), pages);
    // The text of the slide at the carousel's centre (Loop realizes several copies of each slide).
    const shown = () => carousel.evaluate(el => {
      const r = el.getBoundingClientRect();
      return document.elementFromPoint(r.left + r.width / 2, r.top + r.height / 2)?.closest('.maui-collection-item')?.textContent ?? '';
    });

    await swipe(1);
    await expect(app.label('Position: 1')).toBeVisible();
    await swipe(-1);
    await expect(app.label('Position: 0')).toBeVisible();

    await app.content.getByRole('switch').click();
    // Looping starts from the middle copy of the slides.
    await expect.poll(() => carousel.evaluate(el => el.scrollLeft / el.clientWidth)).toBeGreaterThan(4);
    await swipe(-1);
    await expect(app.label('Position: 3')).toBeVisible();
    await expect.poll(shown).toContain('🚀 Deploy');
    await app.button('Next ▶').click();
    await expect(app.label('Position: 0')).toBeVisible();
    await expect.poll(shown).toContain('🌄 Welcome');
    // Five copies of four slides, but only the pages within two of the current one have views.
    await expect.poll(() => carousel.locator('.maui-collection-item:not(.maui-recycled)').count()).toBeLessThanOrEqual(6);
  });
});
