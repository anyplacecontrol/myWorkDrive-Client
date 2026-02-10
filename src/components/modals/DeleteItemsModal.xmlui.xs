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

// --- Processes a single queued item for deletion
function onDeleteQueuedItem(eventArgs) {
  const item = eventArgs.item;
  const url = item.isFolder ? "/DeleteFolder" : "/DeleteFile";
  Actions.callApi({
    url,
    method: "delete",
    queryParams: {
      path: item.path,
    },
    invalidates: [],
  });
}

// --- Handles errors during delete queue processing
function onDeleteQueuedItemError(error, eventArgs) {
  const item = eventArgs.item;

  // Track failed item
  failedItems.push(item);

  if (error.statusCode === 417) {
    signError(`The folder ${item.name} is not empty, it cannot be deleted.`);
  } else {
    signError(`Error deleting ${item.name}.`);
  }
  return false;
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

    // Refresh files list after deletion
    window.publishTopic("FilesContainer:refresh");
  } finally {
    isDialogOpen = false;
    gIsFileOperationInProgress = false;
  }
}
