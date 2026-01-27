var rootPath = ($props.drive || ":sh:Documents:") + "/";
var treeData = [
  {
    id: rootPath,
    name: "Root",
    icon: "folder",
    dynamic: true,
  },
];

function navigateToNode(node) {
  if (!node.id) return;

  // Build URL and navigate to folder
  const targetUrl = MwdHelpers.buildNavigationUrl(node.id);
  Actions.navigate(targetUrl);
}

function onTreeSelectionDidChange(event) {
  if (!event.newNode) return;
  navigateToNode(event.newNode);
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
    url: `/ListFolder?path=${node.id}`,
  });

  const items = Array.isArray(response) ? response : [];
  const mapped = mapFolderItemsToNodes(items);

  // Insert children into local treeData
  const inserted = findAndUpdate(treeData, node.id, (n) => {
    n.children = mapped;
  });

  return inserted ? mapped : null;
}

function onNodeCollapse(node) {
  // Clear children from local treeData on collapse
  findAndUpdate(treeData, node.id, (n) => {
    n.children = [];
  });

  tree.collapseNode(node.id);
  tree.markNodeUnloaded(node.id);
}
