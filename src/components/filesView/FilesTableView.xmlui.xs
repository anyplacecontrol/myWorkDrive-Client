function onItemDoubleClick(item) {
  if (item.isFolder)
    navigateTo(item);
}

function reselectAndOpenMenu(ev, item) {
  //Repeat Windows Explorer behavior with right click
  try {
    const selectedItem = selectionState.value.selectedIds || [];
    const newSelection = !item ? [] : (selectedItem.includes(item.id) ? selectedItem : [item.id]);

    // Update selection state with ids
    selectionState.update({ selectedIds: newSelection });

    // Map ids to full file entry objects from props.fileEntries and pass them to the context menu
    const entries = ($props.fileEntries || []).filter(function(f) {
      return newSelection.includes(f.id);
    });

    contextMenu.openAt(ev, { selectedItems: entries, targetPath: computeTargetPath() });
  } catch (e) {}
}

function computeTargetPath() {
  const drive = getCurrentDrive();
  const folder = getCurrentFolder();
  return window.MwdHelpers.joinPath(drive, folder);
}

// Handler for paste action from keyboard shortcut
function handlePasteAction(row, selectedItems) {
  const drive = getCurrentDrive();
  const folder = getCurrentFolder();
  const targetPath = window.MwdHelpers.joinPath(drive, folder);
  window.publishTopic('PasteItemsModal:open', { targetPath });
}

// Handler for sorting
function handleWillSort(sortBy, direction) {
  appState.update({ sortBy, sortDirection: direction });
  return true;
}
