import type { ReactNode } from "react";

export function PageHeader({
  title,
  description,
  action
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <section className="workspace-header" data-testid="workspace-header">
      <p className="workspace-header__eyebrow">Current Object</p>
      <div className="workspace-header__row">
        <h2>{title}</h2>
        <div className="workspace-header__side">
          {description ? <span className="workspace-header__meta">{description}</span> : null}
          {action ? <div className="workspace-header__action">{action}</div> : null}
        </div>
      </div>
    </section>
  );
}
