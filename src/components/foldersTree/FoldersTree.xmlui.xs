
// Handle incoming messages
function handleMessageReceived(msg) {
  if (msg.type === 'FoldersTree:rename') handleRenameTreeNode(msg.payload);
  else if (msg.type === 'FoldersTree:insert') handleInsertFolder(msg.payload);
  else if (msg.type === 'FoldersTree:delete') handleDeleteFolders(msg.payload);
  else if (msg.type === 'FoldersTree:collapse') handleCollapseRoot();
}

function buildRootNodes(shares) {
  if (!shares || !Array.isArray(shares)) return [];

  return shares
    .filter((share) => !!(share && share.shareName))
    .map((share) => ({
      id: share.drivePath || (":sh:" + share.shareName + ":/"),
      name: share.shareName,
      icon: "folder",
      dynamic: true,
    }));
}

function treeNodeToFolderItem(node) {
  if (!node) return null;
  return {
    name: node.name,
    path: node.id,
    isFolder: true
  };
}

// Handle context menu on tree node
function handleTreeContextMenu(ev, node, contextMenu) {
  delay(300);
  const folderItem = treeNodeToFolderItem(node);
  const targetPath = node.id; // Use the node's id (path) as targetPath
  if (folderItem && targetPath)
    contextMenu.openAt(ev, { selectedItems: [folderItem], targetPath });
}

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

function updateNodeAndChildren(node, oldPathPrefix, newPathPrefix) {
  const updatedNode = {
    id: node.id.replace(oldPathPrefix, newPathPrefix),
    name: node.id === oldPathPrefix
      ? window.MwdHelpers.getFileName(newPathPrefix)
      : node.name,
    icon: node.icon,
    dynamic: true,
  };

  if (node.children && node.children.length > 0) {
    updatedNode.children = node.children.map(child =>
      updateNodeAndChildren(child, oldPathPrefix, newPathPrefix)
    );
  }

  return updatedNode;
}

function handleRenameTreeNode(payload) {
  const { oldPath, newPath } = payload;
  if (!oldPath || !newPath) return;

  const oldNode = tree.getNodeById(oldPath);
  if (!oldNode) return;

  const updatedNodeTree = updateNodeAndChildren(oldNode, oldPath, newPath);
  tree.replaceNode(oldPath, updatedNodeTree);
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
  const currentDrive = getCurrentDrive();
  if (!currentDrive) return;

  tree.markNodeUnloaded(currentDrive);
  delay(300);
  tree.collapseNode(currentDrive);
}
