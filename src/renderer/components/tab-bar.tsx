export function TabBar({
  tabs,
  activeTab,
  onChange
}: {
  tabs: string[];
  activeTab: string;
  onChange: (tab: string) => void;
}) {
  return (
    <div aria-label="workspace-tabs" className="workspace-tabs" data-testid="workspace-tabs" role="tablist">
      {tabs.map((tab) => (
        <button
          className="workspace-tabs__tab"
          key={tab}
          aria-pressed={tab === activeTab}
          onClick={() => onChange(tab)}
          type="button"
        >
          {tab}
        </button>
      ))}
    </div>
  );
}
