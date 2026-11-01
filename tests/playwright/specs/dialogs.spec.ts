import { test, expect } from '../fixtures';

// DisplayAlert / DisplayActionSheet / DisplayPrompt render as HTML <dialog>s and resolve the
// awaiting MAUI task with the user's choice.
test.describe('Alerts & Dialogs', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
    await app.navigate('Alerts & Dialogs');
  });

  test('simple alert', async ({ app }) => {
    await app.button('Simple Alert (OK)').click();
    await expect(app.dialog).toBeVisible();
    await expect(app.dialog.locator('.maui-dialog-title')).toHaveText('Hello!');
    // DisplayAlert(title, message, cancel) has exactly one button.
    await expect(app.dialog.getByRole('button')).toHaveCount(1);
    await app.dialog.getByRole('button', { name: 'OK' }).click();
    await expect(app.dialog).toHaveCount(0);
    await expect(app.label('Result: Simple alert dismissed')).toBeVisible();
  });

  test('confirm returns the choice', async ({ app }) => {
    await app.button('Confirm Alert (Yes / No)').click();
    await app.dialog.getByRole('button', { name: 'No' }).click();
    await expect(app.label('Result: Confirmed = False')).toBeVisible();
    await app.button('Confirm Alert (Yes / No)').click();
    await app.dialog.getByRole('button', { name: 'Yes' }).click();
    await expect(app.label('Result: Confirmed = True')).toBeVisible();
  });

  test('Escape cancels a confirm', async ({ app, page }) => {
    await app.button('Confirm Alert (Yes / No)').click();
    await expect(app.dialog).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(app.label('Result: Confirmed = False')).toBeVisible();
  });

  test('action sheet returns the chosen button', async ({ app }) => {
    await app.button('Action Sheet').click();
    await expect(app.dialog.getByRole('button', { name: 'Delete' })).toHaveClass(/destructive/);
    await app.dialog.getByRole('button', { name: 'Move' }).click();
    await expect(app.label('Result: Action = Move')).toBeVisible();
  });

  test('prompt returns the typed text', async ({ app }) => {
    await app.button('Text Prompt').click();
    const input = app.dialog.locator('input.maui-dialog-input');
    await expect(input).toHaveAttribute('placeholder', 'Enter name...');
    await expect(input).toBeFocused();
    await input.fill('Ada');
    await input.press('Enter');
    await expect(app.label('Result: Name = Ada')).toBeVisible();
  });

  test('prompt shows its initial value and can be cancelled', async ({ app }) => {
    await app.button('Prompt (with initial value)').click();
    await expect(app.dialog.locator('input.maui-dialog-input')).toHaveValue('Hello World');
    await app.dialog.getByRole('button', { name: 'Cancel' }).click();
    await expect(app.label('Result: Prompt cancelled')).toBeVisible();
  });
});
