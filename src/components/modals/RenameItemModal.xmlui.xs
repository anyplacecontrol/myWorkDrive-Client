// --- Executes the rename operation
function doRename({ path, newName, isFolder, conflictBehavior }) {
  // Validate if operation is allowed
  if (!window.MwdHelpers.validateFileOperation(path)) {
    toast.error(`Unable to execute operation with: ${path}`);
    return;
  }

  const parentPath = window.MwdHelpers.getParentFolder(path);
  const newPath = window.MwdHelpers.joinPath(parentPath, newName);

  const url = isFolder ? "/MoveFolder" : "/MoveFile";
  Actions.callApi({
    url,
    method: "post",
    queryParams: {
      path,
      newPath,
      conflictBehavior,
    },
    invalidates: [],
  });
  toast.success(`"${item.name}" renamed to "${newName}"`);
  // Update tree node after successful rename
  if (isFolder) {
    window.publishTopic('FoldersTree:rename', { oldPath: path, newPath });
  }

  // Refresh files list after rename
  window.publishTopic('FilesContainer:refresh');
}

// --- Handles the submit action from the rename modal
function onSubmitClick(newName) {
  const item = itemToRename;
  const path = item.path;
  const isFolder = item.isFolder;
  isFileOperationInProgress = true;

  try {
    doRename({
      path,
      newName,
      isFolder,
      conflictBehavior: "fail",
    });
  } catch (error) {
    if (error.statusCode === 409) {
      const result = confirm({
        title: (isFolder ? "Folder" : "File") + " " + newName + " already exists",
        buttons: [
          {
            label: "Replace",
            value: "replace",
          },
        ],
      });
      if (result === "replace") {
        doRename({
          path,
          newName,
          isFolder,
          conflictBehavior: result,
        });
      }
    }
  } finally {
    isDialogOpen = false;
    isFileOperationInProgress = false;
  }
}
