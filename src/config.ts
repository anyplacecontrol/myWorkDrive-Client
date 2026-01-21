import type { StandaloneAppDescription } from "xmlui";

const appDef: StandaloneAppDescription = {
  name: "MyWorkDrive",
  appGlobals: {
    apiUrl: "http://localhost:8357/api/v3",
    headers: {
      Authorization: "SessionID 12345",
    },
    errorResponseTransform: {
      code: "{$response.error.code}",
      message: "{$response.error.message}",
      details: "{$response.error.innerError}",
    },
    defaultToOptionalMemberAccess: false,
    filesTableColumns: {
      header: 52,
      name: 172,
      type: 90,
      lastModified: 160,
      size: 96,
      created: 160,
      actions: 70,
    },
    fileTileWidth: 140,
    targetSelectorModal: {
      height: 680,
      maxWidth: 1000
    }
  },
  resources: {
    logo: "resources/mwd-logo.svg",
    "logo-dark": "resources/mwd-logo-dark.svg",
    favicon: "resources/favicon.ico",
    "icon.empty-folder": "resources/empty-folder.svg",
  },
  defaultTheme: "windowsExplorer",
  // defaultTone: "dark",
  apiInterceptor: undefined
};

export default appDef;
