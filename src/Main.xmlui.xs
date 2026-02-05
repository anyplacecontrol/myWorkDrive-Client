function handleFileOperation(message) {
  if (!message || !message.type) return;

  const data = message.payload || {};


  // Prefer matching the full topic string per request (analyze full strings)
  switch (message.type) {
    case 'Main:navigate':
      navigateTo(data.target);
      break;

    case 'Main:copy':
      copyOrCut(data.items, 'copy');
      break;
    case 'Main:cut':
      copyOrCut(data.items, 'cut');
      break;
    case 'Main:paste':
      paste(data.items);
      break;
    case 'Main:download':
      downloadFile(data.file);
      break;
    default:
      console.warn('Unknown action topic:', fullType);
  }
}

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

function copyOrCut(items, action) {
  //TODO: Add business logic for copy or cut
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toast.success((action === "copy" ? "Copied " : "Cut ") + names);
}

function paste(items) {
  //TODO: Add business logic for pasting items
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toast.success("Pasted " + names + " item(s)");
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
