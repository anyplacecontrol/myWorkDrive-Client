var fileExtension = getFileExtension($props.file.name);

function downloadButton_onClick() {
  Actions.download({
    url:
      "/drives/" +
      $props.file.parentReference.driveId +
      "/items/" +
      $props.file.id +
      "/content",
    fileName: $props.file.name,
  });
}
