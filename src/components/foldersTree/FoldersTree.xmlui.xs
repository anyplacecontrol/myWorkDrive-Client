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


function loadChildren(node) {
  const response = Actions.callApi({
    method: "get",
    url: `/ListFolder?path=${node.id}`,
  });

  const items = Array.isArray(response) ? response : [];
  return mapFolderItemsToNodes(items);
}


