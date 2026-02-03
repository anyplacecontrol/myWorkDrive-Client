// --- Handles the delete button click
function onDeleteClick() {
  const items = $param || [];
  console.log("Deleting items:", items);
  if (items.length === 0) {
    deleteModal.close();
    return;
  }

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
  });
}

// --- Handles errors during delete queue processing
function onDeleteQueuedItemError(error, eventArgs) {
  if (error.statusCode === 417) {
    signError(`The folder ${eventArgs.item.name} is not empty, it cannot be deleted.`);
  } else {
    signError(`Error deleting ${eventArgs.item.name}.`);
  }
  return false;
}
// --- Called when delete queue completes all items
function onDeleteComplete() {
  inProgress = false;
  deleteModal.close();
}
