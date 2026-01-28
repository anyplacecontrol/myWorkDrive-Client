# XMLUI Inspector

The XMLUI Inspector captures the complete flow of user interactions through your application, showing how events trigger handlers, API calls, state changes, and navigation.

| Feature | What It Shows |
|---------|---------------|
| **Interaction tracing** | Every click, keypress captured with component context |
| **Handler lifecycle** | `handler:start` → work → `handler:complete` |
| **API call tracing** | `api:start` → `api:complete` or `api:error` |
| **State change diffs** | Before/after with smart array diffing |
| **Navigation tracking** | Route changes with from/to paths |
| **Error capture** | Full error message and stack trace |
| **XMLUI source linking** | Click to see the exact component definition |
| **Trace correlation** | All events in a user action share a traceId |

Code: See the judell/inspector branches of xmlui-org/xmlui and judell/myWorkDrive-Client.


Here's how that looks when running/debugging the `myWorkDrive-Client` app.

## 1. Initial Load

When the app starts, the inspector captures DataSource initialization and the initial data fetch.

**What you see:** The file catalog loads its initial folder contents.

<img width="896" height="834" alt="image" src="https://gist.github.com/user-attachments/assets/c3666802-89ff-497a-ae3f-ce3da5666cf1" />

<img width="878" height="774" alt="image" src="https://gist.github.com/user-attachments/assets/0a3f9c9b-9e96-44af-a7be-13561e7ee6c0" />


<details>
<summary>View Trace</summary>

```
=== XMLUI Inspector Export ===
Total events: 7

--- Trace 1: Startup ---
    traceId: startup-mkun3tv4
    diffs: 7, error: false
    [state:changes] AppState:fileCatalogView (perfTs 436.7)
        AppState:fileCatalogView: undefined → {"view":"table","sortBy":"name","sortDirection":"ascending"}
    [state:changes] AppState:fileCatalogSelection (perfTs 476.3)
        AppState:fileCatalogSelection: undefined → {"selectedItems":[]}
    [state:changes] AppState:targetSelector (perfTs 476.3)
        AppState:targetSelector: undefined → {"view":"table","sortBy":"name","sortDirection":"ascending"}
    [state:changes] DataSource:fileCatalogData (perfTs 806.5 | instance ds-q5g7ya-mkun3txk | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: initialized with 2 items
            • 2025 10_15_2025-document(2).pdf (839.9 KB)
            • baz
    [state:changes] DataSource:fileCatalogData (perfTs 831.5 | instance ds-vfzwuu-mkun3txt | url /ListFolder | uid fileCatalogData | file /src/components/fileCatalog/TargetSelectorModal.xmlui)
        source 649-1049
        DataSource:fileCatalogData: initialized with 8 items
            • this-is-a-ridiculously-long-name-seriously-who-does-that.html (4.2 KB)
            • xmlui-invoice.zip (35.3 MB)
            • chase_txns.csv (88.1 KB)
            • test.xlsx (8.1 KB)
            • 2025 09_15_2025-document.pdf (851.4 KB)
            … and 3 more
    [dedupe] 2 repeated state changes removed

--- Notes ---
1. DataSource identity reused across components. These are independent instances that share the same id/uid.
    id/uid: fileCatalogData
        - instance ds-q5g7ya-mkun3txk, url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')}, uid fileCatalogData, file /src/components/fileCatalog/FileCatalog.xmlui, source: 710-1118
        - instance ds-vfzwuu-mkun3txt, url /ListFolder, uid fileCatalogData, file /src/components/fileCatalog/TargetSelectorModal.xmlui, source: 649-1049
        - instance ds-su8skc-mkun3txu, url /ListFolder, uid fileCatalogData, file /src/components/fileCatalog/TargetSelectorModal.xmlui, source: 649-1049

--- Source files (index → path) ---
    [0] /src/Main.xmlui
    [1] /src/components/MwdFooter.xmlui
    [2] /src/components/MwdTitleLink.xmlui
    [3] /src/components/MyFiles.xmlui
    [4] /src/components/SafeLink/SafeLink.xmlui
    [5] /src/components/fileCatalog/ClearSelectionButton.xmlui
    [6] /src/components/fileCatalog/CreateFolderModal.xmlui
    [7] /src/components/fileCatalog/FileCatalog.xmlui
    [8] /src/components/fileCatalog/FileCatalogHeader.xmlui
    [9] /src/components/fileCatalog/FileUploadProgressIndicator.xmlui
    [10] /src/components/fileCatalog/Files.xmlui
    [11] /src/components/fileCatalog/FolderBreadcrumbs.xmlui
    [12] /src/components/fileCatalog/FoldersTree.xmlui
    [13] /src/components/fileCatalog/RenameItemModal.xmlui
    [14] /src/components/fileCatalog/SortingMenuItem.xmlui
    [15] /src/components/fileCatalog/TargetSelectorModal.xmlui
    [16] /src/components/folderView/EmptyDataView.xmlui
    [17] /src/components/folderView/TableView.xmlui
    [18] /src/components/folderView/TileItem.xmlui
    [19] /src/components/folderView/TilesView.xmlui
    [20] /src/components/preview/FileContentModal.xmlui
    [21] /src/components/preview/FilePreview.xmlui
    [22] /src/components/preview/UnknownFilePreview.xmlui
```

</details>

---

## 2. Expand Folder in Tree

Clicking a folder node in the tree triggers lazy loading of its children.

**What you see:** The tree expands to show subfolders, with the API call and state update clearly traced.

<img width="887" height="835" alt="image" src="https://gist.github.com/user-attachments/assets/e2efe263-e8e4-4971-baaa-c2221f146843" />

<img width="862" height="694" alt="image" src="https://gist.github.com/user-attachments/assets/4238a054-00b2-4dd1-88b2-f67dc1b69b4a" />


<details>
<summary>View Trace</summary>

```
--- Trace 1: Tree "tree" loadChildren ---
    traceId: i-mkun68tm-n746wp
    diffs: 1, error: false
    [interaction] click "HStack" (perfTs 113029.4)
    [handler:start] loadChildren "tree" (perfTs 113030.4)
    [api:start] get /ListFolder?path=:sh:Documents:/ (perfTs 113033.7)
    [api:complete] get /ListFolder?path=:sh:Documents:/ (perfTs 113227.5)
    [state:changes] loadChildren "tree" (perfTs 113359.6)
        treeData.0.children: initialized with 1 items
            • foo
    [handler:complete] loadChildren "tree" (perfTs 113371.1)

--- Trace 2: Tree "tree" selectionDidChange ---
    traceId: startup-mkun3tv4
    diffs: 2, error: false
    [handler:start] selectionDidChange "tree" (perfTs 112923.0)
    [navigate] /my-files → /my-files?drive=%3Ash%3ADocuments%3A&folder=%2F (perfTs 112960.3)
    [handler:complete] selectionDidChange "tree" (perfTs 112963.4)
    [state:changes] DataSource:fileCatalogData (perfTs 112995.4 | instance ds-q5g7ya-mkun3txk | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: 2 items → cleared
    [state:changes] DataSource:fileCatalogData (perfTs 113070.5 | instance ds-q5g7ya-mkun3txk | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: initialized with 8 items
            • this-is-a-ridiculously-long-name-seriously-who-does-that.html (4.2 KB)
            • xmlui-invoice.zip (35.3 MB)
            • chase_txns.csv (88.1 KB)
            • test.xlsx (8.1 KB)
            • 2025 09_15_2025-document.pdf (851.4 KB)
            … and 3 more

```

</details>

---

## 3. Select Folder (Navigate)

Selecting a folder in the tree triggers navigation, updating the URL and refreshing the file list.

**What you see:** The URL updates with the folder path, and the main file list reloads.

<img width="861" height="841" alt="image" src="https://gist.github.com/user-attachments/assets/4cd21700-d88d-4b34-800c-b0a7b3b2278c" />

<details>
<summary>View Trace</summary>

```
--- Trace 3: Tree "tree" selectionDidChange ---
    traceId: startup-mkundm5g
    diffs: 10, error: false
    [state:changes] AppState:fileCatalogView (perfTs 397.7)
        AppState:fileCatalogView: undefined → {"view":"table","sortBy":"name","sortDirection":"ascending"}
    [state:changes] AppState:fileCatalogSelection (perfTs 432.2)
        AppState:fileCatalogSelection: undefined → {"selectedItems":[]}
    [state:changes] AppState:targetSelector (perfTs 432.2)
        AppState:targetSelector: undefined → {"view":"table","sortBy":"name","sortDirection":"ascending"}
    [state:changes] DataSource:fileCatalogData (perfTs 736.7 | instance ds-vsarzf-mkundm81 | url /ListFolder | uid fileCatalogData | file /src/components/fileCatalog/TargetSelectorModal.xmlui)
        source 649-1049
        DataSource:fileCatalogData: initialized with 8 items
            • this-is-a-ridiculously-long-name-seriously-who-does-that.html (4.2 KB)
            • xmlui-invoice.zip (35.3 MB)
            • chase_txns.csv (88.1 KB)
            • test.xlsx (8.1 KB)
            • 2025 09_15_2025-document.pdf (851.4 KB)
            … and 3 more
    [state:changes] DataSource:fileCatalogData (perfTs 739.7 | instance ds-m38o8b-mkundm7r | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: initialized with 2 items
            • bar
            • test-project
    [handler:start] selectionDidChange "tree" (perfTs 2524.2)
    [navigate] /my-files → /my-files?drive=%3Ash%3ADocuments%3A&folder=%2F (perfTs 2558.4)
    [handler:complete] selectionDidChange "tree" (perfTs 2560.2)
    [state:changes] DataSource:fileCatalogData (perfTs 2587.8 | instance ds-m38o8b-mkundm7r | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: 2 items → cleared
    [state:changes] DataSource:fileCatalogData (perfTs 2643.3 | instance ds-m38o8b-mkundm7r | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: initialized with 8 items
            • this-is-a-ridiculously-long-name-seriously-who-does-that.html (4.2 KB)
            • xmlui-invoice.zip (35.3 MB)
            • chase_txns.csv (88.1 KB)
            • test.xlsx (8.1 KB)
            • 2025 09_15_2025-document.pdf (851.4 KB)
            … and 3 more
    [handler:start] selectionDidChange "tree" (perfTs 18525.1)
    [navigate] /my-files → /my-files?drive=%3Ash%3ADocuments%3A&folder=%2Ffoo (perfTs 18549.4)
    [state:changes] DataSource:fileCatalogData (perfTs 18595.5 | instance ds-m38o8b-mkundm7r | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData: initialized with 2 items
            • bar
            • test-project
    [handler:complete] selectionDidChange "tree" (perfTs 18636.2)
    [dedupe] 2 repeated state changes removed
```

</details>

---

## 4. Create New Folder

Creating a folder involves a modal dialog, API call, and list refresh.

**What you see:** The create folder modal, the API call to create it, and the updated file list.

<img width="897" height="817" alt="image" src="https://gist.github.com/user-attachments/assets/a50f4429-3a09-4953-aa62-ce152f375c6d" />

<details>
<summary>View Trace</summary>

```
--- Trace 3: Form "createForm" submit ---
    traceId: i-mkunlykn-rwqzzq
    diffs: 1, error: false
    [interaction] click "Form" (perfTs 256959.2)
    [handler:start] submit "createForm" (perfTs 256967.6)
    [api:start] post /CreateFile (perfTs 256968.4)
    [api:complete] post /CreateFile (perfTs 256975.9)
    [handler:complete] submit "createForm" (perfTs 257022.9)
    [state:changes] DataSource:fileCatalogData (perfTs 257031.0 | instance ds-cyexgf-mkunggo5 | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData:
            4 → 5 items
            +1 added: test2
```

</details>

---

## 5. Upload File

File uploads show progress through the queue system with trace context preserved across async operations.

**What you see:** The upload progress indicator and the file appearing in the list.

<img width="894" height="829" alt="image" src="https://gist.github.com/user-attachments/assets/6a19f153-097a-4377-9e4f-5dd15caef731" />

<details>
<summary>View Trace</summary>

```
--- Trace: FileUpload ---

```

</details>

---

## 6. Delete Item

Deletion shows the confirmation dialog flow and the item removal from state.

<img width="897" height="987" alt="image" src="https://gist.github.com/user-attachments/assets/451bf6bc-889a-434f-82c4-654af6473d59" />

<details>
<summary>View Trace</summary>

```

--- Trace 1: Button "Delete" click ---
    traceId: i-mkuoae9s-yqqjm8
    diffs: 4, error: false
    [interaction] click "Delete" (perfTs 33883.9)
    [handler:start] click "Delete" (perfTs 33885.3)
    [handler:start] deleteTriggered (perfTs 33891.2)
    [modal:show] "Delete?" (perfTs 33917.5)
    [interaction] click "Delete" (perfTs 35716.9)
    [modal:confirm] (perfTs 35717.5)
    [handler:start] process "deleteQueue" (perfTs 35789.4)
    [handler:complete] deleteTriggered (perfTs 35799.7)
    [handler:complete] click "Delete" (perfTs 35799.9)
    [api:start] delete /DeleteFile (perfTs 35857.9)
    [api:complete] delete /DeleteFile (perfTs 35866.8)
    [state:changes] DataSource:fileCatalogData (perfTs 35926.2 | instance ds-9p8mft-mkuo9okv | url /ListFolder | uid fileCatalogData | file /src/components/fileCatalog/TargetSelectorModal.xmlui)
        source 649-1049
        DataSource:fileCatalogData:
            8 → 7 items
            −1 removed: chase_txns.csv (88.1 KB)
    [api:start] get /ListFolder (perfTs 35934.1)
    [state:changes] DataSource:fileCatalogData (perfTs 35936.7 | instance ds-dyt9zp-mkuo9okl | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData:
            8 → 7 items
            −1 removed: chase_txns.csv (88.1 KB)
    [state:changes] AppState:fileCatalogSelection (perfTs 36042.7)
        AppState:fileCatalogSelection: {"selectedItems":[],"selectedIds":[":sh:Documents:/chase_txns.csv"]} → {"selectedItems":[],"selectedIds":[]}
    [api:complete] get /ListFolder (perfTs 36204.1)
    [handler:complete] process "deleteQueue" (perfTs 36262.2)
    [dedupe] 1 repeated state change removed
```

</details>

---


## 7. Error Handling

When operations fail, the inspector captures the error with full context.

**What you see:** The error state in the UI and the traced error with stack trace and XMLUI source.

<img width="884" height="956" alt="image" src="https://gist.github.com/user-attachments/assets/2a0afb06-d3b4-4dd3-ad1a-940107789087" />


<details>
<summary>View Trace</summary>

```
--- Trace 1: Button "Download" click ---
    traceId: i-mkupvker-1wj97n
    diffs: 0, error: true
    [interaction] click "Download" (perfTs 4926.9)
    [handler:start] click "Download" (perfTs 4930.0)
    [handler:start] downloadTriggered (perfTs 4936.9)
    [handler:error] downloadTriggered - Error without message in /src/components/fileCatalog/FileCatalog.xmlui (perfTs 4938.5 | file /src/components/fileCatalog/FileCatalog.xmlui)
    [handler:error] click - Error without message in /src/components/fileCatalog/FileCatalogHeader.xmlui (perfTs 4948.2 | file /src/components/fileCatalog/FileCatalogHeader.xmlui)
```

</details>



## Investigating code smell

Let's look through the lens of XMLUI devs who are using this inspector. It can reveal some things they may want to investigate, and provide evidence for AIs to do the investigation. Give this to an LLM and ask about code smell.

```
=== XMLUI Inspector Export ===
Total events: 12

--- Trace 1: Button "Upload" click ---
    traceId: i-mku8256g-jkqv4s
    diffs: 3, error: false
    [interaction] click "Upload" (perfTs 44745.5)
    [handler:start] click "Upload" (perfTs 44746.8)
    [handler:start] uploadTriggered (perfTs 44752.7)
    [handler:complete] uploadTriggered (perfTs 44761.8)
    [handler:complete] click "Upload" (perfTs 44762.6)
    [handler:start] upload "fileUploadDialog" (perfTs 47814.8)
    [handler:complete] upload "fileUploadDialog" (perfTs 47875.8)
    [handler:start] process "uploadItemsQueue" (perfTs 47911.3)
    [state:changes] DataSource:fileCatalogData (perfTs 48218.4 | instance ds-p5w4wv-mku81cug | url /ListFolder?path={$props.drive}{defaultTo($props.folder, '/')} | uid fileCatalogData | file /src/components/fileCatalog/FileCatalog.xmlui)
        source 710-1118
        DataSource:fileCatalogData:
            8 → 9 items
            +1 added: 2025 09_15_2025-document.pdf (851.4 KB)
    [state:changes] DataSource:fileCatalogData (perfTs 48274.4 | instance ds-a138qs-mku81cus | url /ListFolder | uid fileCatalogData | file /src/components/fileCatalog/TargetSelectorModal.xmlui)
        source 649-1049
        DataSource:fileCatalogData:
            8 → 9 items
            +1 added: 2025 09_15_2025-document.pdf (851.4 KB)
    [handler:complete] process "uploadItemsQueue" (perfTs 48478.0)
    [dedupe] 1 repeated state change removed
```

**Claude:**

Looking at this trace as an XMLUI developer investigating potential code smells:

  #### Issues Identified

  ##### 1. Duplicate DataSource Instances with Same UID
  The inspector flagged **3 independent DataSource instances** all using
  `uid="fileCatalogData"`:

  | Instance | Location | URL |
  |----------|----------|-----|
  | ds-p5w4wv | FileCatalog.xmlui | `/ListFolder?path=...` |
  | ds-a138qs | TargetSelectorModal.xmlui | `/ListFolder` |
  | ds-akibck | TargetSelectorModal.xmlui | `/ListFolder` |

  **Problem**: Each makes its own API call to fetch the same folder listing. After upload,
  both FileCatalog's DataSource and TargetSelectorModal's DataSource refresh independently.

  ##### 2. Unnecessary Network Traffic
  During the upload flow, we see:
  - FileCatalog's DataSource updates (8 → 9 items)
  - TargetSelectorModal's DataSource also updates (8 → 9 items)

  But the TargetSelectorModal likely isn't even visible during upload. Why is it fetching
  data?

  ##### 3. Two TargetSelectorModal Instances
  There are **two** DataSource instances from TargetSelectorModal.xmlui (likely `copyModal`
  and `moveModal` in FileCatalog). Both maintain their own data copies even when not in use.

  #### Recommendations

  1. **Lift the DataSource** to FileCatalog and pass data down to TargetSelectorModal via
  props
  2. **Lazy-load modals** - TargetSelectorModal shouldn't fetch data until opened
  3. **Share state** - If multiple components need the same folder listing, use a single
  DataSource at a common ancestor

# Inspector Instrumentation: How It Works

## Current Instrumentation Points

Location: AppContent.tsx:645
What's Traced: User interactions (click, keydown, etc.)
Kind: interaction

Location: AppContent.tsx:446
What's Traced: AppState bucket changes
Kind: state:changes

Location: Container.tsx:256
What's Traced: Handler start/complete/error events
Kind: handler:start, handler:complete, handler:error

Location: Container.tsx:285
What's Traced: Handler error source file info
Kind: handler:error (includes ownerFileId, ownerSource)

Location: DataLoader.tsx:102
What's Traced: DataSource state changes with diffs
Kind: state:changes

Location: ComponentAdapter.tsx:369
What's Traced: Component-specific interactions (e.g., custom events)
Kind: interaction

Location: APICall.tsx
What's Traced: API call start/complete/error
Kind: api:start, api:complete, api:error

Location: NavigateAction.tsx
What's Traced: Route navigation events
Kind: navigate (includes from, to, queryParams)

Location: ErrorBoundary.tsx
What's Traced: React error boundary catches
Kind: error:boundary

Location: ConfirmationModalContextProvider.tsx
What's Traced: Confirmation dialog show/confirm/cancel
Kind: modal:show, modal:confirm, modal:cancel

## Trace Context Propagation

Location: FileUploadDropZoneNative.tsx
What It Does: Captures trace before async file picker, restores after

Location: QueueNative.tsx
What It Does: Captures trace when item enqueued, restores during processing

Location: ConfirmationModalContextProvider.tsx
What It Does: Preserves trace across confirm dialog via _xsPendingConfirmTrace

Location: Container.tsx (getOrCreateEventHandlerFn)
What It Does: Stores source file info in global registry for error attribution

## How to Enable/Disable

In your app's `config.ts`:

```typescript
appGlobals: {
  xsVerbose: true,   // Enable tracing
  // xsVerbose: false  // Disable tracing (default)
  xsVerboseLogMax: 200  // Optional: max log entries (default 200)
}
```

To disable: Set xsVerbose: false or remove the property entirely.

## How Invasive Is It?

When disabled (xsVerbose: false):
- Zero overhead - the tracing code short-circuits immediately
- No event listeners added
- No state change tracking

When enabled (xsVerbose: true):
- Adds document-level event listeners for user interactions (click, keydown, etc.)
- Intercepts state changes in DataSource and other state buckets
- Stores logs in window._xsLogs (in-memory only)
- No network calls, no localStorage, no external dependencies

### Performance Impact

Minimal when enabled:
- Event listener capture: ~microseconds per event
- State diff calculation: already happens for React rendering, just logs the result
- Memory: bounded by xsVerboseLogMax (default 200 entries, oldest purged)

### Production Recommendation

```typescript
appGlobals: {
  xsVerbose: process.env.NODE_ENV === 'development',
}
```

This enables tracing only in development builds.

## Enabling/Disabling Tracing in Standalone Apps

For standalone apps using `xmlui-standalone.umd.js`, configure `xsVerbose` in your
`config.json`:

### config.json

```json
{
  "name": "My App",
  "appGlobals": {
    "xsVerbose": true,
    "xsVerboseLogMax": 200
  }
}
```

### How Standalone Works

1. Buildless apps automatically load config.json from the app root
2. The appGlobals object is merged into the app's global properties
3. xsVerbose: true enables tracing; omit it or set to false to disable
4. xsVerboseLogMax limits in-memory log entries (default: 200)

### Enabling Only in Development

Since standalone apps don't have a build step, you can't use process.env.NODE_ENV. Options:

#### Option 1: Separate config files

```
myapp/
├── config.json           # Production (xsVerbose: false or omitted)
├── config.dev.json       # Development (xsVerbose: true)
└── Main.xmlui
```

Swap files when deploying.

#### Option 2: URL parameter check

The XMLUI framework could be extended to check for a URL parameter like ?debug=true, but
this isn't built-in currently.

### Performance When Disabled

When xsVerbose is false or omitted:
- No event listeners are added to the document
- No state change tracking occurs
- Zero runtime overhead

## Gaps - Areas Without Instrumentation

1. FileUpload/FileDownload Actions

   - No tracing for upload/download start/progress/complete
   - Impact: Missing visibility into file transfer operations

2. TimedAction (setTimeout/setInterval in handlers)

   - Timer callbacks lose trace context
   - Impact: Delayed operations appear orphaned in traces

3. Form Submission

   - No specific tracing for form submit events
   - Impact: Form workflows harder to trace

### Recommendations

Medium Priority:
- FileUpload/Download - Add progress events
- TimedAction - Capture and restore trace context for timers

Lower Priority:
- Form submission - Specific form events (may be covered by handler tracing)
