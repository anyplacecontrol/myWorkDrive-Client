// Define the initial state and functions for file operations
var fileOperationsInitial = {
  // internal clipboard holds copied/cut items (mode: 'copy'|'cut', items: [])
  clipboard: null,

  copyOrCut: (items, action) => {
    const list = items || [];
    const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
    toast.success((action==="copy" ? "Copied ": 'Cut ') + names);
  },
  paste: (items) => {
    const list = items || [];
    const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
    toast.success("Pasted " + names + " item(s)");
  },
};
