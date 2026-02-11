// Handle incoming messages
// Handle incoming pub/sub messages for folder tree operations.
// `msg` shape: { type: string, payload: any }
// Supported types and payloads are documented on each handler below.
function handleMessageReceived(msg) {
  if (msg.type === "FoldersTree:rename") handleRenameTreeNode(msg.payload);
  else if (msg.type === "FoldersTree:insert") handleInsertFolder(msg.payload);
  else if (msg.type === "FoldersTree:delete") handleDeleteFolders(msg.payload);
  else if (msg.type === "FoldersTree:collapse") handleCollapseNodes(msg.payload);
}

function buildRootNodes(shares) {
  if (!shares || !Array.isArray(shares)) return [];

  return shares
    .filter((share) => !!(share && share.shareName))
    .map((share) => ({
      id: window.MwdHelpers.normalizeTreeNodeId(share.drivePath || ":sh:" + share.shareName + ":"),
      name: share.shareName,
      icon: "shared_folder",
      dynamic: true,
    }));
}

function treeNodeToFolderItem(node) {
  if (!node) return null;
  return {
    name: node.name,
    path: node.id,
    isFolder: true,
  };
}

// Handle context menu on tree node
function handleTreeContextMenu(ev, node, contextMenu) {
  delay(300);
  const folderItem = treeNodeToFolderItem(node);
  const targetPath = node.id; // Use the node's id (path) as targetPath
  if (folderItem && targetPath) contextMenu.openAt(ev, { selectedItems: [folderItem], targetPath });
}

function mapFolderItemsToNodes(items) {
  return (items || [])
    .filter((item) => item.isFolder)
    .map((item) => ({
      id: window.MwdHelpers.normalizeTreeNodeId(item.path),
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

function updateNodeAndChildren(node, oldPathPrefix, newPathPrefix) {
  const updatedNode = {
    id: node.id.replace(oldPathPrefix, newPathPrefix),
    name: node.id === oldPathPrefix ? window.MwdHelpers.getFileName(newPathPrefix) : node.name,
    icon: node.icon,
    dynamic: true,
  };

  if (node.children && node.children.length > 0) {
    updatedNode.children = node.children.map((child) =>
      updateNodeAndChildren(child, oldPathPrefix, newPathPrefix)
    )
  }

  return updatedNode;
}

// Handle renaming a folder node.
// Payload: { oldPath: string, newPath: string }
// - oldPath: current node id/path to rename
// - newPath: new id/path to replace with
function handleRenameTreeNode(payload) {
  const { oldPath, newPath } = payload;
  if (!oldPath || !newPath) return;

  const normalizedOldPath = window.MwdHelpers.normalizeTreeNodeId(oldPath);
  const oldNode = tree.getNodeById(normalizedOldPath);
  if (!oldNode) return;

  const normalizedNewPath = window.MwdHelpers.normalizeTreeNodeId(newPath);
  const updatedNodeTree = updateNodeAndChildren(oldNode, normalizedOldPath, normalizedNewPath);
  tree.replaceNode(normalizedOldPath, updatedNodeTree);
}

// Handle deleting multiple folder nodes from the tree.
// Payload: { paths: string[] }
// - paths: array of node ids/paths to remove from the tree
function handleDeleteFolders(payload) {
  if (!payload || !payload.paths || !Array.isArray(payload.paths)) {
    return;
  }

  payload.paths.forEach((path) => {
    const normalizedPath = window.MwdHelpers.normalizeTreeNodeId(path);
    const node = tree.getNodeById(normalizedPath);
    if (node) {
      console.log('----------------removed node', normalizedPath);
      tree.removeNode(normalizedPath);
    }
  });
}

// Handle inserting one or more folders under a parent node.
// Payload: { parentFolder: string, names: string[] }
// - parentFolder: id/path of the folder under which to insert
// - names: array of folder names to create as children
function handleInsertFolder(payload) {
  if (!payload) return;
  const { parentFolder, names } = payload;
  if (!parentFolder) return;

  const folderNames = Array.isArray(names) && names.length > 0 ? names : [];
  if (folderNames.length === 0) return;

  const normalizedParentFolder = window.MwdHelpers.normalizeTreeNodeId(parentFolder);
  const parentNode = tree.getNodeById(normalizedParentFolder);
  if (!parentNode) return;


  for (const n of folderNames) {
    const normalizedPath = window.MwdHelpers.joinPath(normalizedParentFolder, n);
    if (tree.getNodeById(normalizedPath)) {
      //if node already exists, collapse it
      collapseNode(normalizedPath);
    } else {
      console.log('----------------inserting node', normalizedPath);
      // Insert new node
      const newNode = {
        id: normalizedPath,
        name: n,
        icon: "folder",
        dynamic: true,
      };
      tree.appendNode(normalizedParentFolder, newNode);
    }
  }
}

function collapseNode(path) {
  if (!path) return;
  const normalizedPath = window.MwdHelpers.normalizeTreeNodeId(path);
  console.log('----------------collapsing node', normalizedPath);
  tree.markNodeUnloaded(normalizedPath);
  delay(300);
  tree.collapseNode(normalizedPath);
}

// Handle collapsing (marking unloaded and collapsing) multiple nodes.
// Payload: { paths: string[] }
// - paths: array of node ids/paths to collapse
function handleCollapseNodes(payload) {
  if (!payload) return;

  const { paths } = payload;
  if (!paths || !Array.isArray(paths)) return;

  paths.forEach((path) => {
    collapseNode(path);
  });
}
