import React from 'react';

function TabButton({ tab, isActive, onSelect, onClose }) {
  return (
    <div
      className={`tab-button ${isActive ? 'active' : ''}`}
      onClick={() => onSelect(tab.id)}
    >
      <span className="tab-name">{tab.name}</span>
      {tab.dirty && <span className="tab-dirty" aria-label="Unsaved changes">•</span>}
      <button
        className="tab-close"
        onClick={(event) => {
          event.stopPropagation();
          onClose(tab.id);
        }}
      >
        ×
      </button>
    </div>
  );
}

export default function Header({
  version = 'v0.1.0',
  tabs,
  activeTabId,
  onSelectTab,
  onCloseTab,
  onNew,
  onOpen,
  onSave,
  onToggleSettings,
}) {
  return (
    <header className="app-header">
      <div className="title-area">
        <h1>Lingramia <span className="app-version">{version}</span></h1>
      </div>
      <div className="actions-area">
        <button className="primary" onClick={onNew}>New</button>
        <button onClick={onOpen}>Open</button>
        <button onClick={onSave}>Save</button>
        <button disabled>Export</button>
        <button className="icon" onClick={onToggleSettings} title="Settings">
          ⚙️
        </button>
      </div>
      <div className="tabs-area">
        {tabs.map((tab) => (
          <TabButton
            key={tab.id}
            tab={tab}
            isActive={tab.id === activeTabId}
            onSelect={onSelectTab}
            onClose={onCloseTab}
          />
        ))}
        {tabs.length === 0 && <span className="tab-placeholder">No files opened</span>}
      </div>
    </header>
  );
}
