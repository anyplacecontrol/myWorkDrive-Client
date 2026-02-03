
function mapFolderItemsToNodes(items) {
  return (items || [])
    .filter((item) => item.isFolder)
    .map((item) => ({
      id: item.path,
      name: item.name,
      icon: "folder",
      dynamic: true,
    }));
}


function loadChildren(node) {
  const response = Actions.callApi({
    method: "get",
    url: `/ListFolder?path=${node.id}`,
  });

  const items = Array.isArray(response) ? response : [];
  return mapFolderItemsToNodes(items);
}

function handleRenameMessage(msg) {
  if (!msg || msg.type !== 'renameTreeNode') return;

  // Get the old node to find its parent
  const oldNode = tree.getNodeById(msg.oldPath);
  if (!oldNode) return;

  // Get parent ID
  const parentId = oldNode.parentIds.length > 0
    ? oldNode.parentIds[oldNode.parentIds.length - 1]
    : null;

  // If there's a parent, check if it's expanded
  if (parentId) {
    const expandedNodes = tree.getExpandedNodes();
    const isParentExpanded = expandedNodes.includes(parentId);

    // If parent is expanded, collapse
    if (isParentExpanded) {
      tree.markNodeUnloaded(parentId);
      delay(100);
      tree.collapseNode(parentId);
    }
  }
}
