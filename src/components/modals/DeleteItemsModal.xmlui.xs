// --- Handles the delete button click
function onDeleteClick() {
  const items = $param || [];
  console.log("Deleting items:", items);
  if (items.length === 0) {
    deleteModal.close();
    return;
  }

  // Store items for later use in onDeleteComplete
  itemsToDelete = items;
  inProgress = true;
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
  const allItems = itemsToDelete || [];
  const failedPaths = failedItems.map(item => item.path);

  // Find successfully deleted folders
  const deletedFolders = allItems.filter(
    item => item.isFolder && !failedPaths.includes(item.path)
  );

   const deletedPaths = deletedFolders.map(folder => folder.path);

  // Notify FoldersTree to remove deleted folders from the tree
  if (deletedFolders.length > 0) {

    window.postMessage(
      {
        type: "FoldersTree:deleteFolders",
        paths: deletedPaths,
      },
      "*"
    );
  }

  // Refresh files list after deletion
  window.postMessage(
    {
      type: "FilesContainer:refresh"
    },
    "*"
  );

  // Reset state
  failedItems = [];
  itemsToDelete = [];
  inProgress = false;
  deleteModal.close();
}
