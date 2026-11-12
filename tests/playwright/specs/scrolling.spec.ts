import { test, expect } from '../fixtures';
import type { Locator } from '@playwright/test';

/** Where a scroll view's content sits within the area its scroll container shows (clientWidth excludes a classic scrollbar). */
const fit = async (scroll: Locator, content: Locator) => scroll.evaluate((s, c) => {
  const sb = s.getBoundingClientRect();
  const cb = (c as Element).getBoundingClientRect();
  return { left: Math.round(cb.left - sb.left), right: Math.round(sb.left + s.clientWidth - cb.right), scrollbar: s.offsetWidth - s.clientWidth };
}, await content.elementHandle());

test('a padded ScrollView keeps its content inside the padding', async ({ app, page }) => {
  await app.open();
  await app.navigate('Scrolling');
  const scroll = page.locator('[data-automation-id="paddedScroll"]');
  const content = page.locator('[data-automation-id="paddedContent"]');
  await expect(content).toBeVisible();
  // Padding = 20: the content starts 20 in and ends 20 short of the visible edge, not 20 past it.
  await expect.poll(() => fit(scroll, content)).toMatchObject({ left: 20, right: 20 });
});
