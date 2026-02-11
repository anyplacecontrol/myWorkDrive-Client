// --- Executes the paste operation for a single item
function doPaste({ item, conflictBehavior }) {
  const url = item.isFolder ? "/CopyFolder" : "/CopyFile";
  Actions.callApi({
    url,
    method: "post",
    body: {
      path: item.path,
      newPath: item.newPath,
      conflictBehavior: conflictBehavior || "fail",
    },
  });
}

// --- Displays a confirmation dialog when there is a conflict
function confirmConflict(title, message) {
  return confirm({
    title,
    message,
    buttons: [
      {
        label: "Keep both",
        value: "rename",
        themeColor: "primary",
      },
      {
        label: "Replace",
        value: "replace",
        themeColor: "secondary",
        variant: "outlined",
      },
    ],
  });
}

// --- Processes a single queued item for paste (copy/cut)
function onProcessQueuedItem(eventArgs) {
  const item = eventArgs.item;

  try {
    doPaste({ item, conflictBehavior: item.conflictBehavior || "fail" });
  } catch (error) {
    let operationSucceeded = false;
    let showErrorToast = true;

    // Handle 409 error (conflict) with user confirmation
    if (error.statusCode === 409) {
      const result = confirmConflict(
        "Conflict",
        `${item.isFolder ? "Folder" : "File"} "${item.name}" already exists\nChoose how to handle the conflict`
      );
      if (result) {
        try {
          doPaste({ item, conflictBehavior: result });
          operationSucceeded = true;
        } catch (retryError) {
          // Fall through to error handling below
        }
      } else showErrorToast = false; // User cancelled: do not show error toast
    }

    if (!operationSucceeded) {
      // Track failed item and show toast
      const failedItem = itemsToPaste.find((entry) => entry.path === item.path);
      if (failedItem) {
        failedItem.isFailed = true;
      }
      if (showErrorToast) {
        toast.error(`Failed to ${item.action} "${item.name}"`);
      }
    }
  }
}

// --- Handles modal close
function handleClose() {
  if (!gIsFileOperationInProgress) isDialogOpen = false;
  return !gIsFileOperationInProgress;
}

// --- Called when paste queue completes all items
function onPasteComplete() {
  try {
    const action = gFileClipboard.action === "cut" ? "moved" : "copied";

    // Clear clipboard if action was 'cut' (move operation)
    if (gFileClipboard.action === "cut") {
      gClearFileClipboard();
    }

    // Refresh files list after paste
    window.publishTopic("FilesContainer:refresh");

    const allItems = itemsToPaste || [];
    const failedItems = allItems.filter((item) => item.isFailed);
    const failedCount = failedItems.length;
    const successCount = allItems.length - failedCount;

    if (failedCount > 0) {
      toast.success(`Pasted ${successCount} item(s), ${failedCount} failed/skipped.`);
    } else {
      toast.success(`Pasted ${successCount} item(s).`);
    }

  } finally {
    isDialogOpen = false;
    gIsFileOperationInProgress = false;
  }
}

// --- Handles message from MessageListener (moved to XS)
function onPasteMessageReceived(msg) {
  if (!msg || msg.type !== "PasteItemsModal:open") return;

  // Ensure UI state
  gIsFileOperationInProgress = false;

  // Validate clipboard
  if (
    !gFileClipboard ||
    !Array.isArray(gFileClipboard.items) ||
    gFileClipboard.items.length === 0
  ) {
    toast.error("Nothing to paste");
    return;
  }
  if (gFileClipboard.action !== "copy" && gFileClipboard.action !== "cut") {
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

  // --- Path helpers to compare path segments safely
  function normalizePath(p) {
    if (!p) return "";
    // Convert backslashes to slashes, trim, remove trailing slash
    let s = String(p).replace(/\\/g, "/").trim();
    if (s.endsWith("/")) s = s.replace(/\/+$/g, "");
    return s;
  }

  function getPathSegments(p) {
    const s = normalizePath(p);
    return s === "" ? [] : s.split("/").filter(Boolean);
  }

  function pathIsParent(parent, child) {
    // Returns true if `child` is strictly inside `parent` (not equal)
    const parentSeg = getPathSegments(parent);
    const childSeg = getPathSegments(child);
    if (parentSeg.length === 0 || childSeg.length <= parentSeg.length) return false;
    for (let i = 0; i < parentSeg.length; i++) {
      if (parentSeg[i] !== childSeg[i]) return false;
    }
    return true;
  }

  function getParentFolder(p) {
    const seg = getPathSegments(p);
    if (seg.length <= 1) return "";
    return seg.slice(0, seg.length - 1).join("/");
  }

  // Validate destination folder is valid for file operations
  if (!window.MwdHelpers.validateFileOperation(pathAfterCopying)) {
    const targetName = window.MwdHelpers.getFileName(pathAfterCopying);
    toast.error(`Cannot paste into the target folder: ${targetName}`);
    return;
  }

  // Prevent pasting a folder into its own subfolder (use segment-aware comparison)
  const invalidSource = gFileClipboard.items.find(
    (item) => item.path && destPath && pathIsParent(item.path, destPath)
  );

  if (invalidSource) {
    const sourceName = window.MwdHelpers.getFileName(invalidSource.path);
    toast.error(`Parent folder "${sourceName}" cannot be pasted into its child subfolder.`);
    return;
  }

  // Prevent pasting into the same folder as the first item (segment-aware)
  const firstItemPath = gFileClipboard.items[0] && gFileClipboard.items[0].path;
  if (firstItemPath && destPath) {
    const firstParent = getParentFolder(firstItemPath);
    if (firstParent && normalizePath(destPath) === firstParent) {
      const destName = window.MwdHelpers.getFileName(destPath);
      toast.error(`Cannot paste into the same folder you copied from: ${destName}`);
      return;
    }
  }

  // Ask user for confirmation before pasting (ENGLISH) — use global formatter
  const itemsDescription = window.MwdHelpers.formatItemsSummary(gFileClipboard.items);

  const actionText = gFileClipboard.action === "cut" ? "Move" : "Copy";
  const folderName = window.MwdHelpers.getFileName(destPath) || destPath;

  const userConfirmed = confirm(
    "Paste Confirmation",
    "Do you want to " + actionText + " " + itemsDescription + ' to "' + folderName + '"?',
    actionText
  );
  if (!userConfirmed) {
    return;
  }

  // Initialize and open dialog when clipboard is valid
  isDialogOpen = true;

  // Auto-start the paste queue
  gIsFileOperationInProgress = true;
  itemsToPaste = gFileClipboard.items.map((item) =>
    Object.assign({}, item, {
      newPath: window.MwdHelpers.joinPath(destPath, item.name),
      action: gFileClipboard.action,
      isFailed: false,
    })
  );

  pasteQueue.enqueueItems(itemsToPaste);
}
