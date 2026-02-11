// --- Handles incoming message to open delete modal
function handleMessageReceived(msg) {
  if (msg && msg.type === 'DeleteItemsModal:open') {
    gIsFileOperationInProgress = false;
    failedItems = [];
    const incoming = msg.payload || {};
    const items = incoming.items || [];
    // Validate all item paths before opening dialog

      if (!window.MwdHelpers.validateFileOperation(items[0].path)) {
        toast.error(`Unable to execute operation with: ${items[0].name}`);
        return;
      }

    itemsToDelete = items;
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
  const items = itemsToDelete || [];
  console.log("Deleting items:", items);
  if (items.length === 0) {
    isDialogOpen = false;
    return;
  }

  // Validate all items before deletion
  for (const item of items) {
    if (!window.MwdHelpers.validateFileOperation(item.path)) {
      toast.error(`Unable to execute operation with: ${item.path}`);
      isDialogOpen = false;
      return;
    }
  }

  // Store items for later use in onDeleteComplete
  itemsToDelete = items;
  gIsFileOperationInProgress = true;
  deleteQueue.enqueueItems(items);
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
    // Handle 417 error (folder not empty) with user confirmation
    if (error.statusCode === 417 && confirm({
      title: "Delete non-empty folder?",
      message: `The folder "${item.name}" is not empty.`,
      buttons: [{ label: "Yes", value: true }]
    })) {
      try {
        doDelete({ item, actionWhenNotEmpty: "stopOnError" });
        return; // Success - exit without tracking as failed
      } catch (retryError) {
        // Fall through to error handling below
      }
    }

    // Track failed item and show toast for non-417 errors
    failedItems.push(item);
    toast.error(`Error deleting "${item.name}".`);
  }
}
// --- Called when delete queue completes all items
function onDeleteComplete() {
  try {
    const allItems = itemsToDelete || [];
    const failedPaths = failedItems.map((item) => item.path);

    // Find successfully deleted folders
    const deletedFolders = allItems.filter(
      (item) => item.isFolder && !failedPaths.includes(item.path)
    );

    const deletedPaths = deletedFolders.map((folder) => folder.path);

    // Notify FoldersTree to remove deleted folders from the tree
    if (deletedFolders.length > 0) {
      window.publishTopic("FoldersTree:delete", { paths: deletedPaths });
    }
    // Notify FoldersTree to collapse any failed folders to refresh their state
    if (failedPaths.length > 0) {
      window.publishTopic("FoldersTree:collapse", { paths: failedPaths });
    }

    // Refresh files list after deletion
    window.publishTopic("FilesContainer:refresh");

    // Show completion toast
    const deletedCount = allItems.length - failedItems.length;
    const failedCount = failedItems.length;

    if (failedCount > 0) {
      toast.success(`Deleted ${deletedCount} item(s), ${failedCount} failed/skipped.`);
    } else {
      toast.success(`Deleted ${deletedCount} item(s).`);
    }
  } finally {
    isDialogOpen = false;
    gIsFileOperationInProgress = false;
  }
}
