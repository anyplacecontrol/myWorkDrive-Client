// Code-behind for Main.xmlui
// Define the initial state and functions for file operations
var fileOperationsInitial = {
  // internal clipboard holds copied/cut items (mode: 'copy'|'cut', items: [])
  clipboard: null,
  // copy: store items into clipboard and toast concatenated names
  copy: (items) => {
      const list = items || [];

      // store clipboard state in AppState
      //AppState.update("fileOps", { clipboard: { mode: "copy", items: list } });

      const names = list.map((i) => (i && i.name ? i.name : "")).join(", ");
      toast.success("Copied " + names + " item(s)");
  }


};
