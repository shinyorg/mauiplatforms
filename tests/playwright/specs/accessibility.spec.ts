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
