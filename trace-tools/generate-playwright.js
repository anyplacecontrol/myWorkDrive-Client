/**
 * Generate Playwright test from normalized trace
 */

const { parseTrace } = require('./parse-trace');
const { normalizeTrace } = require('./normalize-trace');

function generatePlaywright(normalized, options = {}) {
  const { testName = 'user-journey', baseUrl = '/', captureTrace = true } = options;

  const lines = [
    `import { test, expect } from '@playwright/test';`,
    `import * as fs from 'fs';`,
    ``,
    `test('${testName}', async ({ page }) => {`,
  ];

  // Ensure startup step comes first
  const startupStep = normalized.steps.find(s => s.action === 'startup');
  const otherSteps = normalized.steps.filter(s => s.action !== 'startup');
  const orderedSteps = startupStep ? [startupStep, ...otherSteps] : otherSteps;

  for (const step of orderedSteps) {
    lines.push('');
    lines.push(...generateStepCode(step));
  }

  lines.push(`});`);

  // Wrap the test body in try/finally to capture trace even on failure
  if (captureTrace) {
    // Find the test body start and wrap it
    const testStart = lines.findIndex(l => l.includes("test('"));
    const testEnd = lines.length - 1;

    // Insert try after test opening
    lines.splice(testStart + 1, 0, '  try {');

    // Replace closing with finally block - handle browser already closed
    lines[lines.length - 1] = `  } finally {
    // Capture trace even on failure (if browser still open)
    try {
      await page.waitForTimeout(500);
      const logs = await page.evaluate(() => (window as any)._xsLogs || []);
      const traceFile = process.env.TRACE_OUTPUT || 'captured-trace.json';
      fs.writeFileSync(traceFile, JSON.stringify(logs, null, 2));
      console.log(\`Trace captured to \${traceFile} (\${logs.length} events)\`);
    } catch (e) {
      console.log('Could not capture trace (browser may have closed)');
    }
  }
});`;
  }

  return lines.join('\n');
}

function generateStepCode(step) {
  const lines = [];
  const indent = '  ';

  // Comment describing the step
  lines.push(`${indent}// ${step.action}: ${step.target?.label || step.target?.component || 'startup'}`);

  switch (step.action) {
    case 'startup':
      if (step.await?.api?.length > 0) {
        const firstApi = step.await.api[0];
        const endpoint = extractEndpointPath(firstApi);
        // Wait for initial data load by combining goto with response wait
        lines.push(`${indent}await Promise.all([`);
        lines.push(`${indent}  page.waitForResponse(r => r.url().includes('${endpoint}')),`);
        lines.push(`${indent}  page.goto('/my-files'),`);
        lines.push(`${indent}]);`);
      } else {
        lines.push(`${indent}await page.goto('/my-files');`);
      }
      break;

    case 'click':
      const clickLines = generateClickCode(step, indent);
      lines.push(...clickLines);
      if (clickLines._skipAwait) {
        return lines; // Skip await code for tree navigation
      }
      break;

    case 'contextmenu':
      lines.push(...generateContextMenuCode(step, indent));
      break;

    case 'dblclick':
      lines.push(...generateClickCode(step, indent, 'dblclick'));
      break;

    case 'keydown':
      // Skip keydown events - they represent typing but we don't capture the full text
      // The form submit will be captured separately
      lines.pop(); // Remove the comment we added
      return []; // Return empty to skip this step entirely

    default:
      lines.push(`${indent}// TODO: handle action "${step.action}"`);
  }

  // Add await conditions (skip for startup - already handled inline)
  if (step.await && step.action !== 'startup') {
    lines.push(...generateAwaitCode(step.await, indent));
  }

  return lines;
}

function generateClickCode(step, indent, method = 'click') {
  const lines = [];
  const selector = step.target?.selector;
  const testId = step.target?.testId;
  const label = step.target?.label;
  const component = step.target?.component;
  const targetTag = step.target?.targetTag;
  const selectorPath = step.target?.selectorPath;
  const formTestId = step.target?.formTestId;

  // For form button clicks, handle form data filling and button click
  if (targetTag === 'BUTTON' && (selectorPath || formTestId)) {
    const formData = step.target?.formData;
    const formId = formTestId || selectorPath?.match(/\[data-testid="([^"]+)"\]/)?.[1];

    // If we have form data, fill in the fields first
    if (formData && formId) {
      for (const [fieldName, fieldValue] of Object.entries(formData)) {
        if (typeof fieldValue === 'string') {
          // Use getByLabel to find inputs by their label
          const labelName = fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
          lines.push(`${indent}await page.getByTestId('${formId}').getByLabel('${labelName}').clear();`);
          lines.push(`${indent}await page.getByTestId('${formId}').getByLabel('${labelName}').fill('${fieldValue}');`);
        }
      }
    }

    // Now click the button
    if (selectorPath) {
      const match = selectorPath.match(/\[data-testid="([^"]+)"\]\s*>>\s*text=(.+)/);
      if (match) {
        lines.push(`${indent}await page.getByTestId('${match[1]}').getByRole('button', { name: '${match[2]}' }).${method}();`);
        return lines;
      }
    }
    if (formId && label) {
      lines.push(`${indent}await page.getByTestId('${formId}').getByRole('button', { name: '${label}' }).${method}();`);
      return lines;
    }
  }

  if (selector?.testId) {
    // Best option: use the unique testId from trace
    lines.push(`${indent}await page.getByTestId('${selector.testId}').${method}();`);
  } else if (testId) {
    lines.push(`${indent}await page.getByTestId('${testId}').${method}();`);
  } else if (selector?.role === 'treeitem' || (label && component === 'Tree')) {
    // Tree navigation is unreliable in Playwright - convert to file list dblclick
    // Skip 'Root' as it's the initial tree node, not a folder to navigate into
    const name = selector?.name || label;
    if (name && name !== 'Root') {
      lines.push(`${indent}await page.getByTestId('link-${name}').dblclick();`);
      lines.push(`${indent}await page.waitForResponse(r => r.url().includes('ListFolder')).catch(() => {});`);
    }
    // Return with special marker to skip additional await code
    lines._skipAwait = true;
    return lines;
  } else if (selector?.role === 'menuitem') {
    lines.push(`${indent}await page.getByRole('menuitem', { name: '${selector.name}' }).${method}();`);
  } else if (label) {
    // Check if this looks like a menu item (short label, DIV tag, after contextmenu)
    if (targetTag === 'DIV' && label.length < 20 && !label.includes(' ')) {
      // Likely a menu item
      lines.push(`${indent}await page.getByRole('menuitem', { name: '${label}' }).${method}();`);
    } else {
      // Try text-based selector
      lines.push(`${indent}await page.getByText('${label}').${method}();`);
    }
  } else if (component) {
    // Fallback to test id if available
    lines.push(`${indent}await page.getByTestId('${component}').${method}();`);
  } else {
    lines.push(`${indent}// TODO: need selector for ${method}`);
  }

  return lines;
}

function generateContextMenuCode(step, indent) {
  const lines = [];
  const testId = step.target?.testId;
  const label = step.target?.label;
  const component = step.target?.component;

  if (testId) {
    // Best option: use the unique testId
    lines.push(`${indent}await page.getByTestId('${testId}').click({ button: 'right' });`);
  } else if (label) {
    // Fallback: use text to find the element
    lines.push(`${indent}await page.getByText('${label}').click({ button: 'right' });`);
  } else if (step.target?.selectedPath) {
    // Use the path to construct a selector
    const name = step.target.selectedPath.split('/').pop();
    lines.push(`${indent}await page.getByText('${name}').click({ button: 'right' });`);
  } else if (component) {
    lines.push(`${indent}await page.getByTestId('${component}').click({ button: 'right' });`);
  } else {
    lines.push(`${indent}// TODO: need selector for contextmenu`);
  }

  return lines;
}

function generateAwaitCode(awaitConditions, indent) {
  const lines = [];

  // Wait for navigation
  if (awaitConditions.navigate) {
    const to = awaitConditions.navigate.to;
    // Extract meaningful part of URL for matching
    const folderMatch = to.match(/folder=([^&]+)/);
    if (folderMatch) {
      const folder = decodeURIComponent(folderMatch[1]);
      lines.push(`${indent}await page.waitForURL('**/*folder=${encodeURIComponent(folder)}*');`);
    }
  }

  // Wait for API calls (just the first significant one to avoid over-waiting)
  if (awaitConditions.api?.length > 0) {
    const api = awaitConditions.api.find(a => a.method === 'GET' || a.method === 'POST') || awaitConditions.api[0];
    if (api) {
      const path = extractEndpointPath(api.endpoint || api);
      lines.push(`${indent}await page.waitForResponse(r => r.url().includes('${path}'));`);
    }
  }

  return lines;
}

function extractEndpointPath(endpoint) {
  if (typeof endpoint === 'string') {
    // Remove query params for matching
    return endpoint.split('?')[0].replace(/^\//, '');
  }
  return 'ListFolder'; // default
}

// Export
if (typeof module !== 'undefined') {
  module.exports = { generatePlaywright };
}

// CLI usage
if (require.main === module) {
  const fs = require('fs');
  const { normalizeJsonLogs } = require('./normalize-trace');

  const inputFile = process.argv[2] || '/dev/stdin';
  const testName = process.argv[3] || 'user-journey';
  const input = fs.readFileSync(inputFile, 'utf8');

  let normalized;

  // Detect JSON vs text format
  if (input.trim().startsWith('[') || input.trim().startsWith('{')) {
    // JSON format - use normalizeJsonLogs
    const logs = JSON.parse(input);
    normalized = normalizeJsonLogs(logs);
  } else {
    // Text format - use parseTrace + normalizeTrace
    const parsed = parseTrace(input);
    normalized = normalizeTrace(parsed);
  }

  const playwright = generatePlaywright(normalized, { testName });
  console.log(playwright);
}
