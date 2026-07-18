import React from "react";
import { createRoot } from "react-dom/client";
import "./theme/univerus-tokens.css";
import { loadUdpRuntimeConfig } from "./udp-runtime-config";
import App from "./App";

// Unity convention: populate ConfigService from env BEFORE the first render.
loadUdpRuntimeConfig();

createRoot(document.getElementById("root")).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
