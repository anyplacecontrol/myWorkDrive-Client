// --- Processes a single queued item for paste (copy/cut)
function onProcessQueuedItem(eventArgs) {
  const item = eventArgs.item;

  // Simulate paste operation with delay instead of actual API call
  console.log(`Simulating ${clipboardData.action} for item:`, item.name);
  return Actions.delay(500);
}

// --- Called when paste queue completes all items
function onPasteComplete() {
  try {
    const action = clipboardData && clipboardData.action === "cut" ? "moved" : "copied";

    // Clear clipboard if action was 'cut' (move operation)
    if (clipboardData && clipboardData.action === "cut") {
      AppState.set("fileClipboard", null);
    }

    // Refresh files list after paste
    window.publishTopic("FilesContainer:refresh");
  } finally {
    isDialogOpen = false;
    isFileOperationInProgress = false;
  }
}

// --- Handles message from MessageListener (moved to XS)
function onPasteMessageReceived(msg) {
  if (!msg || msg.type !== "PasteItemsModal:open") return;

  // Ensure UI state
  isFileOperationInProgress = false;

  // Read clipboard from AppState (source of truth)
  const clipboard = AppState.get("fileClipboard");

  // Validate clipboard
  if (!clipboard || !Array.isArray(clipboard.items) || clipboard.items.length === 0) {
    toast.error("Nothing to paste");
    return;
  }
  if (clipboard.action !== "copy" && clipboard.action !== "cut") {
    toast.error("Invalid clipboard action");
    return;
  }

  // Compute destination path from current drive/folder
  const drive = getCurrentDrive();
  const folder = getCurrentFolder();
  const destPath = window.MwdHelpers.joinPath(drive, folder);
  const pathAfterCopying = window.MwdHelpers.joinPath(destPath, "someFile");

  // Validate destination folder is valid for file operations
  if (!window.MwdHelpers.validateFileOperation(pathAfterCopying)) {
    toast.error(`Cannot paste into the current folder`);
    isDialogOpen = false;

    return;
  }

  // Prevent pasting into the same folder as the first item
  const firstItemPath = clipboard.items[0] && clipboard.items[0].path;
  if (firstItemPath && destPath && firstItemPath.includes(destPath)) {
    toast.error("Cannot paste into the same folder");
    isDialogOpen = false;
    return;
  }

  // Initialize and open dialog when clipboard is valid
  clipboardData = clipboard;
  isDialogOpen = true;

  // Auto-start the paste queue
  isFileOperationInProgress = true;
  pasteQueue.enqueueItems(clipboard.items);
}
