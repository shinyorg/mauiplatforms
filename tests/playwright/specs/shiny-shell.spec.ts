import { test, expect, type App } from '../fixtures';

// The menu's Shiny Shell tab: Shiny.Maui.Shell (navigation, dialogs, guards, observers, badges, app
// links, AI tools, SwitchShell) and its ShinyDialogs / UxDiversDialogs add-ons, in a ShinyShell that
// replaces the gallery while it runs.

/** Opens a Shiny Shell tab entry and launches the demo Shell from it. */
async function launch(app: App, entry: string, tabTitle: string) {
  await app.open();
  await app.page.locator('.maui-flyout-pane').getByText('Shiny Shell', { exact: true }).click();
  await app.navigate(entry);
  await app.button('Open in the Shiny Shell demo').click();
  await expect(app.title).toHaveText(tabTitle);
}

const tab = (app: App, name: string) =>
  app.page.locator('.maui-tabbar.bottom').getByRole('tab', { name: new RegExp(`^${name}`) });

/** The newest log line on the Events tab that contains the text. */
async function expectLogged(app: App, text: string | RegExp) {
  await tab(app, 'Events').click();
  await expect(app.title).toHaveText('Events');
  await expect(app.page.locator('.maui-label').filter({ hasText: text }).first()).toBeAttached();
}

test.describe('Shiny Shell', () => {
  test('launches the demo Shell at the chosen tab and goes back to the gallery', async ({ app, page }) => {
    await launch(app, 'Guards', 'Guards');
    await expect(page.locator('.maui-tabbar.bottom').getByRole('tab')).toHaveText(['Navigate', 'Dialogs', 'Guards', 'Events', 'More']);
    await tab(app, 'More').click();
    await app.button('Back to the gallery').click();
    await expect(page.locator('.maui-flyout-pane').getByText('5 demos', { exact: true })).toBeVisible();
  });

  test('pushes pages by route, view model, generated method and markup', async ({ app }) => {
    await launch(app, 'Navigation', 'Navigate');
    const cases: [string, string][] = [
      ['NavigateTo(route, args)', "ItemId = 'A-1', Count = 1"],
      ['NavigateTo<DetailViewModel>', "ItemId = 'B-2', Count = 2"],
      ['Generated NavigateToShellDetail', "ItemId = 'C-3', Count = 3"],
      ['Navigate.Route (markup)', "ItemId = 'D-4 from markup', Count = 0"],
    ];
    for (const [button, arrived] of cases) {
      await app.button(button).click();
      await expect(app.title).toHaveText('Detail');
      await expect(app.label(new RegExp(arrived.replace(/[()]/g, '\\$&')))).toBeVisible();
      await app.back.click();
      await expect(app.title).toHaveText('Navigate');
    }
  });

  test('returns results with GoBack and walks a builder chain', async ({ app }) => {
    await launch(app, 'Navigation', 'Navigate');
    await app.button('Push Chain 1 › 2 › 3').click();
    await expect(app.label(/ItemId = 'Chain 3'/)).toBeVisible();
    await app.back.click();
    await expect(app.label(/ItemId = 'Chain 2'/)).toBeVisible();
    await app.button('GoBack with a result').click();
    await expect(app.label(/ItemId = 'Chain 1'/)).toBeVisible();
    await app.button('PopToRoot').click();
    await expect(app.title).toHaveText('Navigate');
    await expect(app.label(/Result = popped to root/)).toBeVisible();

    await app.button('Generated NavigateToShellDetail').click();
    await app.button('Replace with the issue page').click();
    await expect(app.title).toHaveText('Issue');
    await expect(app.label("Replaced detail 'C-3'")).toBeVisible();
  });

  test('opens and closes a modal page', async ({ app }) => {
    await launch(app, 'Navigation', 'Navigate');
    await app.button('Open the modal page').click();
    await expect(app.page.locator('.maui-modal-layer')).toHaveCount(1);
    await app.button('Close').click();
    await expect(app.page.locator('.maui-modal-layer')).toHaveCount(0);
    await expect(app.label(/Result = closed the modal/)).toBeVisible();
  });

  test('IDialogs through the MAUI and Shiny Controls providers', async ({ app, page }) => {
    await launch(app, 'Dialogs', 'Dialogs');
    await app.button('Confirm').click();
    await app.dialog.getByRole('button', { name: 'Yes' }).click();
    await expect(app.label('Confirm: True')).toBeVisible();

    await page.getByRole('combobox', { name: 'Provider' }).selectOption('ShinyControls');
    await app.button('Alert').click();
    await expect(page.getByText('This alert comes from the ShinyControls provider.')).toBeVisible();
    await page.getByRole('button', { name: 'OK', exact: true }).filter({ visible: true }).last().click();
    await expect(app.label('Alert dismissed')).toBeVisible();
  });

  test('UXDivers popups run in the browser', async ({ app, page }) => {
    await launch(app, 'Dialogs', 'Dialogs');
    await page.getByRole('combobox', { name: 'Provider' }).selectOption('UXDivers');
    const overlay = page.locator('.maui-overlay-layer');

    await app.button('Confirm').click();
    await expect(overlay).toHaveCount(1);
    await expect(overlay.getByText('Do you like this provider?')).toBeVisible();
    await overlay.getByRole('button', { name: 'Not really' }).click();
    await expect(overlay).toHaveCount(0);
    await expect(app.label('Confirm: False')).toBeVisible();

    await app.button('Prompt').click();
    await overlay.locator('input').fill('Ada');
    await overlay.getByRole('button', { name: 'OK' }).click();
    await expect(app.label("Prompt: 'Ada'")).toBeVisible();

    await app.button('Action sheet').click();
    await overlay.getByText('Messages', { exact: true }).click();
    await expect(app.label('Action sheet: Messages')).toBeVisible();

    // A click on the dimmed backdrop, outside the card, dismisses the popup.
    await app.button('Confirm').click();
    await expect(overlay.getByText('Do you like this provider?')).toBeVisible();
    await page.mouse.click(1200, 150);
    await expect(overlay).toHaveCount(0);
  });

  for (const [presenter, colour] of [['ShellModal', 'Amber'], ['ShinyOverlay', 'Slate'], ['UXDivers', 'Coral']] as const) {
    test(`ViewModel dialog with the ${presenter} presenter`, async ({ app, page }) => {
      await launch(app, 'Dialogs', 'Dialogs');
      await page.getByRole('combobox', { name: 'Presenter' }).selectOption(presenter);
      await app.button('Pick a colour').click();
      await expect(app.label('Preset: Lagoon')).toBeVisible();
      await app.button(colour).click();
      await expect(app.label(`Picked ${colour} with the ${presenter} presenter`)).toBeVisible();
    });
  }

  test('a backdrop tap cancels a UXDivers ViewModel dialog', async ({ app, page }) => {
    await launch(app, 'Dialogs', 'Dialogs');
    await page.getByRole('combobox', { name: 'Presenter' }).selectOption('UXDivers');
    await app.button('Pick a colour').click();
    await expect(app.label('Preset: Lagoon')).toBeVisible();
    await page.mouse.click(1200, 150);
    await expect(app.label('Dialog cancelled (UXDivers)')).toBeVisible();
    await expect(page.locator('.maui-overlay-layer')).toHaveCount(0);
  });

  test('the interceptor cancels, redirects and can be bypassed', async ({ app, page }) => {
    await launch(app, 'Guards', 'Guards');
    const guard = page.getByRole('combobox', { name: 'Guard' });

    await guard.selectOption('Cancel');
    await app.button('Open the detail page').click();
    await expect(app.label('NavigateTo returned false: an interceptor cancelled it')).toBeVisible();
    await expect(app.title).toHaveText('Guards');

    await app.button('Open it, bypassing interceptors').click();
    await expect(app.label(/ItemId = 'bypassed'/)).toBeVisible();
    await app.back.click();

    await guard.selectOption('RedirectToLogin');
    await app.button('Open the detail page').click();
    await expect(app.title).toHaveText('Sign in');
    await app.button('Sign in').click();
    await expect(app.label(/ItemId = 'after sign-in'/)).toBeVisible();
  });

  test('CanNavigate asks before a tab tap leaves unsaved changes', async ({ app, page }) => {
    await launch(app, 'Guards', 'Guards');
    await page.getByRole('switch').filter({ visible: true }).click();

    await tab(app, 'Events').click();
    await app.dialog.getByRole('button', { name: 'Stay' }).click();
    await expect(app.title).toHaveText('Guards');

    await tab(app, 'Events').click();
    await app.dialog.getByRole('button', { name: 'Discard' }).click();
    await expect(app.title).toHaveText('Events');
    await expect(app.page.locator('.maui-label').filter({ hasText: 'Left the Guards tab, discarding changes' }).first()).toBeAttached();
  });

  test('observers and lifecycle hooks write to the log', async ({ app }) => {
    await launch(app, 'Navigation', 'Navigate');
    await app.button('Generated NavigateToShellDetail').click();
    await app.back.click();
    await expectLogged(app, /\[Lifecycle\] Detail #\d+ Dispose/);
    await expectLogged(app, /\[Navigated\] ShellDetail \(Push, to DetailViewModel\)/);
    // A tab tap is Shell-driven navigation: only the observer sees it.
    await expectLogged(app, /\[Observer\] #\d+ Shell SetRoot → \/\/demo\/ShellEvents/);
  });

  test('tab badges show counts, dots and text', async ({ app, page }) => {
    await launch(app, 'Events & Badges', 'Events');
    await app.button('Dialogs +1').click();
    await app.button('Dialogs +1').click();
    await app.button('Dot on Guards').click();
    await app.button("'new' on More").click();

    const badge = (name: string) => tab(app, name).locator('.maui-tab-badge');
    await expect(badge('Dialogs')).toHaveText('2');
    await expect(tab(app, 'Dialogs')).toHaveAttribute('aria-label', 'Dialogs (2)');
    await expect(badge('Guards')).toHaveClass(/dot/);
    await expect(badge('More')).toHaveText('new');
    await expect(badge('Navigate')).toBeHidden();

    await app.button('Clear all').click();
    await expect(page.locator('.maui-tab-badge:visible')).toHaveCount(0);
  });

  test('app links, AI tools, routes and SwitchShell', async ({ app }) => {
    await launch(app, 'App Links & AI', 'More');
    await expect(app.label(/• ShellDetail {2}→ {2}DetailPage \/ DetailViewModel/)).toBeVisible();

    await app.button('Handle').click();
    await expect(app.label(/ItemId = '42'/)).toBeVisible();
    await app.back.click();
    await expect(app.label('Navigated: route ShellDetail')).toBeVisible();

    await app.button('NavigateToRoute').click();
    await expect(app.title).toHaveText('Issue');
    await expect(app.label('The kitchen sink is leaking everywhere')).toBeVisible();
    await expect(app.label('Urgent')).toBeVisible();
    await app.back.click();

    await app.button('Switch to the other Shell').click();
    await expect(app.title).toHaveText('Another Shell');
    await app.button('Switch back').click();
    await expect(app.title).toHaveText('Navigate');
  });
});
