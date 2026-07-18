/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly REACT_APP_AUTH_MODE?: "dev" | "entra";
  readonly REACT_APP_VSS_API_DOMAIN?: string;
  readonly REACT_APP_UNITY_API_DOMAIN?: string;
  readonly REACT_APP_UNITY_URL?: string;
  readonly REACT_APP_UNITY_TENANT_ID?: string;
  readonly REACT_APP_UNITY_PRODUCT_ID?: string;
  readonly REACT_APP_UNITY_VERTICAL_ID?: string;
  readonly REACT_APP_ENTRA_CLIENT_ID?: string;
  readonly REACT_APP_ENTRA_AUTHORITY?: string;
  readonly REACT_APP_VSS_API_SCOPE?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
