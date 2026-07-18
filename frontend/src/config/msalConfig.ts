import { PublicClientApplication, type Configuration, type RedirectRequest } from "@azure/msal-browser";

/** MSAL config for Entra sign-in. Only used when REACT_APP_AUTH_MODE=entra. */
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.REACT_APP_ENTRA_CLIENT_ID ?? "",
    authority: import.meta.env.REACT_APP_ENTRA_AUTHORITY ?? "",
    redirectUri: typeof window !== "undefined" ? window.location.origin : "/",
  },
  cache: { cacheLocation: "sessionStorage" },
};

export const msalInstance = new PublicClientApplication(msalConfig);

const scope = import.meta.env.REACT_APP_VSS_API_SCOPE;
export const loginRequest: RedirectRequest = { scopes: ["openid", "profile", "email"] };
export const apiRequest = { scopes: scope ? [scope] : [] };
