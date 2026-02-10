
import type { ThemeDefinition } from "xmlui";

export const MyWorkDriveTheme: ThemeDefinition = {
  name: "MyWorkDrive",
  id: "myWorkDrive",
  themeVars: {
    "maxWidth-App": "$maxWidth-content",

    //Modal Dialog
    "maxWidth-ModalDialog": "calc($maxWidth-content * 0.8)",
    "borderRadius-ModalDialog": "$space-3",

    //General texts
    "textColor--disabled": "$color-secondary-400",

    //General backgrounds
    "backgroundColor": "white",
    "backgroundColor--hover": "$color-primary-50",
    "backgroundColor--selected": "$color-primary-100",

     //App Header
    "paddingRight-AppHeader": "$space-4",
    "backgroundColor-AppHeader": "$color-primary-1",
    "paddingHorizontal-AppHeader": "0",

    //Links
    "textDecorationLine-Link": "none",
    "textColor-Link": "$textColor-primary",

    // Tree-specific variables
    "backgroundColor-Tree-row--selected": "$backgroundColor--selected",
    "backgroundColor-Tree-row--hover": "$backgroundColor--hover",
    "outlineColor-Tree--focus": "$color-secondary",

    //Table elements
    "backgroundColor-heading-Table": "white", // transparent table header
    "backgroundColor-heading-Table--hover": "$backgroundColor--hover", // table header hover
    "backgroundColor-row-Table--hover": "$backgroundColor--hover", //  row hover
    "backgroundColor-selected-Table": "$backgroundColor--selected", // row selected
    "textColor-heading-Table": "$textColor-secondary", // table header text color
    "textColor-row-Table": "$textColor-primary", // table row text color
    "padding-cell-Table": "9px 0px 10px 5px", // cell padding
    "borderBottom-cell-Table": "0px solid transparent", // remove cell bottom border

    //Toolbar
    "fgColor-Toolbar": "$textColor-primary",
    "fgColor-Toolbar--selected": "$color-primary-400",

    //inputs
    "borderColor-Input-default": "$color-secondary-200",
    "textColor-placeholder-TextBox--default": "$textColor--disabled",
    "color-adornment-TextBox--default": "$textColor-primary",

    //Badge
    "border-Badge": "1px solid $color-secondary-300",
    "backgroundColor-Badge": "transparent",
    "textColor-Badge": "$textColor-primary",

    //Splitter
    "thickness-resizer-Splitter": "$space-1",
    "backgroundColor-resizer-Splitter": "transparent",

    //Buttons
    "borderColor-Button-primary-outlined": "$color-secondary-300",

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
    "icon.btn-mediumIcons": "resources/btn-mediumIcons.svg",
    //icons
    "icon.circle_info": "resources/circle-info.svg",
    "icon.circle_delete": "resources/circle-delete.svg",
    //file type icons
    "icon.folder": "resources/folder.svg",
    "icon.shared_folder": "resources/shared-folder.svg",
    "icon.emptyfolder": "resources/folder.svg",
    "icon.xls": "resources/onedrive-regular-xls.svg",
    "icon.doc": "resources/onedrive-regular-doc.svg",
    "icon.ppt": "resources/onedrive-regular-ppt.svg",
    // menu icons
    "icon.cmd_add_zip": "resources/cmd-add-zip.svg",
    "icon.cmd_copy": "resources/cmd-copy.svg",
    "icon.cmd_cut": "resources/cmd-cut.svg",
    "icon.cmd_delete": "resources/cmd-delete.svg",
    "icon.cmd_download_zip": "resources/cmd-download-zip.svg",
    "icon.cmd_paste": "resources/cmd-paste.svg",
    "icon.cmd_properties": "resources/cmd-properties.svg",
    "icon.cmd_rename": "resources/cmd-rename.svg",
    "icon.cmd_share": "resources/cmd-share.svg",
    "icon.cmd_upload_files": "resources/cmd-upload-files.svg",
    "icon.cmd_upload_folder": "resources/cmd-upload-folder.svg",
    // new/updated icons
    "icon.cmd_download_file": "resources/cmd-download-file.svg",
    "icon.cmd_save_copy": "resources/cmd-save-copy.svg",
    "icon.cmd_view_file": "resources/cmd-view-file.svg",
  },
};

export default MyWorkDriveTheme;
