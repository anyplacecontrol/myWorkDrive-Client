    var drive = getCurrentDrive();
    var folder = getCurrentFolder();

    var decoded = decodeURIComponent(folder);
    var isWindows = /\\/.test(decoded);

    var crumbs = isWindows
      ? [{ name: decoded.split(/[\\\/]+/).filter(p => p).pop() || folder, folder: folder }]
      : (folder.startsWith('/') ? folder : '/' + folder)
          .split('/')
          .filter(p => p)
          .map((p, i, a) => ({
             name: p,
             folder: a.slice(0, i + 1).join('/')
          }));

    var driveName = drive.replace(':sh:', '').replace(':/', '').replace(':', '');
