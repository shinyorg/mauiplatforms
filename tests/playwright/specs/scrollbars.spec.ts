import { test, expect } from '../fixtures';
import type { Locator, Page } from '@playwright/test';

/** Where a scroll view's content sits within the area its scroll container shows (clientWidth excludes a classic scrollbar). */
const fit = async (scroll: Locator, content: Locator) => scroll.evaluate((s, c) => {
  const sb = s.getBoundingClientRect();
  const cb = (c as Element).getBoundingClientRect();
  return { left: Math.round(cb.left - sb.left), right: Math.round(sb.left + s.clientWidth - cb.right), scrollbar: s.offsetWidth - s.clientWidth };
}, await content.elementHandle());

// Playwright hides scrollbars by default; without that, Chromium draws classic ones that take room.
test.use({ launchOptions: { ignoreDefaultArgs: ['--hide-scrollbars'] } });
test.skip(({ browserName }) => browserName !== 'chromium', 'Classic scrollbars are only drawn by Chromium here.');

/** The room a classic scrollbar takes in this browser: 0 where the OS draws overlay scrollbars (macOS's default without a mouse). */
const classicScrollbar = (page: Page) => page.evaluate(() => {
  const probe = document.createElement('div');
  probe.style.cssText = 'position:absolute;left:-10000px;width:100px;height:100px;overflow:scroll';
  document.body.appendChild(probe);
  const size = probe.offsetWidth - probe.clientWidth;
  probe.remove();
  return size;
});

test.beforeEach(async ({ page }) => {
  await page.goto('about:blank');
  test.skip(await classicScrollbar(page) === 0, 'This system draws overlay scrollbars, which take no room.');
});

test('ScrollView content stays clear of the scrollbar', async ({ app, page }) => {
  await app.open();
  await app.navigate('Scrolling');
  const scroll = page.locator('[data-automation-id="paddedScroll"]');
  const content = page.locator('[data-automation-id="paddedContent"]');
  await expect(content).toBeVisible();
  const result = await fit(scroll, content);
  expect(result.scrollbar).toBeGreaterThan(0);
  await expect.poll(() => fit(scroll, content)).toMatchObject({ left: 20, right: 20 });
});

test('CollectionView items stay clear of the scrollbar', async ({ app, page }) => {
  await app.open();
  await app.navigate('Collection View');
  await page.locator('select.maui-picker').first().selectOption('10K Virtual');
  const list = app.content.locator('.maui-collection').first();
  await expect(list.locator('.maui-collection-item').first()).toBeVisible();
  const overhang = () => list.evaluate(l => {
    const visibleRight = l.getBoundingClientRect().left + l.clientWidth;
    const items = [...l.querySelectorAll(':scope > .maui-collection-item, .maui-collection-item')];
    return { scrollbar: l.offsetWidth - l.clientWidth, overhang: Math.max(0, ...items.map(i => Math.round(i.getBoundingClientRect().right - visibleRight))) };
  });
  await expect.poll(overhang).toMatchObject({ overhang: 0 });
  expect((await overhang()).scrollbar).toBeGreaterThan(0);
});
