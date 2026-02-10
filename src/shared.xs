function navigateTo(pathOrItem) {
  // Handle both string paths and item objects
  if (!pathOrItem) return;

  let path;
  if (typeof pathOrItem === "string") {
    path = pathOrItem;
  } else if (pathOrItem && typeof pathOrItem === "object") {
    // If it's an object, check if it's a folder
    if (pathOrItem.isFolder === false) return;
    path = typeof pathOrItem.path === "string" ? pathOrItem.path : pathOrItem.id;
  } else {
    return;
  }

  if (!path || typeof path !== "string") return;

  const targetUrl = MwdHelpers.buildNavigationUrl(path);

  if (targetUrl) {
    Actions.navigate(targetUrl);
  }
}

function getCurrentDrive() {
  return defaultTo($queryParams.drive, window.AppConfig.rootDrive);
}

function getCurrentFolder() {
  return $queryParams.folder || '' //defaultTo($queryParams.folder, '/');
}


function downloadFile(file) {
  if (!file || !file.path) return;

  // Get actual file info to ensure size is up-to-date
  const fileInfo = Actions.callApi({
    url: '/GetFileInfo',
    method: 'get',
    queryParams: {
      path: file.path,
    },
  });

  if (fileInfo.isFolder)
    return;

  Actions.download({
    url: '/ReadFile',
    queryParams: {
      path: file.path,
      startPosition: 0,
      count: fileInfo.size,
    },
    fileName: file.name,
  });
}

function copyOrCut(items, action) {
  const list = Array.isArray(items) ? items : [];
  if (list.length === 0) return;

  // Validate first item to ensure copy/cut operation is allowed
  const firstItem = list[0];
  if (!window.MwdHelpers.validateFileOperation(firstItem.path)) {
    toast.error(`Unable to execute operation "${action}" with: ${firstItem.name}`);
    return;
  }

  const payload = {
    action: action === "cut" ? "cut" : "copy",
    items: list
  };

  // Use global clipboard helper instead of AppState bucket
  gSetFileClipboard(payload);
  // Use a formatted summary computed from the items array
  const summary =  window.MwdHelpers.formatItemsSummary(list);
  const actionText = payload.action === "copy" ? "Copied " : "Cut ";
  toast.success(actionText + summary);
}


