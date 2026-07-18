import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// The @univerus component library lives on a private registry and can't install
// locally. This alias points the (convention-perfect) import at an in-repo dev stub.
// On the Univerus network: delete this alias entry and `npm i` the real package.
const UDP = "@univerus/udp-react-enterprise-component-library";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      [UDP]: path.resolve(import.meta.dirname, "dev-stubs/udp-react-enterprise-component-library/index.tsx"),
    },
  },
  server: {
    port: 5173,
    // Proxy API calls to the backend so the app is single-origin (needed when sharing via a
    // tunnel — the browser only ever talks to the frontend origin). Set
    // REACT_APP_VSS_API_DOMAIN="" (see .env.local) so client calls are relative "/api/...".
    proxy: { "/api": "http://localhost:5047" },
    // Allow the app to be reached through a Cloudflare quick tunnel (temporary external share).
    allowedHosts: [".trycloudflare.com", "localhost", "127.0.0.1"],
  },
  // Univerus reads REACT_APP_* via import.meta.env — expose that prefix (plus VITE_).
  envPrefix: ["VITE_", "REACT_APP_"],
});
