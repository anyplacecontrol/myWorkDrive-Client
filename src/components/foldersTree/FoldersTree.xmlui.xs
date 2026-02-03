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

function handleMessage(msg) {
  if (!msg || !msg.type) return;

  switch (msg.type) {
    case "renameTreeNode":
      handleRenameTreeNode(msg);
      break;
    case "deleteFolders":
      handleDeleteFolders(msg);
      break;
    case "collapseRoot":
      handleCollapseRoot();
      break;
    default:
      // Unknown message type, ignore
      break;
  }
}

function handleRenameTreeNode(msg) {
  // Get the old node to find its parent
  const oldNode = tree.getNodeById(msg.oldPath);
  if (!oldNode) return;

  // Get parent ID
  const parentId =
    oldNode.parentIds.length > 0 ? oldNode.parentIds[oldNode.parentIds.length - 1] : null;

  // If there's a parent, check if it's expanded
  if (parentId) {
    tree.markNodeUnloaded(parentId);
    delay(100);
    tree.collapseNode(parentId);
  }
}

function handleDeleteFolders(msg) {
  if (!msg.paths || !Array.isArray(msg.paths)) return;

  msg.paths.forEach((path) => {
    const node = tree.getNodeById(path);

    if (node) {
      tree.removeNode(path);
    }
  });
}

function handleCollapseRoot() {
  const rootNodeId = $props.drive + "/";

  tree.markNodeUnloaded(rootNodeId);
  delay(100);
  tree.collapseNode(rootNodeId);
}
