import PropTypes from 'prop-types';
import clsx from 'clsx';

function TopBar({ tabs, activeTabId, onSelectTab, onCloseTab, onNewFile, onOpenFile, onSaveFile }) {
  return (
    <header className="top-bar">
      <div className="app-title">
        <h1>Lingramia</h1>
        <span className="version-tag">v0.1.0</span>
      </div>
      <div className="toolbar">
        <button type="button" onClick={onNewFile} className="toolbar-btn">
          New
        </button>
        <button type="button" onClick={onOpenFile} className="toolbar-btn">
          Open
        </button>
        <button type="button" onClick={onSaveFile} className="toolbar-btn primary">
          Save
        </button>
        <button type="button" className="toolbar-btn" disabled>
          Export
        </button>
      </div>
      <nav className="tab-strip" aria-label="Open files">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={clsx('tab', { active: tab.id === activeTabId, dirty: tab.dirty })}
            onClick={() => onSelectTab(tab.id)}
          >
            <span className="tab-title">{tab.title}</span>
            {tab.dirty && <span className="unsaved-indicator">●</span>}
            {tabs.length > 1 && (
              <span
                role="button"
                tabIndex={0}
                className="tab-close"
                onClick={(event) => {
                  event.stopPropagation();
                  onCloseTab(tab.id);
                }}
              >
                ×
              </span>
            )}
          </button>
        ))}
      </nav>
      <button type="button" className="settings-btn" disabled>
        ⚙️ Settings
      </button>
    </header>
  );
}

TopBar.propTypes = {
  tabs: PropTypes.arrayOf(
    PropTypes.shape({
      id: PropTypes.string.isRequired,
      title: PropTypes.string.isRequired,
      dirty: PropTypes.bool,
    }),
  ).isRequired,
  activeTabId: PropTypes.string,
  onSelectTab: PropTypes.func.isRequired,
  onCloseTab: PropTypes.func.isRequired,
  onNewFile: PropTypes.func.isRequired,
  onOpenFile: PropTypes.func.isRequired,
  onSaveFile: PropTypes.func.isRequired,
};

TopBar.defaultProps = {
  activeTabId: null,
};

export default TopBar;
