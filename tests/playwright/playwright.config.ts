import { defineConfig, devices } from '@playwright/test';

// The app under test is the published SampleWeb (trimmed Release build), because trimming is where
// the web backend most often breaks. Publish it first:
//   npm run publish-sample            (or: dotnet publish samples/SampleWeb -c Release -o artifacts/SampleWeb)
// SAMPLE_WWWROOT points somewhere else if needed.
const wwwroot = process.env.SAMPLE_WWWROOT ?? '../../artifacts/SampleWeb/wwwroot';
const port = Number(process.env.PORT ?? 5199);

export default defineConfig({
  testDir: './specs',
  // Each test boots the .NET runtime in a fresh page; a few seconds each, so keep the workers busy.
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL: `http://127.0.0.1:${port}/`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    viewport: { width: 1280, height: 860 },
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 860 },
        // A synthetic camera and microphone for the Shiny CameraView tests (shiny.spec.ts).
        launchOptions: { args: ['--use-fake-device-for-media-stream', '--use-fake-ui-for-media-stream'] },
      },
    },
    { name: 'webkit', use: { ...devices['Desktop Safari'], viewport: { width: 1280, height: 860 } } },
    { name: 'mobile', use: { ...devices['Pixel 7'] }, testMatch: /responsive\.spec\.ts/ },
  ],
  webServer: {
    command: `node server.mjs ${wwwroot} ${port}`,
    url: `http://127.0.0.1:${port}/index.html`,
    reuseExistingServer: !process.env.CI,
  },
});
