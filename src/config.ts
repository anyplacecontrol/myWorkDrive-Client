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
    fileTileWidth: 140,
  },
  resources: {
    favicon: "resources/favicon.ico",
  //  "icon.empty-folder": "resources/folder.svg",
  },
  defaultTheme: "myWorkDrive",
  apiInterceptor: undefined
};

export default appDef;
