import { BrowserPage } from "../pages/browser-page";
import { ConnectionFormPage } from "../pages/connection-form-page";
import { HomePage } from "../pages/home-page";
import { useConnectionStore } from "../stores/connection-store";

export function App() {
  const currentView = useConnectionStore((state) => state.currentView);

  if (currentView === "new-connection") {
    return <ConnectionFormPage />;
  }

  if (currentView === "browser") {
    return <BrowserPage />;
  }

  return <HomePage />;
}
