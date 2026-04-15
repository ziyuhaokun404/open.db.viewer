import type { ReactNode } from "react";

export function EmptyState({
  title,
  message,
  action
}: {
  title: string;
  message: string;
  action?: ReactNode;
}) {
  return (
    <section className="empty-state">
      <h2>{title}</h2>
      <p>{message}</p>
      {action ? <div>{action}</div> : null}
    </section>
  );
}
