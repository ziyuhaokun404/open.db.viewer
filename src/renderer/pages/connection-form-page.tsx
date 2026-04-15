import { AppShell } from "../app/app-shell";
import { ConnectionForm } from "../features/connections/connection-form";

export function ConnectionFormPage() {
  return <AppShell sidebar={<p>保存后显示在左侧。</p>}><ConnectionForm /></AppShell>;
}
