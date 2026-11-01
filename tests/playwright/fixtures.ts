import { test as base, expect, type Locator, type Page } from '@playwright/test';

/** Errors the app wrote to the console or threw while a test ran. */
export type AppErrors = string[];

/** Helpers for driving the SampleWeb gallery through the DOM the web backend renders. */
export class App {
  constructor(readonly page: Page) {}

  /** The visible navigation bar title (nested NavigationPages hide the outer bar). */
  get title(): Locator {
    return this.page.locator('.maui-navbar .title').filter({ visible: true }).first();
  }

  /** The detail area the current gallery page is rendered into. */
  get content(): Locator {
    return this.page.locator('.maui-nav-content').filter({ visible: true }).last();
  }

  /** Loads the app and waits for the .NET runtime to render the first page. */
  async open(path = '/') {
    await this.page.goto(path);
    await expect(this.title).toHaveText('Overview', { timeout: 30_000 });
  }

  /** Opens a gallery page from the flyout menu, opening the menu first on narrow screens. */
  async navigate(menuTitle: string) {
    const item = this.page.locator('.maui-flyout-pane').getByText(menuTitle, { exact: true });
    // On narrow screens the flyout may still be sliding open or shut; decide once it has settled.
    await this.settled('.maui-flyout-pane');
    if (!(await item.isVisible()))
      await this.page.getByRole('button', { name: 'Open navigation' }).click();
    await this.settled('.maui-flyout-pane');
    await item.click();
  }

  /** Waits until the matching elements have no running CSS transitions or animations. */
  async settled(selector: string) {
    await this.page.waitForFunction(s =>
      [...document.querySelectorAll(s)].every(e => e.getAnimations({ subtree: true }).every(a => a.playState !== 'running')), selector);
  }

  /** The visible tabs of a TabbedPage. */
  tab(name: string): Locator {
    return this.page.locator('.maui-tabbed-page').getByRole('tab', { name, exact: true }).filter({ visible: true });
  }

  /** A MAUI Label by its exact text. */
  label(text: string | RegExp): Locator {
    return this.page.locator('.maui-label').getByText(text, { exact: typeof text === 'string' }).filter({ visible: true });
  }

  /** A MAUI Button (or any element exposed as a button) by its exact text. */
  button(name: string): Locator {
    return this.page.getByRole('button', { name, exact: true }).filter({ visible: true });
  }

  /** The open DisplayAlert/ActionSheet/Prompt dialog. */
  get dialog(): Locator {
    return this.page.locator('dialog.maui-dialog[open]');
  }

  /** The navigation bar's back button (the visible one, ignoring pages that draw their own). */
  get back(): Locator {
    return this.page.locator('.maui-navbar > button[aria-label="Back"]').filter({ visible: true }).first();
  }
}

/** Console noise that is not an app failure. */
const ignored = [
  /Failed to load resource/i,         // e.g. the WebView demo's third-party iframe content
  /favicon\.ico/i,
];

export const test = base.extend<{ app: App; appErrors: AppErrors }>({
  appErrors: [async ({ page }, use, testInfo) => {
    const errors: AppErrors = [];
    page.on('pageerror', e => errors.push(`pageerror: ${e.message}`));
    page.on('console', m => {
      if (m.type() === 'error' && !ignored.some(r => r.test(m.text())))
        errors.push(`console.error: ${m.text()}`);
    });
    await use(errors);
    // Every test also asserts the app stayed healthy: an exception in a handler or a failed
    // navigation is logged by the backend even when the page still looks right.
    if (testInfo.status === testInfo.expectedStatus && !testInfo.annotations.some(a => a.type === 'allow-errors'))
      expect(errors, 'the app logged errors').toEqual([]);
  }, { auto: true }],

  app: async ({ page }, use) => {
    await use(new App(page));
  },
});

export { expect };

/** Every page in the SampleWeb flyout menu, in menu order. */
export const galleryPages = [
  'Home', 'XAML Runtime', 'Image / Font / Icon', 'Image Showcase', 'Controls', 'Pickers & Search', 'Layouts',
  'Flex & Absolute', 'FormattedText', 'Shapes', 'Transforms & Effects', 'Alerts & Dialogs', 'Collection View',
  'Text Input Styling', 'Refresh & Swipe', 'Carousel & Indicators', 'Graphics (Canvas)', 'Graphics Features',
  'Device & App Info', 'Battery & Network', 'Clipboard & Storage', 'Communication', 'Launch & Share',
  'Location, Sensors & Speech', 'WebView', 'Navigation', 'TabbedPage', 'FlyoutPage', 'Shell Navigation',
  'ControlTemplate', 'Modal Pages', 'Theme',
];
