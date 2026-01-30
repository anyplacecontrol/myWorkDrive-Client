//=================== UTILITIES ==============================================
function toastSuccess(message) {
  toast.success(message);
}

function copyOrCut(utilities, items, action) {
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  utilities.toastSuccess((action === "copy" ? "Copied " : "Cut ") + names);
}

function paste(utilities, items) {
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  utilities.toastSuccess("Pasted " + names + " item(s)");
}

function remove(utilities, items) {
  const list = items || [];
  const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
  utilities.toastSuccess("Deleted " + names + " item(s)");
}

//=================== FILE OPERATIONS STATE INITIALIZER ======================

function getFileOperationsInitial() {
  const utilities = {
    toastSuccess,
    copyOrCut,
    paste,
    remove
  };

  return {
    copyOrCut: (items, action) => utilities.copyOrCut(utilities, items, action),
    paste: (items) => utilities.paste(utilities, items),
    remove: (items) => utilities.remove(utilities, items),
  };
}
