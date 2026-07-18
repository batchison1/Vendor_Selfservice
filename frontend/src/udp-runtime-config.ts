import { ConfigService } from "@univerus/udp-react-enterprise-component-library";

/**
 * Populate ConfigService from build-time env. Per Unity convention this runs in
 * src/index.jsx BEFORE createRoot().render() so config is ready for the first render.
 */
export function loadUdpRuntimeConfig() {
  ConfigService.loadConfigObject({
    UNITY_API_DOMAIN: import.meta.env.REACT_APP_UNITY_API_DOMAIN ?? "https://gateway.unitydev.ca",
    UNITY_URL: import.meta.env.REACT_APP_UNITY_URL ?? "https://unitydev.ca",
    UNITY_TENANT_ID: import.meta.env.REACT_APP_UNITY_TENANT_ID ?? "",
    UNITY_PRODUCT_ID: import.meta.env.REACT_APP_UNITY_PRODUCT_ID ?? "vss",
    UNITY_VERTICAL_ID: import.meta.env.REACT_APP_UNITY_VERTICAL_ID ?? "",
  });
}
