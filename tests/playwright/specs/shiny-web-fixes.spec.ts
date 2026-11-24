import { test, expect, type App } from '../fixtures';

// Regressions found in the Shiny Controls gallery: borderless text fields, Layout child replacement
// (the agent's streamed markdown), nested taps and pans (TreeView, the scheduler agenda), and the
// timeline rail.

async function openShiny(app: App, title: string) {
  await app.open();
  await app.page.locator('.maui-flyout-pane').getByText('Shiny Controls', { exact: true }).click();
  await app.navigate(title);
}

test.describe('Shiny controls on the web backend', () => {
  for (const page of ['TagEntry', 'Password Strength', 'Text Entry']) {
    test(`${page}: the text field draws no border of its own`, async ({ app }) => {
      await openShiny(app, page);
      const input = app.content.getByRole('textbox').first();
      await expect(input).toHaveClass(/maui-borderless/);
      await expect(input).toHaveCSS('border-top-width', '0px');
      await expect(input).toHaveCSS('background-color', 'rgba(0, 0, 0, 0)');
    });
  }

  test('Agent View: streamed markdown leaves no stale copies behind', async ({ app, page }) => {
    await openShiny(app, 'Agent View');
    await app.content.getByText('What can you do?', { exact: true }).click();
    const reply = page.locator('.maui-label').filter({ hasText: 'no model, no API key' });
    await expect(reply.filter({ hasText: 'Try:' })).toHaveCount(1, { timeout: 20_000 });
    // Each intermediate state of the stream (ending in the typing caret) used to stay in the DOM.
    await expect(reply).toHaveCount(1);
    await expect(page.locator('.maui-label').filter({ hasText: '▍' })).toHaveCount(0);
  });

  test('TreeView: the chevron expands a folder without selecting the row', async ({ app }) => {
    await openShiny(app, 'Tree View');
    const chevron = app.content.locator('.maui-label').getByText('▶', { exact: true }).first();
    await chevron.click();
    await expect(app.label('Projects')).toBeVisible();
    await expect(app.label('Expanded: Documents')).toBeVisible();

    // A row can still be dragged onto a folder after the press-and-move starts a pan.
    const from = (await app.label('todo.txt').boundingBox())!;
    const to = (await app.label('Downloads').boundingBox())!;
    await app.page.mouse.move(from.x + 10, from.y + from.height / 2);
    await app.page.mouse.down();
    for (let i = 1; i <= 10; i++)
      await app.page.mouse.move(from.x + 10, from.y + from.height / 2 + (to.y - from.y) * i / 10);
    await app.page.mouse.up();
    await expect(app.label('Moved todo.txt into Downloads')).toBeVisible();
  });

  test('Scheduler Agenda: an event can be dragged to a new time', async ({ app, page }) => {
    await openShiny(app, 'Scheduler Agenda');
    const gym = app.label('Gym');
    // .NET formats times with a narrow no-break space before AM/PM; \s matches it.
    await expect(app.label(/^5:30\sPM - 6:30\sPM$/)).toBeVisible();
    // The agenda rebuilds its event views once more after it first lays out (scrolling to now);
    // a press on a view that's then replaced goes nowhere.
    await page.waitForTimeout(1500);
    const box = (await gym.boundingBox())!;
    const x = box.x + 200, y = box.y + 20;
    await page.mouse.move(x, y);
    await page.mouse.down();
    // Down 90px, about an hour and a half: the pointer crosses the event's bottom resize grip and
    // leaves the event on the way, neither of which may end the drag.
    for (let i = 1; i <= 15; i++) {
      await page.mouse.move(x, y + 90 * i / 15);
      await page.waitForTimeout(16);
    }
    await page.mouse.up();
    // Dragging near the bottom edge auto-scrolls the agenda, so the exact drop time varies:
    // it moved later than 5:30 and kept its hour.
    await expect(app.label(/^5:30\sPM - 6:30\sPM$/)).toHaveCount(0);
    await expect(app.label(/^(6|7|8):\d\d\sPM - (7|8|9):\d\d\sPM$/)).toHaveCount(1);
    // The release is not also a tap on the empty timeline.
    await expect(page.getByText('Time Selected')).toHaveCount(0);
  });

  test('Timeline: the rail and titles line up on every row', async ({ app }) => {
    await openShiny(app, 'Timeline');
    const xs = await Promise.all(['Order placed', 'Picked', 'In transit', 'Out for delivery', 'Delivered']
      .map(async t => Math.round((await app.label(t).boundingBox())!.x)));
    expect(new Set(xs).size, `title x positions ${xs}`).toBe(1);
  });
});
