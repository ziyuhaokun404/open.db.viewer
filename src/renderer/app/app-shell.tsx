import type { ReactNode } from "react";
import { TopBar } from "../components/top-bar";
import "./app-layout.css";

export function AppShell({
  sidebar,
  children,
  footer
}: {
  sidebar: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="app-shell">
      <TopBar />
      <div className="app-shell__body">
        <aside className="app-shell__sidebar">{sidebar}</aside>
        <main className="app-shell__main">{children}</main>
      </div>
      {footer ? <footer className="app-shell__footer">{footer}</footer> : null}
    </div>
  );
}
