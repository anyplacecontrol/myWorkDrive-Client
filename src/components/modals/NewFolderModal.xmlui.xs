
// --- Handles incoming message to open new folder modal
function handleMessageReceived(msg) {
  if (msg && msg.type === 'NewFolderModal:open') {
    isFileOperationInProgress = false;
    isDialogOpen = true;
  }
}

// --- Handles modal close
function handleClose() {
  if (!isFileOperationInProgress) isDialogOpen = false;
  return !isFileOperationInProgress;
}

// --- Handles the submit action from the new folder modal
function onSubmitClick(name) {
  const parentFolder = window.MwdHelpers.joinPath(getCurrentDrive(), getCurrentFolder());
  if (!window.MwdHelpers.validateFileOperation( parentFolder + name)) {
    toast.error(`Unable to execute operation in current folder`);
    isDialogOpen = false;
    return;
  }

  isFileOperationInProgress = true;
  try {
    Actions.callApi({
      url: "/CreateFile",
      method: "post",
      body: {
        name,
        path: parentFolder,
        conflictBehavior: "fail",
      },
      invalidates: [],
    });
    toast.success(`"${name}" folder successfully created`);

    // Update tree node after successful creation (send names array)
    window.publishTopic("FoldersTree:insert", { parentFolder, names: [name] });

    // Refresh files list after creation
    window.publishTopic("FilesContainer:refresh");
  } catch (error) {
    if (error.statusCode === 409) {
      toast.error("Folder " + name + " already exists");
    } else {
      toast.error("Error creating folder");
    }
  } finally {
    isDialogOpen = false;
    isFileOperationInProgress = false;
  }
}
