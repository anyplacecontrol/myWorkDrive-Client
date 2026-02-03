// --- Executes the rename operation
function doRename({ path, newName, isFolder, conflictBehavior }) {
  const parentPath = window.MwdHelpers.getParentFolder(path);
  const newPath = parentPath + "/" + newName;

  const url = isFolder ? "/MoveFolder" : "/MoveFile";
  Actions.callApi({
    url,
    method: "post",
    queryParams: {
      path,
      newPath,
      conflictBehavior,
    },
  });
  toast.success(`"${item.name}" renamed to "${newName}"`);
  // Update tree node after successful rename
  if (isFolder) {
    window.postMessage(
      {
        type: "renameTreeNode",
        oldPath: path,
        newPath,
      },
      "*"
    );
  }
}

// --- Handles the submit action from the rename modal
function onSubmitClick(newName) {
  const item = $param;
  const path = item.path;
  const isFolder = item.isFolder;
  inProgress = true;

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
    inProgress = false;
    renameModal.close();
  }
}
