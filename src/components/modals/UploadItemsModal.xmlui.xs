function toFileArray(files) {
  if (Array.isArray(files)) return files;
  if (files && typeof files.length === "number") return Array.from(files);
  return [];
}

function getCurrentTargetPath() {
  const drive = $queryParams.drive || "";
  const folder = $queryParams.folder || "";
  return window.MwdHelpers.joinPath(drive, folder);
}

function resolveTargetPath(targetPath) {
  return targetPath || uploadTargetPath || getCurrentTargetPath();
}

function getBaseFileName(name) {
  const raw = String(name || "");
  const normalized = raw.replace(/\\/g, "/");
  const parts = normalized.split("/").filter(Boolean);
  return parts.length > 0 ? parts[parts.length - 1] : raw;
}

function myLog(stage, details) {
  try {
    if (details === undefined) {
      console.log("[my] " + stage);
    } else {
      console.log("[my] " + stage, details);
    }
  } catch (error) {
  }
}

const UPLOAD_FILE_STORE_KEY = "__myUploadFileStore";

function getUploadFileStore() {
  const existingStore = window[UPLOAD_FILE_STORE_KEY];
  if (existingStore && typeof existingStore === "object") {
    return existingStore;
  }

  const createdStore = {};
  if (window.MwdHelpers && typeof window.MwdHelpers.setWindowProperty === "function") {
    window.MwdHelpers.setWindowProperty(UPLOAD_FILE_STORE_KEY, createdStore);
  }

  const resolvedStore = window[UPLOAD_FILE_STORE_KEY];
  if (resolvedStore && typeof resolvedStore === "object") {
    return resolvedStore;
  }

  myLog("upload file store fallback in use");
  return createdStore;
}

function createUploadFileRef(file) {
  const refId = "uf_" + Date.now() + "_" + Math.floor(Math.random() * 1000000);
  const store = getUploadFileStore();
  if (!store || typeof store !== "object") {
    throw "Upload file store is not available";
  }
  store[refId] = file;
  return refId;
}

function getUploadFileByRef(refId) {
  const store = getUploadFileStore();
  if (!store || typeof store !== "object") return undefined;
  return store[refId];
}

function clearUploadFileRefs(items) {
  const store = getUploadFileStore();
  if (!store || typeof store !== "object") return;
  (items || []).forEach((item) => {
    if (item && item.fileRefId) {
      delete store[item.fileRefId];
    }
  });
}

function patchUploadItem(item, patch) {
  if (item && patch) {
    Object.assign(item, patch);
  }

  const key = item && item.fileRefId;
  if (!key) return;

  (uploadQueueItems || []).forEach((entry) => {
    if (entry && entry.fileRefId === key) {
      Object.assign(entry, patch);
    }
  });
}

function normalizeUploadFile(rawFile) {
  if (!rawFile) return null;
  if (typeof rawFile.slice === "function") return rawFile;

  const handle = rawFile.handle;
  if (handle && typeof handle.slice === "function") {
    return handle;
  }

  return rawFile;
}

function onUploadMessageReceived(msg) {
  if (!msg) return;
  myLog("message received", msg);

  if (msg.type === "UploadFiles:open") {
    const requestedPath = msg.targetPath || getCurrentTargetPath();
    myLog("open upload picker", { requestedPath });
    const samplePath = window.MwdHelpers.joinPath(requestedPath, "someFile");
    if (!window.MwdHelpers.validateFileOperation(samplePath)) {
      const targetName = window.MwdHelpers.getFileName(samplePath);
      myLog("target validation failed", { samplePath, targetName });
      toast.error(`Cannot upload into the target folder: ${targetName}`);
      return;
    }
    uploadTargetPath = requestedPath;
    myLog("picker opening", { uploadTargetPath });
    uploadFileInput.open();
    return;
  }

  if (msg.type === "UploadItemsModal:startPending") {
    const pendingFiles = window.pendingUploadFiles;
    const targetPath = msg.targetPath || getCurrentTargetPath();
    myLog("start pending upload", { targetPath, count: toFileArray(pendingFiles).length });
    startUploadFlow(pendingFiles, targetPath);
    window.MwdHelpers.setWindowProperty("pendingUploadFiles", null);
  }
}

function onUploadFilesSelected(files) {
  myLog("files selected", { count: toFileArray(files).length, uploadTargetPath });
  startUploadFlow(files, resolveTargetPath(uploadTargetPath));
}

function startUploadFlow(files, targetPath) {
  const list = toFileArray(files);
  myLog("start upload flow", { inputCount: list.length, targetPath });
  if (list.length === 0) return;

  const finalTargetPath = resolveTargetPath(targetPath);
  if (!finalTargetPath) {
    myLog("resolve target failed", { targetPath, uploadTargetPath });
    toast.error("Cannot determine target folder for upload.");
    return;
  }
  myLog("resolved target", { finalTargetPath });
  const samplePath = window.MwdHelpers.joinPath(finalTargetPath, "someFile");
  if (!window.MwdHelpers.validateFileOperation(samplePath)) {
    const targetName = window.MwdHelpers.getFileName(samplePath);
    myLog("target validation failed", { samplePath, targetName });
    toast.error(`Cannot upload into the target folder: ${targetName}`);
    return;
  }

  const itemsDescription = list.length === 1 ? `file "${list[0].name}"` : `${list.length} files`;
  const folderName = window.MwdHelpers.getFileName(finalTargetPath) || finalTargetPath;
  const userConfirmed = confirm(
    "Upload Confirmation",
    `Do you want to upload ${itemsDescription} to "${folderName}"?`,
    "Upload"
  );
  myLog("upload confirmation", { userConfirmed, itemsDescription, folderName });
  if (!userConfirmed) return;

  uploadTargetPath = finalTargetPath;
  selectedUploadFiles = list;
  uploadQueueItems = list.map((file) => {
    const safeName = getBaseFileName(file.name);
    const fileRefId = createUploadFileRef(file);
    return {
      file,
      fileRefId,
      name: safeName,
      size: Number(file.size) || 0,
      targetPath: finalTargetPath,
      isFailed: false,
      isSkipped: false,
      uploadedPath: null,
    };
  });

  totalUploadBytes = uploadQueueItems.reduce((sum, item) => sum + (item.size || 0), 0);
  processedUploadBytes = 0;
  currentUploadBytes = 0;
  uploadProgress = 0;

  isUploadDialogOpen = true;
  isFileOperationInProgress = true;
  myLog("enqueue upload items", {
    count: uploadQueueItems.length,
    totalUploadBytes,
    finalTargetPath,
    names: uploadQueueItems.map((item) => item.name),
  });
  uploadQueue.enqueueItems(uploadQueueItems);
}

function confirmUploadConflict(itemName) {
  return confirm({
    title: "Conflict",
    message: `File "${itemName}" already exists\nChoose how to handle the conflict`,
    buttons: [
      {
        label: "Keep both",
        value: "rename",
        themeColor: "primary",
      },
      {
        label: "Replace",
        value: "replace",
        themeColor: "secondary",
        variant: "outlined",
      },
    ],
  });
}

function cancelUploadSessionSafe(uploadId) {
  if (!uploadId) return;
  try {
    myLog("cancel upload session", { uploadId });
    Actions.callApi({
      url: "/CancelUpload",
      method: "post",
      queryParams: {
        uploadId,
      },
    });
    myLog("cancel upload session completed", { uploadId });
  } catch (error) {
    myLog("cancel upload session failed", {
      uploadId,
      statusCode: error && error.statusCode,
      message: error && (error.message || error.details && error.details.message),
    });
  }
}

function updateUploadProgressState(item, loadedBytes) {
  currentUploadBytes = Math.min(loadedBytes || 0, item.size || loadedBytes || 0);
  const total = Math.max(totalUploadBytes, 1);
  uploadProgress = Math.min(1, (processedUploadBytes + currentUploadBytes) / total);
  const progressBucket = Math.floor(uploadProgress * 100 / 10) * 10;
  if (item._lastProgressBucket !== progressBucket) {
    item._lastProgressBucket = progressBucket;
    myLog("upload progress", {
      name: item.name,
      loaded: currentUploadBytes,
      total: item.size,
      percent: Math.round(uploadProgress * 100),
    });
  }
}

function finishUploadItem(item) {
  processedUploadBytes += item.size || 0;
  currentUploadBytes = 0;
  uploadProgress = totalUploadBytes > 0 ? Math.min(1, processedUploadBytes / totalUploadBytes) : 1;
  myLog("finish upload item", {
    name: item.name,
    isFailed: !!item.isFailed,
    isSkipped: !!item.isSkipped,
    uploadedPath: item.uploadedPath,
    processedUploadBytes,
    totalUploadBytes,
  });
}

function startUploadSessionWithRetry(item, filePath, fileSize) {
  myLog("start upload session", {
    name: item.name,
    filePath,
    size: fileSize,
    conflictBehavior: item.conflictBehavior || "fail",
  });
  try {
    const startResult = Actions.callApi({
      url: "/StartFileUpload",
      method: "post",
      headers: {
        Accept: "application/json",
      },
      queryParams: {
        path: filePath,
      },
      body: {
        size: fileSize,
        conflictBehavior: item.conflictBehavior || "fail",
      },
    });
    myLog("start upload session success", {
      name: item.name,
      uploadId: startResult && startResult.uploadId,
    });
    return startResult;
  } catch (error) {
    myLog("start upload session error", {
      name: item.name,
      statusCode: error && error.statusCode,
      message: error && (error.message || error.details && error.details.message),
    });
    if (!(error && error.statusCode === 409)) throw error;

    const result = confirmUploadConflict(item.name);
    myLog("upload conflict resolution", { name: item.name, result });
    if (!result) {
      patchUploadItem(item, { isSkipped: true });
      return null;
    }

    item.conflictBehavior = result;
    const retryStartResult = Actions.callApi({
      url: "/StartFileUpload",
      method: "post",
      headers: {
        Accept: "application/json",
      },
      queryParams: {
        path: filePath,
      },
      body: {
        size: fileSize,
        conflictBehavior: result,
      },
    });
    myLog("retry start upload session success", {
      name: item.name,
      uploadId: retryStartResult && retryStartResult.uploadId,
      conflictBehavior: result,
    });
    return retryStartResult;
  }
}

function onProcessUploadQueuedItem(eventArgs) {
  const chunkUnitInBytes = 327680;
  const chunkSizeInBytes = 10 * chunkUnitInBytes;

  const item = eventArgs.item;
  const safeTargetPath = resolveTargetPath(item.targetPath);
  if (!safeTargetPath) throw "Missing target path for upload";
  const safeName = getBaseFileName(item.name);
  const filePath = window.MwdHelpers.joinPath(safeTargetPath, safeName);
  const fileFromRef = getUploadFileByRef(item.fileRefId);
  const rawFile = fileFromRef || item.file;
  const file = normalizeUploadFile(rawFile);
  if (!file) throw "Upload source file reference missing";
  item.targetPath = safeTargetPath;
  item.name = safeName;
  myLog("process queued item", {
    name: item.name,
    safeTargetPath,
    filePath,
    size: item.size,
    chunkSizeInBytes,
    fileRefId: item.fileRefId,
    fileSource: fileFromRef ? "ref" : "item",
    rawFileType: typeof rawFile,
    rawFileTag: Object.prototype.toString.call(rawFile),
    fileType: typeof file,
    fileTag: Object.prototype.toString.call(file),
    rawHasHandle: !!(rawFile && rawFile.handle),
    rawHandleTag: rawFile && rawFile.handle ? Object.prototype.toString.call(rawFile.handle) : undefined,
    rawHandleHasSlice: !!(rawFile && rawFile.handle && typeof rawFile.handle.slice === "function"),
    hasSize: typeof file.size === "number",
    hasSlice: typeof file.slice === "function",
  });

  if (typeof file.slice !== "function") {
    throw "Upload source is not a File/Blob (missing slice)";
  }

  let uploadId = null;
  try {
    const fileSize = Number(file.size) || item.size || 0;
    const startResult = startUploadSessionWithRetry(item, filePath, fileSize);
    if (item.isSkipped || !startResult) {
      myLog("item skipped", { name: item.name, filePath });
      finishUploadItem(item);
      return;
    }

    uploadId = startResult && startResult.uploadId;
    if (!uploadId) throw "StartFileUpload did not return uploadId";
    myLog("begin write blocks", { name: item.name, uploadId });
    myLog("write blocks onProgress mode", {
      name: item.name,
      mode: "direct-eventArgs.onProgress",
      hasOnProgress: !!eventArgs.onProgress,
    });

    Actions.upload({
      url: ({ $uploadParams }) =>
        "/WriteFileBlock?uploadId=" + uploadId + "&startPosition=" + $uploadParams.chunkStart,
      method: "put",
      headers: ({ $uploadParams }) => {
        return {
          "Content-Range":
            "bytes " +
            $uploadParams.chunkStart +
            "-" +
            $uploadParams.chunkEnd +
            "/" +
            $uploadParams.fileSize,
        };
      },
      chunkSizeInBytes: chunkSizeInBytes,
      file,
      onProgress: eventArgs.onProgress,
    });
    myLog("write blocks completed", { name: item.name, uploadId });

    myLog("complete upload call", { name: item.name, uploadId });
    Actions.callApi({
      url: "/CompleteUpload?uploadId=" + uploadId,
      method: "post",
    });
    myLog("complete upload success", { name: item.name, uploadId, filePath });

    patchUploadItem(item, { uploadedPath: filePath });
    finishUploadItem(item);
  } catch (error) {
    patchUploadItem(item, { isFailed: true });
    myLog("process queued item failed", {
      name: item.name,
      filePath,
      uploadId,
      rawErrorType: typeof error,
      rawError: String(error),
      rawErrorTag: Object.prototype.toString.call(error),
      rawErrorKeys: error && typeof error === "object" ? Object.keys(error) : [],
      statusCode: error && error.statusCode,
      errorCode: error && error.errorCode,
      message: error && (error.message || error.details && error.details.message),
    });
    cancelUploadSessionSafe(uploadId);
    finishUploadItem(item);
  }
}

function handleUploadClose() {
  if (!isFileOperationInProgress) isUploadDialogOpen = false;
  return !isFileOperationInProgress;
}

function onUploadComplete() {
  try {
    window.publishTopic("FilesContainer:refresh");

    const allItems = uploadQueueItems || [];
    const failedItems = allItems.filter((item) => item.isFailed);
    const skippedItems = allItems.filter((item) => item.isSkipped);
    const failedCount = failedItems.length;
    const skippedCount = skippedItems.length;
    const successCount = allItems.length - failedCount - skippedCount;

    const parts = [`Uploaded ${successCount} file(s)`];
    if (failedCount > 0) parts.push(`${failedCount} failed`);
    if (skippedCount > 0) parts.push(`${skippedCount} skipped`);
    const summary = parts.join(", ") + ".";
    myLog("upload queue complete", {
      successCount,
      failedCount,
      skippedCount,
      summary,
    });
    if (failedCount > 0) toast.error(summary);
    else toast.success(summary);
  } finally {
    clearUploadFileRefs(uploadQueueItems);
    selectedUploadFiles = [];
    uploadQueueItems = [];
    isUploadDialogOpen = false;
    isFileOperationInProgress = false;
    totalUploadBytes = 0;
    processedUploadBytes = 0;
    currentUploadBytes = 0;
    uploadProgress = 0;
  }
}
