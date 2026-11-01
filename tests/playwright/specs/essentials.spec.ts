import { test, expect } from '../fixtures';

test('DeviceInfo and AppInfo describe the browser app', async ({ app }) => {
  await app.open();
  await app.navigate('Device & App Info');
  await expect(app.label('  Platform: Web')).toBeVisible();
  await expect(app.label('  Package Name: org.shiny.web')).toBeVisible();
});

test('Preferences persist across a reload', async ({ app, page }) => {
  await app.open();
  await app.navigate('Location, Sensors & Speech');
  const note = page.getByPlaceholder('Type a note, then reload the page');
  await note.fill('remember me');
  await page.reload();
  await expect(app.title).toHaveText('Overview', { timeout: 30_000 });
  await app.navigate('Location, Sensors & Speech');
  await expect(note).toHaveValue('remember me');
});

test('Preferences save and load', async ({ app, page }) => {
  await app.open();
  await app.navigate('Clipboard & Storage');
  await page.getByPlaceholder('Value').fill('42');
  await app.button('💾 Save Preference').click();
  await expect(app.label(/^Saved: demo_key = /)).toBeVisible();
  await app.button('📂 Load Preference').click();
  await expect(app.label(/^Loaded: demo_key = .*42/)).toBeVisible();
});

test('Clipboard round-trips text', async ({ app, page, context, browserName }) => {
  test.skip(browserName !== 'chromium', 'clipboard permissions can only be granted in Chromium');
  await context.grantPermissions(['clipboard-read', 'clipboard-write']);
  await app.open();
  await app.navigate('Clipboard & Storage');
  await page.getByPlaceholder('Text to copy...').fill('copied by the test');
  await app.button('📋 Copy to Clipboard').click();
  await expect(app.label('Copied: "copied by the test"')).toBeVisible();
  expect(await page.evaluate(() => navigator.clipboard.readText())).toBe('copied by the test');
  await page.evaluate(() => navigator.clipboard.writeText('pasted from the test'));
  await app.button('📄 Paste from Clipboard').click();
  await expect(app.label('Pasted: "pasted from the test"')).toBeVisible();
});

test('Geolocation reads the browser location', async ({ app, context }) => {
  await context.grantPermissions(['geolocation']);
  await context.setGeolocation({ latitude: 47.6062, longitude: -122.3321, accuracy: 25 });
  await app.open();
  await app.navigate('Location, Sensors & Speech');
  await app.button('Get my location').click();
  await expect(app.label(/^47\.6062, -122\.3321/)).toBeVisible();
});

test('Connectivity reports going offline', async ({ app, context }) => {
  await app.open();
  await app.navigate('Battery & Network');
  await expect(app.label('  Network Access: Internet')).toBeVisible();
  await context.setOffline(true);
  await app.button('🔄 Refresh').click();
  await expect(app.label(/Network Access: (None|Local)/)).toBeVisible();
  await context.setOffline(false);
});
