import { test, expect } from '../fixtures';

// Input reaches MAUI: each control is driven through the real DOM element the backend renders and
// the assertion is on a Label the sample updates from the control's MAUI event.
test.describe('Controls page', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
    await app.navigate('Controls');
    await expect(app.title).toHaveText('Controls');
  });

  test('Button.Clicked', async ({ app }) => {
    await app.button('Click me!').click();
    await app.button('Click me!').click();
    await expect(app.label('Clicks: 2')).toBeVisible();
  });

  test('Entry.TextChanged', async ({ app, page }) => {
    await page.getByPlaceholder('Type here...').fill('Hello web');
    await expect(app.label('Echo: Hello web')).toBeVisible();
  });

  test('Slider.ValueChanged', async ({ app, page }) => {
    await page.locator('input.maui-slider').first().fill('80');
    await expect(app.label('Slider: 80')).toBeVisible();
  });

  test('Switch.Toggled', async ({ app, page }) => {
    const toggle = page.getByRole('switch').first();
    await toggle.click();
    await expect(toggle).toBeChecked();
    await expect(app.label('On')).toBeVisible();
  });

  test('CheckBox.CheckedChanged', async ({ app, page }) => {
    await page.locator('input.maui-check-box').first().click();
    await expect(app.label('Checked ✓')).toBeVisible();
  });

  test('Stepper increments by its Increment', async ({ app, page }) => {
    await page.getByRole('button', { name: 'Increase' }).first().click();
    await page.getByRole('button', { name: 'Increase' }).first().click();
    await expect(app.label('Stepper: 10')).toBeVisible();
    await page.getByRole('button', { name: 'Decrease' }).first().click();
    await expect(app.label('Stepper: 5')).toBeVisible();
  });

  test('RadioButton group selection', async ({ app, page }) => {
    await page.locator('.maui-radio').getByText('Option C', { exact: true }).click();
    await expect(app.label('Selected: Option C')).toBeVisible();
  });

  test('Editor accepts multi-line text', async ({ page }) => {
    const editor = page.getByPlaceholder('Multi-line text editor...');
    await editor.fill('line one\nline two');
    await expect(editor).toHaveValue('line one\nline two');
  });

  test('Behavior limits an Entry to numbers', async ({ app, page }) => {
    await page.getByPlaceholder('Only numbers allowed').pressSequentially('42');
    await expect(app.label('✅ Valid number: 42')).toBeVisible();
  });
});

test.describe('Pickers & Search page', () => {
  test.beforeEach(async ({ app }) => {
    await app.open();
    await app.navigate('Pickers & Search');
  });

  test('Picker.SelectedIndexChanged', async ({ app, page }) => {
    await page.locator('select.maui-picker').first().selectOption('Blue');
    await expect(app.label('Selected: Blue')).toBeVisible();
  });

  test('DatePicker.DateSelected', async ({ app, page }) => {
    await page.locator('input.maui-date-picker').first().fill('2026-03-14');
    await expect(app.label(/^Selected date: .*2026/)).toBeVisible();
  });

  test('SearchBar.TextChanged filters', async ({ app, page }) => {
    await page.getByPlaceholder('Search fruits...').fill('an');
    await expect(app.label('Found: Banana, Mango')).toBeVisible();
    await page.getByPlaceholder('Search fruits...').fill('zzz');
    await expect(app.label('No matches found.')).toBeVisible();
  });
});
