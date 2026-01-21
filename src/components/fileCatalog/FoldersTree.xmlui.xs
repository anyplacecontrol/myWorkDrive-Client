var rootPath = ($props.drive || ":sh:Documents:") + "/";
var treeData = [
  {
    id: rootPath,
    name: "Root",
    icon: "folder",
    dynamic: true,
  },
];
var isCollapseSync = false;

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

// Helper: find a node by id in nested nodes and apply an updater function to it
function findAndUpdate(nodes, targetId, updater) {
  if (!nodes || !Array.isArray(nodes)) return false;
  for (let n of nodes) {
    if (n.id === targetId) {
      updater(n);
      return true;
    }
    if (n.children && n.children.length > 0) {
      const found = findAndUpdate(n.children, targetId, updater);
      if (found) return true;
    }
  }
  return false;
}

function loadChildren(node) {
  const response = Actions.callApi({
    method: "get",
    url: "/ListFolder",
    queryParams: { path: node.id },
  });
  const items = Array.isArray(response) ? response : [];
  const mapped = mapFolderItemsToNodes(items);

  // Insert mapped children into local treeData so we can inspect local state
  const inserted = findAndUpdate(treeData, node.id, (n) => {
    n.children = mapped;
  });

  // Only return mapped children if they were attached to local treeData;
  // otherwise signal failure with null so caller can handle it.
  if (inserted) return mapped;
  return null;
}

function onNodeCollapse(node) {
  if (isCollapseSync) {
    return;
  }
  isCollapseSync = true;
  // Remove children from local treeData because of potential filesystem change
  try {
    findAndUpdate(treeData, node.id, (n) => {
      n.children = [];
    });
  } catch (e) {
    // swallow logging errors silently
  }

  tree.collapseNode(node.id);
  tree.markNodeUnloaded(node.id);
  isCollapseSync = false;
}
