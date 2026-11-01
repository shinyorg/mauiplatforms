import { test, expect } from '../fixtures';

test.describe('page navigation', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
  });

  test('NavigationPage push and pop', async ({ app }) => {
    await app.navigate('Navigation');
    await expect(app.title).toHaveText('Nav Depth 1');
    await app.button('Push Page (→ Depth 2)').click();
    await expect(app.title).toHaveText('Nav Depth 2');
    await app.button('Push Page (→ Depth 3)').click();
    await expect(app.title).toHaveText('Nav Depth 3');
    await app.back.click();
    await expect(app.title).toHaveText('Nav Depth 2');
    await app.button('Pop Page (← Go Back)').click();
    await expect(app.title).toHaveText('Nav Depth 1');
  });

  test('TabbedPage switches tabs', async ({ app, page }) => {
    await app.navigate('TabbedPage');
    await expect(app.label('TabbedPage Overview')).toBeVisible();
    await app.tab('📊 Stats').click();
    await expect(app.tab('📊 Stats')).toHaveAttribute('aria-selected', 'true');
    await expect(app.label('73% sprint completion')).toBeVisible();
    await expect(app.label('TabbedPage Overview')).toBeHidden();
    await app.tab('⚙️ Settings').click();
    await expect(app.label('Settings')).toBeVisible();
  });

  test('FlyoutPage shows the selected item in its detail', async ({ app, page }) => {
    await app.navigate('FlyoutPage');
    await expect(app.label('Select an item from the sidebar to see its content here.')).toBeVisible();
    await app.button('📥  Inbox').click();
    await expect(app.label(/messages$/).first()).toBeVisible();
  });

  test('Shell: GoToAsync, query parameters and back', async ({ app, page }) => {
    await app.navigate('Shell Navigation');
    await app.button('Launch Shell App').click();
    await expect(app.label('🏠 Welcome Home')).toBeVisible();
    // The demo Shell starts with its flyout presented; tapping outside closes it.
    const backdrop = page.locator('.maui-flyout-backdrop').filter({ visible: true });
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 1200, y: 400 } });
    await expect(backdrop).toBeHidden();
    await app.button('GoToAsync → Details?id=42&name=Widget').click();
    await expect(app.label('ID: 42')).toBeVisible();
    await expect(app.label('Name: Widget')).toBeVisible();
    await app.back.click();
    await expect(app.label('🏠 Welcome Home')).toBeVisible();
    await app.button('GoToAsync → About').click();
    await expect(app.title).toHaveText(/About/);
  });

  test('ControlTemplate and ContentPresenter', async ({ app }) => {
    await app.navigate('ControlTemplate');
    await expect(app.label('This content is provided by the ContentView.')).toBeVisible();
  });
});

test.describe('modal pages', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
    await app.navigate('Modal Pages');
  });

  test('full-window modal', async ({ app, page }) => {
    await app.button('Full-Window Modal').click();
    await expect(page.locator('.maui-modal-layer')).toBeVisible();
    await app.button('Dismiss').click();
    await expect(app.label(/was dismissed/)).toBeVisible();
  });

  test('transparent full-window modal shows the page below it', async ({ app, page }) => {
    await app.button('Transparent Modal (Popup)').click();
    const layer = page.locator('.maui-modal-layer');
    await expect(layer).toBeVisible();
    // Neither the layer nor the page (BackgroundColor = Transparent) paints over what's below.
    await expect.poll(() => layer.evaluate(l => [l, l.firstElementChild!].map(e => getComputedStyle(e).backgroundColor)))
      .toEqual(['rgba(0, 0, 0, 0)', 'rgba(0, 0, 0, 0)']);
    // A page that doesn't set a background is still opaque.
    const defaultBackground = await layer.evaluate(l => {
      const page = l.firstElementChild as HTMLElement;
      page.style.backgroundColor = '';
      return getComputedStyle(page).backgroundColor;
    });
    expect(defaultBackground).not.toBe('rgba(0, 0, 0, 0)');
    await app.button('Dismiss').click();
    await expect(app.label('Transparent modal was dismissed.')).toBeVisible();
  });

  test('dialog modal dismisses from the backdrop', async ({ app, page }) => {
    await app.button('Dialog Modal (Default)').click();
    await expect(page.locator('.maui-modal-dialog')).toBeVisible();
    await page.locator('.maui-modal-backdrop').click({ position: { x: 5, y: 5 } });
    await expect(page.locator('.maui-modal-backdrop')).toHaveCount(0);
    await expect(app.label('Default dialog was dismissed.')).toBeVisible();
  });

  test('custom-size dialog modal', async ({ app, page }) => {
    await app.button('Dialog Modal (400×300)').click();
    await app.settled('.maui-modal-dialog');
    const box = await page.locator('.maui-modal-dialog').boundingBox();
    expect(box?.width).toBe(400);
    expect(box?.height).toBe(300);
  });

  test('stacked modals unwind', async ({ app, page }) => {
    await app.button('Push Two Stacked Modals').click();
    await expect(page.locator('.maui-modal-layer, .maui-modal-backdrop')).toHaveCount(1);
    await app.button('Push Another Modal').click();
    await expect(page.locator('.maui-modal-layer, .maui-modal-backdrop')).toHaveCount(2);
    await app.button('Dismiss').last().click();
    await app.button('Dismiss').last().click();
    await expect(page.locator('.maui-modal-layer, .maui-modal-backdrop')).toHaveCount(0);
    await expect(app.label('Stacked modals were dismissed.')).toBeVisible();
  });
});
