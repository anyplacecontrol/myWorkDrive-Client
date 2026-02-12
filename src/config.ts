import type { StandaloneAppDescription } from "xmlui";

const appDef: StandaloneAppDescription = {
  name: "MyWorkDrive",
  appGlobals: {
    //xsVerbose: true,
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
  },
  defaultTheme: "myWorkDrive",
  apiInterceptor: undefined
};

export default appDef;
