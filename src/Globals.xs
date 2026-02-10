// true If current file operation is in progress,
// must be set to false in the end of the operation
var isFileOperationInProgress = false;

// Global reactive clipboard for file copy/cut operations.
// Use the provided setters/getters to keep usage consistent across modules.
var fileClipboard = { items: [], action: null };

function setFileClipboard(value) {
	// assign to reactive var
	fileClipboard = value;
}
function clearFileClipboard() {
	fileClipboard = { items: [], action: null };
}

