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
    invalidates: [],
  });

  const items = Array.isArray(response) ? response : [];
  const filtered = window.MwdHelpers.filterListResults(items, node.id);
  return mapFolderItemsToNodes(filtered);
}

// Message handling moved to BusHandler in markup

function handleRenameTreeNode(payload) {
  // Get the old node to find its parent
  const oldNode = tree.getNodeById(payload.oldPath);
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

function handleDeleteFolders(payload) {
  if (!payload || !payload.paths || !Array.isArray(payload.paths)) {
    return;
  }

  payload.paths.forEach((path) => {
    const node = tree.getNodeById(path);
    if (node) {
      tree.removeNode(path);
    }
  });
}

function handleInsertFolder(payload) {
  if (!payload) return;

  const {parentFolder, name} = payload;

  const normalizedParent = parentFolder.endsWith('/') ? parentFolder : parentFolder + '/';
  const path = normalizedParent + name;

  const newNode = {
    id: path,
    name: name,
    icon: 'folder',
    dynamic: true,
  };

  const parentNode = tree.getNodeById(parentFolder);
  if (parentNode) {
    if (tree.getNodeById(path)) return;
    tree.appendNode(parentFolder, newNode);
  }
}

function handleCollapseRoot() {
  const rootNodeId = rootDrive + "/";

  tree.markNodeUnloaded(rootNodeId);
  delay(100);
  tree.collapseNode(rootNodeId);
}
