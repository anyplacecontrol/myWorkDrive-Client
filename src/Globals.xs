// true If current file operation is in progress,
// must be set to false in the end of the operation
var gIsFileOperationInProgress = false;

// Global reactive clipboard for file copy/cut operations.
// Use the provided setters/getters to keep usage consistent across modules.
var gFileClipboard = { items: [], action: null };

// Controls visibility of the folders tree.
var gIsFoldersVisible = true;

// View and sorting globals
var gView = 'table';
var gSortBy = 'name';
var gSortDirection = 'ascending';

function gSetFileClipboard(value) {
	gFileClipboard = value;
}

function gClearFileClipboard() {
	gFileClipboard = { items: [], action: null };
}

