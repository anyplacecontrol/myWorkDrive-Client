import { test, expect } from '@playwright/test';
import * as fs from 'fs';

test('rename-demo', async ({ page }) => {
  try {

  // startup: startup
  await Promise.all([
    page.waitForResponse(r => r.url().includes('ListFolder')),
    page.goto('/my-files'),
  ]);

  // dblclick: foo
  await page.getByTestId('link-foo').dblclick();
  await page.waitForURL('**/*folder=%2Ffoo*');

  // dblclick: test-project
  await page.getByTestId('link-test-project').dblclick();
  await page.waitForURL('**/*folder=%2Ffoo%2Ftest-project*');

  // contextmenu: topographic-sequoia.jpg
  await page.getByTestId('link-topographic-sequoia.jpg').click({ button: 'right' });

  // click: Rename
  await page.getByRole('menuitem', { name: 'Rename' }).click();


  // click: Form
  await page.getByTestId('renameForm').getByLabel('Name').clear();
  await page.getByTestId('renameForm').getByLabel('Name').fill('topographic-sequoia.jpg2');
  await page.getByTestId('renameForm').getByRole('button', { name: 'Rename' }).click();
  await page.waitForResponse(r => r.url().includes('ListFolder'));

  // contextmenu: topographic-sequoia.jpg2
  await page.getByTestId('link-topographic-sequoia.jpg2').click({ button: 'right' });

  // click: Rename
  await page.getByRole('menuitem', { name: 'Rename' }).click();


  // click: Form
  await page.getByTestId('renameForm').getByLabel('Name').clear();
  await page.getByTestId('renameForm').getByLabel('Name').fill('topographic-sequoia.jpg');
  await page.getByTestId('renameForm').getByRole('button', { name: 'Rename' }).click();
  await page.waitForResponse(r => r.url().includes('ListFolder'));
  } finally {
    // Capture trace even on failure (if browser still open)
    try {
      await page.waitForTimeout(500);
      const logs = await page.evaluate(() => (window as any)._xsLogs || []);
      const traceFile = process.env.TRACE_OUTPUT || 'captured-trace.json';
      fs.writeFileSync(traceFile, JSON.stringify(logs, null, 2));
      console.log(`Trace captured to ${traceFile} (${logs.length} events)`);
    } catch (e) {
      console.log('Could not capture trace (browser may have closed)');
    }
  }
});
