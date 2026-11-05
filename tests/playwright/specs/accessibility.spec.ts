import { test, expect } from '../fixtures';

test('controls expose their roles to assistive technology', async ({ app, page }) => {
  await app.open();
  await app.navigate('Controls');
  await expect(page.getByRole('heading', { name: 'Controls' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Click me!' })).toBeVisible();
  await expect(page.getByRole('switch').first()).toBeVisible();
  await expect(page.getByRole('checkbox').first()).toBeVisible();
  await expect(page.getByRole('slider').first()).toBeVisible();
  await expect(page.getByRole('toolbar')).toBeVisible();
});

test('buttons are reachable and operable from the keyboard', async ({ app, page }) => {
  await app.open();
  await app.navigate('Controls');
  const button = page.getByRole('button', { name: 'Click me!' });
  await button.focus();
  await page.keyboard.press('Enter');
  await page.keyboard.press('Space');
  await expect(app.label('Clicks: 2')).toBeVisible();
});

test('alerts are modal dialogs', async ({ app, page }) => {
  await app.open();
  await app.navigate('Alerts & Dialogs');
  await app.button('Simple Alert (OK)').click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByRole('dialog').getByRole('button', { name: 'OK' })).toBeFocused();
});

test.describe('MAUI accessibility properties', () => {
  const byId = (page: import('@playwright/test').Page, id: string) => page.locator(`[data-automation-id="${id}"]`);

  test.beforeEach(async ({ app, page }) => {
    await app.open();
    await app.navigate('Accessibility');
    await expect(byId(page, 'a11y-heading')).toBeVisible();
  });

  test('HeadingLevel becomes a heading role that can be removed again', async ({ app, page }) => {
    await expect(page.getByRole('heading', { name: 'A level 2 heading', level: 2 })).toBeVisible();
    await app.button('Toggle heading').click();
    await expect(page.getByRole('heading', { name: 'A level 2 heading' })).toHaveCount(0);
    await expect(byId(page, 'a11y-heading')).not.toHaveAttribute('role');
    await app.button('Toggle heading').click();
    await expect(page.getByRole('heading', { name: 'A level 2 heading', level: 2 })).toBeVisible();
  });

  test('Description and Hint name and describe controls', async ({ page }) => {
    const close = page.getByRole('button', { name: 'Close', exact: true });
    await expect(close).toBeVisible();
    await expect(close).toHaveAccessibleDescription('Closes the current panel');
    // A switch's name has to land on the focusable checkbox, not its wrapper.
    await expect(page.getByRole('switch', { name: 'Wi-Fi' })).toBeVisible();
    const name = page.getByRole('textbox', { name: 'Full name' });
    await expect(name).toBeVisible();
    await expect(name).toHaveAccessibleDescription('As it appears on your card');
    await expect(page.getByRole('combobox', { name: 'Plan' })).toBeVisible();
    await expect(page.getByRole('group', { name: 'Pro plan card' })).toBeVisible();
    await expect(page.getByRole('img', { name: 'Sea green' })).toBeVisible();
  });

  test('a described label reads its description instead of its text', async ({ page }) => {
    await expect(byId(page, 'a11y-price')).toHaveAccessibleName('');
    await expect(byId(page, 'a11y-price')).toContainText('4 dollars 99 per month');
    await expect(byId(page, 'a11y-price').locator('[aria-hidden="true"]')).toHaveText('$4.99/mo');
  });

  test('image descriptions become alt text and other images are decorative', async ({ page }) => {
    await expect(page.getByRole('img', { name: 'The .NET bot waving' })).toBeVisible();
    await expect(byId(page, 'a11y-decorative')).toHaveAttribute('alt', '');
  });

  test('LabeledBy, Name and HelpText are honored', async ({ page }) => {
    await expect(page.getByRole('textbox', { name: 'Email address' })).toBeVisible();
    const legacy = page.getByRole('button', { name: 'Settings' });
    await expect(legacy).toBeVisible();
    await expect(legacy).toHaveAccessibleDescription('Opens app settings');
  });

  test('IsInAccessibleTree and ExcludedWithChildren hide views', async ({ app, page }) => {
    await expect(byId(page, 'a11y-stars')).toHaveAttribute('aria-hidden', 'true');
    // A layout outside the tree loses its own semantics but its children stay readable.
    await expect(byId(page, 'a11y-unwrapped')).toHaveAttribute('role', 'none');
    await expect(byId(page, 'a11y-unwrapped')).not.toHaveAttribute('aria-hidden');
    await expect(page.getByRole('button', { name: 'Hidden button' })).toHaveCount(0);
    await app.button('Include the hidden row').click();
    await expect(page.getByRole('button', { name: 'Hidden button' })).toBeVisible();
  });

  test('Announce speaks through a live region', async ({ app, page }) => {
    await app.button('Announce').click();
    await expect(page.locator('[role="status"][aria-live="polite"]')).toHaveText(/Saved\. Your changes are live\./);
  });

  test('font sizes follow the browser text size unless auto-scaling is off', async ({ page }) => {
    await expect(byId(page, 'a11y-scaled')).toHaveCSS('font-size', '16px');
    await page.evaluate(() => { document.documentElement.style.fontSize = '20px'; });
    await expect(byId(page, 'a11y-scaled')).toHaveCSS('font-size', '20px');
    await expect(byId(page, 'a11y-fixed')).toHaveCSS('font-size', '16px');
  });
});
