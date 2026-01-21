var previewFile = $props.data;

var previewUrl =
  appGlobals.apiUrl + "/ReadFile?path=" + encodeURIComponent(previewFile.path) 
    + "&startPosition=0&count=" + previewFile.size;

var fileExtension = getFileExtension(previewFile.path);

var isPicture = ["jpg", "jpeg", "svg", "png", "gif"].includes(fileExtension);
var isPdf = fileExtension === "pdf";