// --- Processes a single queued item for paste (copy/cut)
function onProcessQueuedItem(eventArgs) {
  const item = eventArgs.item;

  // Simulate paste operation with delay instead of actual API call
  console.log(`Simulating ${clipboardData.action} for item:`, item.name);
  return Actions.delay(500);
}

// --- Handles modal close
function handleClose() {
  if (!isFileOperationInProgress) isDialogOpen = false;
  return !isFileOperationInProgress;
}

// --- Called when paste queue completes all items
function onPasteComplete() {
  try {
    const action = clipboardData && clipboardData.action === "cut" ? "moved" : "copied";

    // Clear clipboard if action was 'cut' (move operation)
    if (clipboardData && clipboardData.action === "cut") {
        clearFileClipboard();
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

  // Read clipboard directly from reactive global `fileClipboard`
  const clipboard = fileClipboard;

  // Validate clipboard
  if (!clipboard || !Array.isArray(clipboard.items) || clipboard.items.length === 0) {
    toast.error("Nothing to paste");
    return;
  }
  if (clipboard.action !== "copy" && clipboard.action !== "cut") {
    toast.error("Invalid clipboard action");
    return;
  }

  // Compute destination path: use targetPath from message if provided, otherwise use current drive/folder
  let destPath;
  if (msg.targetPath) {
    destPath = msg.targetPath;
  } else {
    const drive = getCurrentDrive();
    const folder = getCurrentFolder();
    destPath = window.MwdHelpers.joinPath(drive, folder);
  }
  const pathAfterCopying = window.MwdHelpers.joinPath(destPath, "someFile");

  // Validate destination folder is valid for file operations
  if (!window.MwdHelpers.validateFileOperation(pathAfterCopying)) {
    const targetName = window.MwdHelpers.getFileName(pathAfterCopying);
    toast.error(`Cannot paste into the target folder: ${targetName}`);
    isDialogOpen = false;

    return;
  }

  // Prevent pasting into the same folder as the first item
  const firstItemPath = clipboard.items[0] && clipboard.items[0].path;
  if (firstItemPath && destPath && firstItemPath.includes(destPath)) {
    const destName = window.MwdHelpers.getFileName(destPath);
    toast.error(`Cannot paste into the same folder you copied from: ${destName}`);
    isDialogOpen = false;
    return;
  }

  // Ask user for confirmation before pasting (ENGLISH) — use global formatter
  const itemsDescription =
     window.MwdHelpers.formatItemsSummary(clipboard.items);

  const actionText = clipboard.action === "cut" ? "Move" : "Copy";
  const folderName = window.MwdHelpers.getFileName(destPath) || destPath;

  const userConfirmed = confirm(
    "Paste Confirmation",
    "Do you want to " + actionText + " " + itemsDescription + " to \"" + folderName + "\"?",
    actionText
  );
  if (!userConfirmed) {
    return;
  }

  // Initialize and open dialog when clipboard is valid
  clipboardData = clipboard;
  isDialogOpen = true;

  // Auto-start the paste queue
  isFileOperationInProgress = true;
  pasteQueue.enqueueItems(clipboard.items);
}
