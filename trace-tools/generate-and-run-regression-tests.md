# Trace-Based Regression Testing

This workflow captures user journeys as traces, generates Playwright tests from them, and detects regressions after code changes.

## Part 1: Capture a Baseline Trace

1. Start the dev server: `npm run dev`
2. Open http://localhost:5188/my-files in Chrome
3. Open DevTools → Elements → select the `<iframe>` → right-click → "Reveal in Inspector panel"
4. In the Inspector, click **Clear** to start fresh
5. Perform a user journey:
   - Double-click `foo` folder
   - Double-click `test-project` folder
   - Right-click `topographic-sequoia.jpg` → Rename → enter `topographic-sequoia.jpg2` → click Rename
   - Right-click `topographic-sequoia.jpg2` → Rename → enter `topographic-sequoia.jpg` → click Rename
6. In the Inspector, click **Export → JSON**
7. Save as `~/Downloads/baseline.json`

## Part 2: Verify the Baseline Works

```bash
./trace-tools/trace.sh --run --test-name rename-demo ~/Downloads/baseline.json
```

**Expected output:**
```
═══════════════════════════════════════════════════════════════
                    REGRESSION TEST REPORT
═══════════════════════════════════════════════════════════════

✅ Test PASSED - Journey completed successfully

✓ Traces match semantically

Before:
  APIs: GET /ListFolder, POST /MoveFile
  Form submits: 2 (topographic-sequoia.jpg2 → topographic-sequoia.jpg)

After:
  APIs: GET /ListFolder, POST /MoveFile
  Form submits: 2 (topographic-sequoia.jpg2 → topographic-sequoia.jpg)

═══════════════════════════════════════════════════════════════
```

## Part 3: Introduce a Breaking Change

Edit `src/components/modals/RenameItemModal.xmlui.xs` line 6:
```javascript
// Change this:
const url = isFolder ? "/MoveFolder" : "/MoveFile";
// To this:
const url = isFolder ? "/MoveFolder" : "/MoveFil";
```

## Part 4: Detect the Regression

```bash
./trace-tools/trace.sh --run --test-name rename-demo ~/Downloads/baseline.json
```

**Expected output:**
```
═══════════════════════════════════════════════════════════════
                    REGRESSION TEST REPORT
═══════════════════════════════════════════════════════════════

❌ Test FAILED - Regression detected!

    Error: page.waitForResponse: Test timeout of 30000ms exceeded.

      30 |   await page.getByTestId('renameForm').getByLabel('Name').fill('topographic-sequoia.jpg2');
      31 |   await page.getByTestId('renameForm').getByRole('button', { name: 'Rename' }).click();
    > 32 |   await page.waitForResponse(r => r.url().includes('ListFolder'));
         |              ^
      33 |
      34 |   // contextmenu: topographic-sequoia.jpg2

⚠️  Captured trace is Xs old (test may have failed before capture)

═══════════════════════════════════════════════════════════════
```

## Part 5: Fix and Confirm

1. Revert the change (restore `/MoveFile`)
2. Re-run the test:
```bash
./trace-tools/trace.sh --run --test-name rename-demo ~/Downloads/baseline.json
```

**Expected output:**
```
═══════════════════════════════════════════════════════════════
                    REGRESSION TEST REPORT
═══════════════════════════════════════════════════════════════

✅ Test PASSED - Journey completed successfully

✓ Traces match semantically

Before:
  APIs: GET /ListFolder, POST /MoveFile
  Form submits: 2 (topographic-sequoia.jpg2 → topographic-sequoia.jpg)

After:
  APIs: GET /ListFolder, POST /MoveFile
  Form submits: 2 (topographic-sequoia.jpg2 → topographic-sequoia.jpg)

═══════════════════════════════════════════════════════════════
```

---

## CLI Reference

```bash
# View trace summary
./trace-tools/trace.sh ~/Downloads/baseline.json

# View with journey details
./trace-tools/trace.sh --show-journey ~/Downloads/baseline.json

# Generate Playwright test without running
./trace-tools/trace.sh --playwright ~/Downloads/baseline.json

# Generate and run Playwright test
./trace-tools/trace.sh --run ~/Downloads/baseline.json

# Specify test name
./trace-tools/trace.sh --run --test-name my-journey ~/Downloads/baseline.json

# Compare two traces step-by-step
./trace-tools/trace.sh --compare-raw before.json after.json

# Compare two traces by outcomes (APIs, forms)
./trace-tools/trace.sh --compare-semantic before.json after.json

# Show journey in semantic comparison
./trace-tools/trace.sh --compare-semantic --show-journey before.json after.json
```

## How It Works

1. **Capture**: The XMLUI Inspector records all component renders, events, and API calls to `window._xsLogs`
2. **Export**: JSON export serializes the trace for offline analysis
3. **Normalize**: `normalize-trace.js` extracts semantic steps (clicks, form submits, navigation)
4. **Generate**: `generate-playwright.js` creates a Playwright test that replays the journey
5. **Run**: Playwright executes the test and captures a new trace
6. **Compare**: `compare-traces.js` compares baseline and captured traces to detect differences
