export function TopBar() {
  return (
    <header className="top-bar">
      <div className="top-bar__drag-region">
        <div className="top-bar__cluster" data-testid="top-bar-brand">
          <div className="top-bar__identity">
            <span className="top-bar__app-dot" aria-hidden="true" />
            <p className="top-bar__eyebrow">Open DB Viewer</p>
          </div>
        </div>
      </div>
    </header>
  );
}
