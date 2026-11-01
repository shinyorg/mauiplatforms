// Regenerates the README screenshots in docs/web/images from the published sample:
//   npm run publish-sample && npm run screenshots
import { chromium } from '@playwright/test';
import { spawn } from 'node:child_process';

const port = 5195;
const out = '../../docs/web/images';
const server = spawn('node', ['server.mjs', process.env.SAMPLE_WWWROOT ?? '../../artifacts/SampleWeb/wwwroot', String(port)], { stdio: 'ignore' });
await new Promise(r => setTimeout(r, 500));
const browser = await chromium.launch();

async function session({ dark = false, phone = false } = {}) {
  const ctx = await browser.newContext(phone
    ? { viewport: { width: 390, height: 844 }, deviceScaleFactor: 3, isMobile: true, hasTouch: true, colorScheme: dark ? 'dark' : 'light' }
    : { viewport: { width: 1280, height: 800 }, deviceScaleFactor: 2, colorScheme: dark ? 'dark' : 'light' });
  const page = await ctx.newPage();
  await page.goto(`http://127.0.0.1:${port}/`);
  await page.locator('.maui-navbar .title', { hasText: 'Overview' }).waitFor({ timeout: 30_000 });
  await page.evaluate(() => document.fonts.ready);
  // Finite animations (page transitions, the flyout sliding) must finish; the home page's wave runs forever.
  const settle = () => page.waitForFunction(() =>
    document.getAnimations().every(a => a.playState !== 'running' || a.effect?.getTiming().iterations === Infinity));
  const go = async title => {
    await settle();
    const item = page.locator('.maui-flyout-pane').getByText(title, { exact: true });
    if (!(await item.isVisible())) { await page.getByRole('button', { name: 'Open navigation' }).click(); await settle(); }
    await item.click();
    await page.waitForTimeout(900);
    await settle();
  };
  const shot = async name => { await page.waitForTimeout(400); await page.screenshot({ path: `${out}/${name}.png` }); console.log(`${out}/${name}.png`); };
  return { page, ctx, go, shot, settle };
}

try {
  {
    const { page, ctx, go, shot } = await session();
    await page.waitForTimeout(1200); await shot('home');
    await go('Controls'); await shot('controls');
    await go('Collection View'); await shot('collectionview');
    await go('Graphics (Canvas)'); await shot('graphics');
    await go('Shapes'); await shot('shapes');
    await go('Alerts & Dialogs');
    await page.getByRole('button', { name: 'Confirm Alert (Yes / No)' }).click();
    await page.waitForTimeout(500); await shot('dialog');
    await page.keyboard.press('Escape');
    await go('Location, Sensors & Speech'); await shot('essentials');
    await ctx.close();
  }
  {
    const { page, ctx, go, shot } = await session({ dark: true });
    await page.waitForTimeout(1200); await shot('home-dark');
    await go('Pickers & Search'); await shot('pickers-dark');
    await ctx.close();
  }
  {
    const { page, ctx, shot, settle } = await session({ phone: true });
    await settle(); await page.waitForTimeout(800); await shot('phone-home');
    await page.getByRole('button', { name: 'Open navigation' }).click(); await settle(); await shot('phone-menu');
    await page.locator('.maui-flyout-pane').getByText('TabbedPage', { exact: true }).click();
    await page.waitForTimeout(900); await settle(); await shot('phone-tabs');
    await ctx.close();
  }
} finally {
  await browser.close();
  server.kill();
}
