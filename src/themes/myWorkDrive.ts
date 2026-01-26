
import type { ThemeDefinition } from "xmlui";

export const MyWorkDriveTheme: ThemeDefinition = {
  name: "MyWorkDrive",
  id: "myWorkDrive",
  themeVars: {
    "maxWidth-App": "$maxWidth-content",
    "boxShadow-navPanel-App": "none",

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

     //App Header
    "paddingRight-AppHeader": "$space-4",
    "backgroundColor-AppHeader": "$color-primary-1",
    "paddingHorizontal-AppHeader": "0",

    //Table elements
    "backgroundColor-heading-Table": "transparent", // transparent table header
    "backgroundColor-heading-Table--hover": "$backgroundColor--hover", // table header hover
    "backgroundColor-row-Table--hover": "$backgroundColor--hover", //  row hover
    "backgroundColor-selected-Table": "$backgroundColor--selected", // row selected
    "textColor-heading-Table": "$textColor-secondary", // table header text color
    "textColor-row-Table": "$textColor-primary", // table row text color

    //Toolbar
    "toolbar-fg-color": "$textColor-primary",
  },
  resources: {
    //button icons
    "icon.btn_folders": "resources/btn-folders.svg",
    "icon.btn_favorites": "resources/btn-favorites.svg",
    "icon.btn_up": "resources/btn-up.svg",
    "icon.btn_refresh": "resources/btn-refresh.svg",
    "icon.btn_upload": "resources/btn-upload.svg",
    "icon.btn_new": "resources/btn-new.svg",
    "icon.btn_view": "resources/btn-view.svg",
    //file type icons
    "icon.folder": "resources/folder.svg",
    "icon.emptyfolder": "resources/folder.svg",
    "icon.xls": "resources/onedrive-regular-xls.svg",
    "icon.doc": "resources/onedrive-regular-doc.svg",
    "icon.ppt": "resources/onedrive-regular-ppt.svg",
  },
};

export default MyWorkDriveTheme;
