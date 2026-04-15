import { defineConfig, externalizeDepsPlugin } from "electron-vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  main: {
    plugins: [externalizeDepsPlugin()],
    build: {
      lib: {
        entry: "src/main/index.ts"
      }
    }
  },
  preload: {
    plugins: [externalizeDepsPlugin()],
    build: {
      lib: {
        entry: "src/main/preload.ts"
      }
    }
  },
  renderer: {
    build: {
      rollupOptions: {
        input: {
          index: "src/renderer/index.html"
        }
      }
    },
    plugins: [react()]
  }
});
