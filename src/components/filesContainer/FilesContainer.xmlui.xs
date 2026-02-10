var drive = getCurrentDrive();
var folder = getCurrentFolder();

var sortedFiles = MwdHelpers.sortFiles(
  fileCatalogData.value,
  gSortBy,
  gSortDirection
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
    let item_ = JSON.parse(JSON.stringify(item));
    item_.id = item.path;
    item_.type = item.isFolder ? "Folder" : "File " + getFileExtension(item.path);
    return item_;
  });
}
