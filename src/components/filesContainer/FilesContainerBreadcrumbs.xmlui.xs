function getDriveName() {
  return ($queryParams.drive || "").replace(":sh:", "").replace(":/", "").replace(":", "");
}

function getCrumbs() {
  const folder = $queryParams.folder || "";
  const decoded = decodeURIComponent(folder);
  const isWindows = /\\/.test(decoded);

  const crumbs = isWindows
    ? [
        {
          name:
            decoded
              .split(/[\\\/]+/)
              .filter((p) => p)
              .pop() || folder,
          folder: folder,
        },
      ]
    : (folder.startsWith("/") ? folder : "/" + folder)
        .split("/")
        .filter((p) => p)
        .map((p, i, a) => ({
          name: p,
          folder: a.slice(0, i + 1).join("/"),
        }));
  return crumbs;
}
