function handleMessage(msg) {
  if (!msg || !msg.type) return;

  switch (msg.type) {
    case "FilesContainer:refresh":
      fileCatalogData.refetch();
      break;
    default:
      // Unknown message type, ignore
      break;
  }
}
