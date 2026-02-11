// --- Handles incoming message to open delete modal
function handleMessageReceived(msg) {
  if (msg && msg.type === 'DeleteItemsModal:open') {
    gIsFileOperationInProgress = false;
    const incoming = msg.payload || {};
    const items = incoming.items || [];

    // Validate all item paths before opening dialog
    for (const item of items) {
      if (!window.MwdHelpers.validateFileOperation(item.path)) {
        toast.error(`Unable to execute operation with: ${item.path}`);
        isDialogOpen = false;
        return;
      }
    }

    // Store items for later use in onDeleteComplete
    itemsToDelete = items.map((item) => Object.assign({}, item, { isFailed: false, isSkipped: false }));
    isDialogOpen = true;
  }
}

// --- Handles modal close
function handleClose() {
  if (!gIsFileOperationInProgress) isDialogOpen = false;
  return !gIsFileOperationInProgress;
}

// --- Handles the delete button click
function onDeleteClick() {
  gIsFileOperationInProgress = true;
  deleteQueue.enqueueItems(itemsToDelete || []);
}

// --- Executes the delete operation for a single item
function doDelete({ item, actionWhenNotEmpty }) {
  const url = item.isFolder ? "/DeleteFolder" : "/DeleteFile";
  const queryParams = {
    path: item.path,
  };

  // For folders, add recursive deletion parameters if specified
  if (item.isFolder && actionWhenNotEmpty) {
    queryParams.actionWhenNotEmpty = actionWhenNotEmpty;
  }

  Actions.callApi({
    url,
    method: "post",
    queryParams,
    invalidates: [],
  });
}

// --- Processes a single queued item for deletion
function onDeleteQueuedItem(eventArgs) {
  const item = eventArgs.item;

  try {
    doDelete({ item, actionWhenNotEmpty: item.actionWhenNotEmpty || null });
  } catch (error) {
    let operationSucceeded = false;
    let showErrorToast = true;

    // Handle 417 error (folder not empty) with user confirmation
    if (error && error.statusCode === 417) {
      const userConfirmed = confirm({
        title: "Delete non-empty folder?",
        message: `The folder "${item.name}" is not empty.`,
        buttons: [{ label: "Yes", value: true }]
      });
      if (userConfirmed) {
        try {
          doDelete({ item, actionWhenNotEmpty: "stopOnError" });
          operationSucceeded = true;
        } catch (retryError) {
          // Fall through to error handling below
        }
      } else {
        // User cancelled: mark skipped and do not show toast
        showErrorToast = false;
      }
    }

    if (!operationSucceeded) {
      const unsuccessfulItem = itemsToDelete.find((entry) => entry.path === item.path);

      if (unsuccessfulItem && !showErrorToast) {
        // skipped by user
        unsuccessfulItem.isSkipped = true;
      }

      if (showErrorToast) {
        // failed due to error
        if (unsuccessfulItem) unsuccessfulItem.isFailed = true;
        toast.error(`Error deleting "${item.name}".`);
      }
    }
  }
}
// --- Called when delete queue completes all items
function onDeleteComplete() {
  try {
    const allItems = itemsToDelete || [];
    const failedItems = allItems.filter((item) => item.isFailed);
    const skippedItems = allItems.filter((item) => item.isSkipped);

    // Find successfully deleted folders (both flags must be false)
    const deletedFolders = allItems.filter(
      (item) => item.isFolder && !item.isFailed && !item.isSkipped
    );

    const deletedPaths = deletedFolders.map((folder) => folder.path);

    // Notify FoldersTree to remove deleted folders from the tree
    if (deletedFolders.length > 0) {
      window.publishTopic("FoldersTree:delete", { paths: deletedPaths });
    }
    // Notify FoldersTree to collapse any failed folders to refresh their state (only failed, not skipped)
    const failedFolderPaths = failedItems.filter((item) => item.isFolder).map((item) => item.path);
    if (failedFolderPaths.length > 0) {
      window.publishTopic("FoldersTree:collapse", { paths: failedFolderPaths });
    }

    // Refresh files list after deletion
    window.publishTopic("FilesContainer:refresh");

    // Show completion toast with detailed parts
    const failedCount = failedItems.length;
    const skippedCount = skippedItems.length;
    const deletedCount = allItems.length - failedCount - skippedCount;

    const parts = [`Deleted ${deletedCount} item(s)`];
    if (failedCount > 0) parts.push(`${failedCount} failed`);
    if (skippedCount > 0) parts.push(`${skippedCount} skipped`);
    toast.success(parts.join(", ") + ".");
  } finally {
    isDialogOpen = false;
    gIsFileOperationInProgress = false;
  }
}
