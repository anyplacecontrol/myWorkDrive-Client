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
  if (!node || !node.id) return;
  const queryParams = MwdHelpers.fileItemQueryParams({
    path: node.id,
    isFolder: true,
  });
  const entries = Object.entries(queryParams || {});
  const parts = [];
  for (let i = 0; i < entries.length; i++) {
    const key = entries[i][0];
    const value = entries[i][1];
    if (value == null || value === "") {
      continue;
    }
    parts.push(encodeURIComponent(key) + "=" + encodeURIComponent(String(value)));
  }
  const queryString = parts.join("&");
  const targetUrl = queryString ? `/my-files?${queryString}` : "/my-files";
  Actions.navigate(targetUrl);
}

function onTreeSelectionDidChange(event) {
  console.log("Tree selection changed:", event);
  if (!event || !event.newNode) return;
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
}
