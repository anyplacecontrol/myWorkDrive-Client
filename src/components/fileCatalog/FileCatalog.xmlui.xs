// ============================================================================
// UI component event handlers

// --- Add the selected file item to the queue managing the "move" operation
function doMoveItems(targetId) {
  moveItemsQueue.enqueueItems(
    selectionState.value.selectedIds.map((id) => {
      const item = getCatalogItemById(id);
      return {
        path: item.id,
        isFolder: item.isFolder || false,
        name: item.id.split("/").pop(),
        newPath:
          targetId +
          (targetId.endsWith("/") ? "" : "/") +
          item.id.split("/").pop(),
        conflictBehavior: "fail",
      };
    })
  );
  moveModal.close();
}

// --- Add the selected file item to the queue managing the "copy" operation
function doCopyItems(targetId) {
  copyItemsQueue.enqueueItems(
    selectionState.value.selectedIds.map((id) => {
      const item = getCatalogItemById(id);
      return {
        path: item.id,
        isFolder: item.isFolder || false,
        name: item.id.split("/").pop(),
        newPath:
          targetId +
          (targetId.endsWith("/") ? "" : "/") +
          item.id.split("/").pop(),
        conflictBehavior: "fail",
      };
    })
  );
  copyModal.close();
}

// --- Download the selected files
function downloadFiles() {
  if (!selectionState.value.selectedIds.length) {
    return;
  }
  if (
    selectionState.value.selectedIds.length === 1 &&
    !!getCatalogItemById(selectionState.value.selectedIds[0]).name &&
    !getCatalogItemById(selectionState.value.selectedIds[0]).isFolder
  ) {
    const item = getCatalogItemById(selectionState.value.selectedIds[0]);

    // --- Use the API endpoint for single file download
    Actions.download({
      url: "/ReadFile",
      method: "get",
      queryParams: {
        path: item.path,
        startPosition: 0,
        count: item.size,
      },
      fileName: item.name,
    });
  } else {
    // --- Use the API endpoint for multiple files download (zip them)
    const fileName = "export_" + formatDate(Date.now()) + ".zip";
    Actions.download({
      url: "/ZipFiles",
      method: "post",
      queryParams: {
        zipName: fileName,
        respondWith: "data",
      },
      body: {
        paths: selectionState.value.selectedIds.map(
          (id) => getCatalogItemById(id).path
        ),
      },
      fileName,
    });
  }
}

// --- Select only the specified item
function onSelectItem(selectedItem) {
  selectionState.update({
    selectedIds: [selectedItem],
  });
}

// --- Rename the specified item
function onSaveRenamedItem(newPath) {
  const item = getCatalogItemById(selectionState.value.selectedIds[0]);
  const path = item.path;
  const isFolder = item.isFolder;
  try {
    doRename({
      path,
      newPath,
      isFolder,
      conflictBehavior: "fail",
    });
    renameModal.close();
  } catch (error) {
    if (error.statusCode === 409) {
      const result = confirm({
        title: "File " + newPath + " already exists",
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
          newPath,
          isFolder,
          conflictBehavior: result,
        });
      }
      renameModal.close();
    }
  }
}

// --- Queue the specified file items for deletion (after user confirmation)
function deleteFileEntries() {
  const subjectName =
    selectionState.value.selectedIds.length === 1
      ? getCatalogItemById(selectionState.value.selectedIds[0]).name
      : selectionState.value.selectedIds.length + " items";
  const result = confirm(
    "Delete?",
    "Are you sure you want to delete " + subjectName + "?",
    "Delete"
  );
  if (result) {
    console.log("Deleting items", filterCatalogDataBySelection());
    deleteQueue.enqueueItems(filterCatalogDataBySelection());
  }
}

// ============================================================================
// Helper functions

function suggestName(origName) {
  const parts = origName.split(".");
  if (parts.length === 1) {
    return origName + "(2)";
  } else {
    const ext = parts.pop();
    return parts.join(".") + "(2)." + ext;
  }
}

// ============================================================================
// Queue event handlers

function onUploadFiles(files) {
  // === DEBUG LOGGING - Initial file reception ===
  console.log("📥 UPLOAD DEBUG - Received files:", files.length, "files");
  files.forEach((file, index) => {
    console.log(`File ${index} - Enhanced Check:`);
    console.log("  name:", file.name);
    console.log("  webkitRelativePath:", file.webkitRelativePath);
    console.log("  path:", file.path);
    console.log("  fullPath:", file.fullPath);
    console.log("  size:", file.size);
    console.log("  type:", file.type);
  });

  uploadItemsQueue.enqueueItems(
    files.map((file) => {
      return {
        drive: $props.drive,
        folder: $props.folder,
        file: file,
        conflictBehavior: "fail",
      };
    })
  );
}

// ============================================================================
// Queue handlers

// --- Mange the upload queue
function onUploadQueuedItem(eventArgs) {
  // --- Microsoft: Use a byte range size that is a multiple of 320 KiB (327,680 bytes).
  // --- Failing to use a fragment size that is a multiple of 320 KiB can result in large
  // --- file transfers failing after the last byte range is uploaded.
  const chunkUnitInBytes = 327680;
  const chunkSizeInBytes = 10 * chunkUnitInBytes;

  // --- Use this item
  const fileItem = eventArgs.item;

  // --- Create upload session for large files
  const startUploadBody = {
    size: fileItem.file.size,
    conflictBehavior: fileItem.conflictBehavior,
  };

  // --- Start the file upload
  let uploadId;

  // --- Extract and prepare path information
  let pathToUse = fileItem.file.name;
  let directoriesNeeded = [];

  if (fileItem.file.path) {
    const segments = fileItem.file.path.split("/").filter(Boolean);
    if (segments.length > 1) {
      pathToUse = segments.slice(1).join("/");
      // Extract directory structure that needs to be created
      const pathParts = pathToUse.split("/");
      if (pathParts.length > 1) {
        // Create all intermediate directory paths
        for (let i = 1; i < pathParts.length; i++) {
          const dirPath =
            fileItem.drive +
            (fileItem.folder || "/") +
            (fileItem.folder.endsWith("/") ? "" : "/") +
            pathParts.slice(0, i).join("/");
          directoriesNeeded.push(dirPath);
        }
      }
    }
  } else if (fileItem.file.webkitRelativePath) {
    pathToUse = fileItem.file.webkitRelativePath;
  }

  const finalPath =
    fileItem.drive +
    (fileItem.folder || "/") +
    (fileItem.folder.endsWith("/") ? "" : "/") +
    pathToUse;

  console.log("🎯 UPLOAD DEBUG - Path and directories:");
  console.log("  finalPath:", finalPath);
  console.log("  directoriesNeeded:", directoriesNeeded);

  // --- Create directories first in the correct order (parent before child)
  // Remove duplicates manually and sort
  const uniqueDirs = directoriesNeeded
    .filter((dir, index, arr) => arr.indexOf(dir) === index)
    .sort();

  console.log("📁 Need to create directories:", uniqueDirs);

  for (const dirPath of uniqueDirs) {
    console.log("📁 Creating directory:", dirPath);

    // Extract the parent path and folder name
    const lastSlashIndex = dirPath.lastIndexOf("/");
    const parentPath = dirPath.substring(0, lastSlashIndex + 1); // Include trailing slash
    const folderName = dirPath.substring(lastSlashIndex + 1);

    console.log("  Parent path:", parentPath);
    console.log("  Folder name:", folderName);

    const createResult = Actions.callApi({
      url: "/CreateFile",
      method: "post",
      body: {
        name: folderName,
        path: parentPath,
        conflictBehavior: "fail",
      },
    });

    console.log("✅ Directory created successfully:", dirPath, createResult);
  }

  console.log("⚙️ All directories processed, starting file upload...");
  const startResult = Actions.callApi({
    url: "/StartFileUpload",
    method: "post",
    headers: {
      Accept: "application/json",
    },
    queryParams: {
      path: finalPath,
    },
    body: startUploadBody,
  });
  uploadId = startResult.uploadId;

  Actions.upload({
    url: ({ $uploadParams }) =>
      "/WriteFileBlock?uploadId=" +
      uploadId +
      "&startPosition=" +
      $uploadParams.chunkStart,
    method: "put",
    headers: ({ $uploadParams }) => {
      return {
        "Content-Range":
          "bytes " +
          $uploadParams.chunkStart +
          "-" +
          $uploadParams.chunkEnd +
          "/" +
          $uploadParams.fileSize,
      };
    },

    chunkSizeInBytes: chunkSizeInBytes,
    file: eventArgs.item.file,
    onProgress: eventArgs.onProgress,
  });

  // --- Complete the file upload
  const { uploadResult } = Actions.callApi({
    url: "/CompleteUpload?uploadId=" + uploadId,
    method: "post",
  });
}

// --- Handle the upload conflicts
function onUploadQueuedItemError(error, eventArgs) {
  console.log("HERE");
  if (error.statusCode === 409) {
    // --- Decide what to do with conflicting upload
    const result = confirmConflict(
      "File " + eventArgs.item.file.name + " already exists"
    );
    uploadItemsQueue.remove(eventArgs.actionItemId);
    if (result) {
      uploadItemsQueue.enqueueItems([
        { ...eventArgs.item, conflictBehavior: result },
      ]);
    }
    return false;
  }
}

// --- Move the current queued item
function onMoveQueuedItem(eventArgs) {
  const item = eventArgs.item;
  const url = item.isFolder ? "/MoveFolder" : "/MoveFile";
  Actions.callApi({
    url,
    method: "post",
    queryParams: {
      path: item.path,
      newPath: item.newPath,
      name: item.name,
      conflictBehavior: item.conflictBehavior || "fail",
    },
  });
}

// --- Manage move operation conflicts
function onMoveQueuedItemError(error, eventArgs) {
  if (error.statusCode === 409) {
    const result = confirmConflict(
      "File " + eventArgs.item.name + " already exists"
    );
    console.log("RESULT", result);
    moveItemsQueue.remove(eventArgs.actionItemId);
    if (result) {
      moveItemsQueue.enqueueItems([
        { ...eventArgs.item, conflictBehavior: result },
      ]);
    }
    return false;
  }
}

// --- Move the current queued item
function onCopyQueuedItem(eventArgs) {
  const item = eventArgs.item;
  const url = item.isFolder ? "/CopyFolder" : "/CopyFile";
  Actions.callApi({
    url,
    method: "post",
    body: {
      path: item.path,
      newPath: item.newPath,
      conflictBehavior: item.conflictBehavior || "fail",
    },
  });
}

// --- Manage copy operation conflicts
function onCopyQueuedItemError(error, eventArgs) {
  if (error.statusCode === 409) {
    const result = confirmConflict(
      "File " + eventArgs.item.name + " already exists"
    );
    copyItemsQueue.remove(eventArgs.actionItemId);
    if (result) {
      copyItemsQueue.enqueueItems([
        { ...eventArgs.item, conflictBehavior: result },
      ]);
    }
    return false;
  }
}

// --- Delete the current queued item
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

// --- Manage delete operation errors
function onDeleteQueuedItemError(error, eventArgs) {
  console.log("status", error.statusCode);
  if (error.statusCode === 417) {
    signError("The folder is not empty, it cannot be deleted.");
  } else {
    console.log("Delete error", error);
    signError(error.details.message || error.message);
  }
  return false;
}

// ============================================================================
// Internal functions used only within this file

function filterCatalogDataBySelection() {
  return fileCatalogData.value.filter((item) =>
    selectionState.value.selectedIds.some((selected) => selected === item.id)
  );
}

function getCatalogItemById(id) {
  return fileCatalogData.value.find((item) => item.id === id);
}

// --- Executes the rename operation
function doRename({ path, newPath, isFolder, conflictBehavior }) {
  const parentPath = path.split("/").slice(0, -1).join("/") || "/";
  const url = isFolder ? "/MoveFolder" : "/MoveFile";
  Actions.callApi({
    url,
    method: "post",
    queryParams: {
      path,
      newPath: parentPath + (parentPath === "/" ? "" : "/") + newPath,
      conflictBehavior,
    },
  });
}

// --- This function displays a confirmation dialog when there is a conflict
// --- during file operations like upload, move, or copy. It provides options to
// --- either keep both files (rename) or replace the existing file.
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
