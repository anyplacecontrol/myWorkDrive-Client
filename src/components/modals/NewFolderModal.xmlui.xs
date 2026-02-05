// --- Executes the create folder operation
function doCreateFolder({ parentFolder, name, conflictBehavior }) {
  Actions.callApi({
    url: "/CreateFile",
    method: "post",
    body: {
      name,
      path: parentFolder,
      conflictBehavior,
    },
    invalidates: [],
  });
  toast.success(`"${name}" folder successfully created`);

  // Update tree node after successful creation
  window.publishTopic("FoldersTree:insert", { name, parentFolder });

  // Refresh files list after creation
  window.publishTopic("FilesContainer:refresh");
}

// --- Handles the submit action from the new folder modal
function onSubmitClick(name) {
  inProgress = true;
  const parentFolder = $props.drive + defaultTo($props.folder, "/");
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

    // Update tree node after successful creation
    window.publishTopic("FoldersTree:insert", { name, parentFolder });

    // Refresh files list after creation
    window.publishTopic("FilesContainer:refresh");
  } catch (error) {
    if (error.statusCode === 409) {
      toast.error("Folder " + name + " already exists");
    } else {
      toast.error("Error creating folder");
    }
  } finally {
    inProgress = null;
    newFolderModal.close();
  }
}
