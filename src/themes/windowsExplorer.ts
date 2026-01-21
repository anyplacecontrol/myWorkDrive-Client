
import type { ThemeDefinition } from "xmlui";

export const WindowsExplorerTheme: ThemeDefinition = {
  name: "Windows Explorer",
  id: "windowsExplorer",
  themeVars: {
    "maxWidth-App": "$maxWidth-content",
    "boxShadow-navPanel-App": "none",
    "paddingRight-AppHeader": "$space-8",
    "paddingHorizontal-Footer": "$space-8",
    "textDecorationLine-Link": "none",
    "textColor-Link": "$textColor-primary",
    "maxWidth-ModalDialog": "calc($maxWidth-content * 0.8)",
    "padding-cell-Table": "0px 0px 0px 5px", // reduce cell padding   
    "borderBottom-cell-Table": "0px solid transparent", // remove cell bottom border

    "backgroundColor": "$color-primary-1",
    "backgroundColor--hover": "$color-primary-50", // light red
    "backgroundColor--selected": "$color-primary-100", // dark red
    // Tree-specific variables (control selection & focus appearance)
    "backgroundColor-Tree-row--selected": "$backgroundColor--selected",
    "backgroundColor-Tree-row--hover": "$backgroundColor--hover",
    "outlineColor-Tree--focus": "$color-secondary",
    
    "backgroundColor-row-Table--hover": "$backgroundColor--hover", // light red for table row hover
    "backgroundColor-selected-Table": "$backgroundColor--selected", // dark red for table row selected
    "backgroundColor-AppHeader": "$color-surface-50", // light blue
    "backgroundColor-heading-Table": "transparent", // transparent table header
    "backgroundColor-heading-Table--hover": "$backgroundColor--hover", // table header hover
    "textColor-heading-Table": "$textColor-secondary", // table header text color    
    "textColor-row-Table": "$textColor-primary", // table row text color

  },
  resources: {
    "icon.folder": "resources/onedrive-business-folder.svg",
    "icon.emptyfolder": "resources/onedrive-business-folder.svg",
    "icon.xls": "resources/onedrive-regular-xls.svg",
    "icon.doc": "resources/onedrive-regular-doc.svg",
    "icon.ppt": "resources/onedrive-regular-ppt.svg",
  },
};

export default WindowsExplorerTheme;
