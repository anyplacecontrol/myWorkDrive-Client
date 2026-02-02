
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


