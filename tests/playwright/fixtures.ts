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
    const menu = this.page.getByRole('button', { name: 'Open navigation' });
    const list = this.page.locator('.maui-flyout-pane .maui-collection').first();
    // On narrow screens the flyout may still be sliding shut, or only just switching from docked to
    // popover. A closing pane stays visible until its transition ends, so the item can look
    // clickable and then disappear. Retry until the click lands, opening the menu whenever the
    // item is hidden.
    await expect(async () => {
      await this.settled('.maui-flyout-pane');
      // The menu is a virtualized CollectionView: rows past the viewport aren't in the DOM until
      // it scrolls to them. Page through it (wrapping at the end) until the row is rendered.
      if ((await item.count()) === 0 && (await list.isVisible())) {
        await list.evaluate(el => {
          el.scrollTop = el.scrollTop + el.clientHeight >= el.scrollHeight - 1 ? 0 : el.scrollTop + el.clientHeight * 0.75;
        });
        throw new Error(`scrolling the menu to '${menuTitle}'`);
      }
      if (!(await item.isVisible()))
        await menu.click({ timeout: 2_000 });
      await this.settled('.maui-flyout-pane');
      await item.click({ timeout: 2_000 });
    }).toPass({ timeout: 30_000 });
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
    // Errors a test said to expect (see expectBrokenImageErrors) are dropped here, at teardown, so
    // ones logged after the test body finished are dropped too.
    const expected = testInfo.annotations.filter(a => a.type === 'expected-error').map(a => new RegExp(a.description!));
    for (let i = errors.length - 1; i >= 0; i--)
      if (expected.some(r => r.test(errors[i]))) errors.splice(i, 1);
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

/**
 * Tells the current test to expect the errors WebKit logs for the Image Viewer page's deliberately
 * broken image URL (https://example.com/does-not-exist.png, shown to demo the error artwork).
 */
export function expectBrokenImageErrors() {
  for (const pattern of ['example\\.com/does-not-exist\\.png', 'Preflight response is not successful'])
    test.info().annotations.push({ type: 'expected-error', description: pattern });
}

/** Every page in the SampleWeb flyout menu, in menu order. */
export const galleryPages = [
  'Home', 'XAML Runtime', 'Image / Font / Icon', 'Image Showcase', 'Controls', 'Pickers & Search', 'Layouts',
  'Flex & Absolute', 'FormattedText', 'Shapes', 'Transforms & Effects', 'Alerts & Dialogs', 'Collection View', 'Scrolling',
  'Text Input Styling', 'Refresh & Swipe', 'Carousel & Indicators', 'Graphics (Canvas)', 'Graphics Features', 'SkiaSharp',
  'Device & App Info', 'Battery & Network', 'Clipboard & Storage', 'Communication', 'Launch & Share',
  'Location, Sensors & Speech', 'WebView', 'Navigation', 'TabbedPage', 'FlyoutPage', 'Shell Navigation',
  'ControlTemplate', 'Modal Pages', 'Theme', 'Accessibility',
];

/** Every page in the flyout's Shiny Controls tab, in menu order. */
export const shinyPages = [
  'CameraView', 'MediaElement', 'VideoText', 'Pills', 'Badge', 'Avatar', 'Chips', 'ShinyButton', 'ButtonGroup', 'FAB & FabMenu',
  'Gauge', 'Progress Bar', 'Slider', 'Range Slider', 'Range Pickers', 'Expander', 'Timeline', 'Text Entry',
  'TagEntry', 'Password Strength', 'Skeleton', 'Marquee', 'Confetti', 'Animated Backgrounds', 'Border Animations',
  'Toast', 'Dialogs', 'ChatView', 'Floating Panel', 'Top Sheet', 'Dual Panels', 'Minimized Sheet', 'Signature Pad',
  'Scheduler Calendar', 'Scheduler Agenda', 'Scheduler List', 'Agenda + Calendar Picker', 'Gantt', 'Diagram',
  'Mermaid Flowchart', 'Mermaid Directions', 'Mermaid Subgraphs', 'Mermaid Themes', 'Mermaid Editor',
  'Image Viewer', 'Image Gallery', 'TableView Settings', 'TableView Pickers', 'TableView Dynamic', 'TableView Drag Sort',
  'TableView Styling', 'Tree View', 'Data Grid', 'Tree Data Grid', 'Grid Grouping', 'Grid Formatting', 'Overlay', 'Kanban', 'Keyframe Animation', 'Walkthrough', 'Tooltip', 'Spreadsheet',
  'Document Viewer', 'Slide Viewer',
];
