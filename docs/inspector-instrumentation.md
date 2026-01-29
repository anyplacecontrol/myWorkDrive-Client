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

## Recommendations

Medium Priority:
- FileUpload/Download - Add progress events
- TimedAction - Capture and restore trace context for timers

Lower Priority:
- Form submission - Specific form events (may be covered by handler tracing)

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
