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
  const payload = {
    action: action === "cut" ? "cut" : "copy",
    items: list
  };

  AppState.set('fileClipboard', payload);

  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toast.success((payload.action === "copy" ? "Copied " : "Cut ") + (names || "item(s)"));
}

function paste(items) {
  //TODO: Add business logic for pasting items
  const targetList = Array.isArray(items) ? items : [];

  const target = targetList.find((it) => it && it.isFolder) || targetList[0];
  if (!target || !target.path) {
    toast.error("Select a folder to paste into");
    return;
  }

  const clipboard = AppState.get('fileClipboard');
  if (!clipboard || !Array.isArray(clipboard.items) || clipboard.items.length === 0) {
    toast.error("Nothing to paste");
    return;
  }

  const results = [];

  for (const it of clipboard.items) {
    if (!it || !it.path) continue;

    const isFolder = !!it.isFolder;
    const url = clipboard.action === 'cut' ? (isFolder ? '/MoveFolder' : '/MoveFile') : (isFolder ? '/CopyFolder' : '/CopyFile');
    const newPath = MwdHelpers.joinPath(target.path, it.name || MwdHelpers.getFileName(it.path));

    try {
      const res = Actions.callApi({
        url,
        method: 'post',
        body: {
          path: it.path,
          newPath,
        },
      });
      results.push(res);
    } catch (err) {
      results.push({ error: err });
    }
  }

  if (clipboard.action === 'cut') {
    AppState.set('fileClipboard', null);
  }

  toast.success("Paste operation started");
  return results;
}
