//=================== UTILITIES ==============================================

function handleFileOperation(payload) {
  if (!payload || !payload.type) return;

  switch (payload.type) {
    case "navigate":
      navigateTo(payload.target);
      break;
    case "remove":
      remove(payload.items);
      break;
    case "copy":
      copyOrCut(payload.items, "copy");
      break;
    case "cut":
      copyOrCut(payload.items, "cut");
      break;
    case "paste":
      paste(payload.items);
      break;
    case "refresh":
      filesContainer.refresh();
      break;
    case "rename":
      renameModal.open(payload.item);
      break;
    default:
      console.warn("Unknown action type:", payload.type);
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

function remove(items) {
  //TODO: Add business logic for removing items
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toast.success("Deleted " + names + " item(s)");
}
