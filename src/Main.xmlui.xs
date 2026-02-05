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
