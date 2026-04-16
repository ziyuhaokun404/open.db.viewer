import React from "react";
import ReactDOM from "react-dom/client";
import { App } from "./app/App";
import { ThemeSync } from "./app/theme-sync";
import "./styles/base.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeSync />
    <App />
  </React.StrictMode>
);
