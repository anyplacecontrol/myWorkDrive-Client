// --- Executes the paste operation for a single item
function doPaste({ item, conflictBehavior }) {
  // Use Move endpoints for cut (move) operations, Copy endpoints for copy operations
  const url = (fileClipboard.action === "cut")
    ? (item.isFolder ? "/MoveFolder" : "/MoveFile")
    : (item.isFolder ? "/CopyFolder" : "/CopyFile");

  const response = Actions.callApi({
    url,
    method: "post",
    body: {
      path: item.path,
      newPath: item.newPath,
      conflictBehavior: conflictBehavior || "fail",
    },
  });

  // Update item if API returned new name/path (e.g., when renamed due to conflict)
  if (response && response.name) {
    const itemToUpdate = itemsToPaste.find((entry) => entry.path === item.path);
    if (itemToUpdate) {
      if (response.path) {
        itemToUpdate.newPath = response.path; //overwrite with actual new path from API
      }
    }
  }

  return response;
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
      const unsuccessfulItem = itemsToPaste.find((entry) => entry.path === item.path);

      if (unsuccessfulItem && !showErrorToast) {
        //skipped by user
        unsuccessfulItem.isSkipped = true;
      }

      if (showErrorToast) {
        //failed due to error
         if (unsuccessfulItem)
          unsuccessfulItem.isFailed = true;
        toast.error(`Failed to ${fileClipboard.action === "cut" ? "move" : "copy"} "${item.name}"`);
      }
    }
  }
}

// --- Handles modal close
function handleClose() {
  if (!isFileOperationInProgress) isDialogOpen = false;
  return !isFileOperationInProgress;
}

// --- Called when paste queue completes all items
function onPasteComplete() {
  try {
    // Refresh files list after paste
    window.publishTopic("FilesContainer:refresh");

    const allItems = itemsToPaste || [];
    const failedItems = allItems.filter((item) => item.isFailed);
    const skippedItems = allItems.filter((item) => item.isSkipped);
    const failedCount = failedItems.length;
    const skippedCount = skippedItems.length;
    const successCount = allItems.length - failedCount - skippedCount;

    // Build single toast message with optional parts (will be just the pasted count when no failures/skips)
    const parts = [`Pasted ${successCount} item(s)`];
    if (failedCount > 0) parts.push(`${failedCount} failed`);
    if (skippedCount > 0) parts.push(`${skippedCount} skipped`);
    toast.success(parts.join(", ") + ".");

    const failedFolderPaths = failedItems
      .filter((item) => item.isFolder)
      .map((item) => item.path);

    // Collapse failed Moved folders in tree (not skipped, only failed)
    if (failedFolderPaths.length > 0 && fileClipboard.action === "cut") {
      window.publishTopic("FoldersTree:collapse", { paths: failedFolderPaths });
    }

    // Find successfully created new folders
    const createdFolders = allItems.filter(
      (item) => item.isFolder && !item.isFailed && !item.isSkipped
    );

    const createdPaths = createdFolders.map((folder) => folder.newPath);
    const deletedPaths = createdFolders.map((folder) => folder.path);

    //Delete Successfully moved folders from tree (only for cut operation)
    if (fileClipboard.action === "cut" && deletedPaths.length > 0) {
      window.publishTopic("FoldersTree:delete", { paths: deletedPaths });
    }

    // Insert successfully created folders into tree
    if (createdPaths.length > 0 ) {
      const parentFolder = window.MwdHelpers.getParentFolder(createdPaths[0]);
      const newNames = createdPaths.map((path) => window.MwdHelpers.getFileName(path));
      window.publishTopic("FoldersTree:insert", { parentFolder, names: newNames });
    }

  } finally {
    // Clear clipboard if action was 'cut' (move operation)
    if (fileClipboard.action === "cut") {
      clearFileClipboard();
    }
    isDialogOpen = false;
    isFileOperationInProgress = false;
  }
}


function onPasteMessageReceived(msg) {
  if (!msg || msg.type !== "PasteItemsModal:open") return;

  // Ensure UI state
  isFileOperationInProgress = false;

  // Validate clipboard
  if (
    !fileClipboard ||
    !Array.isArray(fileClipboard.items) ||
    fileClipboard.items.length === 0
  ) {
    toast.error("Nothing to paste");
    return;
  }
  if (fileClipboard.action !== "copy" && fileClipboard.action !== "cut") {
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
  const invalidSource = fileClipboard.items.find(
    (item) => item.path && destPath && pathIsParent(item.path, destPath)
  );

  if (invalidSource) {
    const sourceName = window.MwdHelpers.getFileName(invalidSource.path);
    toast.error(`Parent folder "${sourceName}" cannot be pasted into its child subfolder.`);
    return;
  }

  // Prevent pasting into the same folder as the first item (segment-aware)
  const firstItemPath = fileClipboard.items[0] && fileClipboard.items[0].path;
  if (firstItemPath && destPath) {
    const firstParent = getParentFolder(firstItemPath);
    if (firstParent && normalizePath(destPath) === firstParent) {
      const destName = window.MwdHelpers.getFileName(destPath);
      toast.error(`Cannot paste into the same folder you copied from: ${destName}`);
      return;
    }
  }

  // Ask user for confirmation before pasting (ENGLISH) — use global formatter
  const itemsDescription = window.MwdHelpers.formatItemsSummary(fileClipboard.items);

  const actionText = fileClipboard.action === "cut" ? "Move" : "Copy";
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
  isFileOperationInProgress = true;
  itemsToPaste = fileClipboard.items.map((item) =>
    Object.assign({}, item, {
      newPath: window.MwdHelpers.joinPath(destPath, item.name),
      isFailed: false,
      isSkipped: false,
    })
  );

  pasteQueue.enqueueItems(itemsToPaste);
}
