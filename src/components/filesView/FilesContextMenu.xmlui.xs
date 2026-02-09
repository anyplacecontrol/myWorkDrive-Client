// Helper functions for menu item visibility

function isAnythingSelected() {
  return $context.selectedItems && $context.selectedItems.length > 0;
}

function isSingleSelection() {
  return $context.selectedItems && $context.selectedItems.length === 1;
}

function isMultipleSelection() {
  return $context.selectedItems && $context.selectedItems.length > 1;
}

function isAnyFolder() {
  return $context.selectedItems && $context.selectedItems.length > 0 &&
         $context.selectedItems.some(function(item) { return item.isFolder; });
}

function isSingleFile() {
  return $context.selectedItems && $context.selectedItems.length === 1 &&
         !$context.selectedItems[0].isFolder;
}
