function getSortedFiles() {
  return MwdHelpers.sortFiles(fileCatalogData.value, sortBy, sortDirection);
}

function isAnyFiles() {
  return $queryParams.drive ? getSortedFiles().length > 0 && !fileCatalogData.loading : false;
}

function onDroppedUpload(files) {
  const targetPath = window.MwdHelpers.joinPath($queryParams.drive || "", $queryParams.folder || "");
  window.MwdHelpers.setWindowProperty("pendingUploadFiles", files);
  window.publishTopic("UploadItemsModal:startPending", { targetPath });
}

function handleTreeContextMenu(ev) {
  const targetPath = window.MwdHelpers.joinPath($queryParams.drive, $queryParams.folder);
  dropZoneContextMenu.openAt(ev, { selectedItems: [], targetPath });
}

function transformResult(result) {
  const requestPath = window.MwdHelpers.joinPath($queryParams.drive, $queryParams.folder);
  const filtered = MwdHelpers.filterListResults(result, requestPath);
  return filtered.map((item) => {
    return Object.assign({}, item, {
      id: item.path,
      type: item.isFolder ? "Folder" : "File " + getFileExtension(item.path),
    });
  });
}
