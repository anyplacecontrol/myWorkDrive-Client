var drive = $queryParams.drive || '';
var folder = $queryParams.folder || '';

var sortedFiles = MwdHelpers.sortFiles(
  fileCatalogData.value,
  sortBy,
  sortDirection
);

var isAnyFiles = drive ? (sortedFiles.length > 0 && !fileCatalogData.loading) : false;

function handleTreeContextMenu(ev) {
  const targetPath = window.MwdHelpers.joinPath(drive, folder);
  dropZoneContextMenu.openAt(ev, { selectedItems: [], targetPath });
}

function transformResult(result) {
  const requestPath = window.MwdHelpers.joinPath(drive, folder);
  const filtered = MwdHelpers.filterListResults(result, requestPath);
  return filtered.map((item) => {
    return Object.assign({}, item, {
      id: item.path,
      type: item.isFolder ? "Folder" : "File " + getFileExtension(item.path)
    });
  });
}
