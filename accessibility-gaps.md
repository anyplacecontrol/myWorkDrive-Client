# XMLUI Accessibility Gaps

## Background

While generalizing trace-tools to auto-generate Playwright tests across multiple XMLUI apps, we discovered that test generation quality depends directly on the accessibility of the rendered DOM.

### What we did

We built a pipeline: capture a user journey trace from the XMLUI inspector, generate a Playwright test from it, and replay the test to verify the journey still works after refactoring. The generated tests need reliable selectors to find and interact with elements.

We started with `data-testid` attributes (manually added by developers in myWorkDrive-Client), but this approach doesn't generalize — it requires every app developer to know about and add testIds. Instead, we enhanced the XMLUI framework to capture ARIA role and accessible name at interaction time (in `AppContent.tsx`). The generator now uses:

- `getByRole(role, { name })` when ARIA info is available (buttons, links, checkboxes, etc.)
- `getByLabel(name)` for form field filling (from submit handler args)
- `getByText(label, { exact: true })` as fallback when no ARIA role exists

This works well for elements with proper semantics — buttons, labeled inputs, links. Where it breaks down is exactly where accessibility breaks down: elements without roles, names, or labels.

### What we learned

The trace-tools pipeline surfaces accessibility gaps automatically. When the generator emits `// ACCESSIBILITY GAP:` instead of a working selector, it means a screen reader would have the same problem finding that element. Fixing these gaps improves both testability and accessibility.

### Auth handling

Apps that require login are handled via `trace-tools/app-config.json` and a Playwright auth setup project (`auth-setup.ts`). The config describes the login form fields and submit button; the setup project runs before tests and saves browser state. Generated tests don't contain any auth logic. For apps without login (like myWorkDrive-Client), omit the config file and the defaults apply.

### Scope

We are testing this approach across multiple XMLUI apps. We expect to find similar gaps in xmlui-mastodon and others as we expand coverage.

---

## Gaps found

### 1. SVG icon buttons without accessible names

**Apps affected:** core-ssh-server-ui, likely others

**Problem:** Clickable SVG icons (`<svg>`, `<path>`) have no `aria-label` or `role="button"`. The trace captures `targetTag: "svg"` or `targetTag: "path"` with no ariaRole or ariaName.

**Impact:** Screen readers cannot identify these controls. Test generator cannot produce a selector.

**Fix:** Add `aria-label` to icon buttons describing their action (e.g., `aria-label="Close"`, `aria-label="Settings"`). If the icon is inside a `<button>`, the button's text content or aria-label is sufficient.

### 2. Navigation links rendered as plain DIVs

**Apps affected:** core-ssh-server-ui

**Problem:** Navigation items like "USERS", "SETTINGS" render as `<div>` elements instead of `<a>` with `href`. They have no implicit ARIA role.

**Impact:** Screen readers don't identify these as navigation links. Test generator falls back to `getByText` which works but is less precise than `getByRole('link', { name })`.

**Fix:** Use `<a>` elements (or `role="link"`) for navigation. XMLUI's `NavLink` component should render with proper link semantics.

### 3. Tree nodes without treeitem role

**Apps affected:** myWorkDrive-Client

**Problem:** Tree nodes render as `<div>` with text content but no `role="treeitem"`. When the same text appears elsewhere (e.g., "Root" in both the tree and breadcrumb), `getByText` matches multiple elements.

**Impact:** Screen readers cannot navigate the tree structure. Test generator produces ambiguous selectors.

**Fix:** Tree component should render nodes with `role="treeitem"` within a container with `role="tree"`. This is the standard WAI-ARIA tree pattern.

### 4. Table row selection without accessible checkboxes

**Apps affected:** core-ssh-server-ui

**Problem:** Clicking a table cell (`<td>`) to select a row provides no ARIA context about what is being selected. The trace captures `targetTag: "TD"` with no role or name.

**Impact:** Screen readers cannot determine what selecting a row means. Test generator cannot produce a selector.

**Fix:** Row selection should use a checkbox (`<input type="checkbox">`) with an accessible name identifying the row (e.g., `aria-label="Select user elvis"`).

---

## Adding new gaps

As we test more apps (xmlui-mastodon, etc.), add entries here following the pattern above. Each gap should include: apps affected, the problem (what the trace shows), the impact, and the fix.
