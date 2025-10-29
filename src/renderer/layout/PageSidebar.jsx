import PropTypes from 'prop-types';

function PageSidebar({ pages, selection, onAddPage, onSelectPage, onRenamePage, onDeletePage }) {
  const handleRename = (event, page) => {
    const value = event.target.value;
    onRenamePage(page.pageId, value);
  };

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <h2>Pages</h2>
        <button type="button" onClick={onAddPage} className="sidebar-action">
          +
        </button>
      </div>
      <ul className="page-list">
        {pages.map((page) => {
          const isActive = selection?.pageId === page.pageId;
          return (
            <li key={page.pageId} className={isActive ? 'active' : ''}>
              <button type="button" onClick={() => onSelectPage(page.pageId)} className="page-select">
                <span className="page-title">{page.aboutPage || page.pageId}</span>
              </button>
              <input
                type="text"
                value={page.aboutPage || ''}
                placeholder="Page title"
                onChange={(event) => handleRename(event, page)}
              />
              <div className="page-actions">
                <button type="button" onClick={() => onDeletePage(page.pageId)} title="Delete page">
                  🗑️
                </button>
              </div>
            </li>
          );
        })}
        {pages.length === 0 && <li className="empty-state">No pages yet. Create one to begin.</li>}
      </ul>
    </aside>
  );
}

PageSidebar.propTypes = {
  pages: PropTypes.arrayOf(
    PropTypes.shape({
      pageId: PropTypes.string.isRequired,
      aboutPage: PropTypes.string,
    }),
  ).isRequired,
  selection: PropTypes.shape({
    pageId: PropTypes.string,
  }),
  onAddPage: PropTypes.func.isRequired,
  onSelectPage: PropTypes.func.isRequired,
  onRenamePage: PropTypes.func.isRequired,
  onDeletePage: PropTypes.func.isRequired,
};

PageSidebar.defaultProps = {
  selection: null,
};

export default PageSidebar;
