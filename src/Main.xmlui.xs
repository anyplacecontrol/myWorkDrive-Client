//=================== UTILITIES ==============================================

function toastSuccess(message) {
  toast.success(message);
}

function handleFileOperation(payload) {
  if (!payload || !payload.type) return;

  switch(payload.type) {
    case 'navigate':
      navigateTo(payload.target);
      break;
    case 'remove':
      remove(payload.items);
      break;
    case 'copy':
      copyOrCut(payload.items, 'copy');
      break;
    case 'cut':
      copyOrCut(payload.items, 'cut');
      break;
    case 'paste':
      paste(payload.items);
      break;
    default:
      console.warn('Unknown action type:', payload.type);
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
    path =
      typeof pathOrItem.path === "string" ? pathOrItem.path : pathOrItem.id;
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
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toastSuccess((action === "copy" ? "Copied " : "Cut ") + names);
}

function paste(items) {
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toastSuccess("Pasted " + names + " item(s)");
}

function remove(items) {
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  toastSuccess("Deleted " + names + " item(s)");
}

function rename(utilities, refs, item) {
  refs.renameModal.open({
    item,
    onSubmit: (newName) => {
      const result = confirm({
        title: "Confirm renaming " + item.name + " to " + newName,
        buttons: [
          {
            label: "Confirm",
            value: "confirm",
          },
        ],
      });
      if (result !== "confirm") return;
      if (!item) return;

      utilities.toastSuccess("Rename successful");
    },
  });
}

//=================== FILE OPERATIONS STATE INITIALIZER ======================

function getFileOperationsInitial() {
  const utilities = {
    toastSuccess,
    navigateTo,
    rename,
  };

  const refs = {
    renameModal,
    filesContainer,
  };

  return {
    rename: (item) => {
      utilities.rename(utilities, refs, item);
    },
    refresh: () => refs.filesContainer.refresh(),
  };
}
