import React from 'react';

const LeftSidebar = ({ pages, selectedPageIndex, onSelectPage, onAddPage, onDeletePage }) => {
  return (
    <aside className="left-sidebar">
      <div className="sidebar-header">
        <h2>Pages</h2>
        <button className="btn btn-small" onClick={onAddPage} title="Add Page">
          + Add
        </button>
      </div>
      
      <div className="pages-list">
        {pages.length === 0 ? (
          <div className="empty-state">
            No pages yet. Click "Add" to create a page.
          </div>
        ) : (
          pages.map((page, index) => (
            <div
              key={index}
              className={`page-item ${selectedPageIndex === index ? 'active' : ''}`}
              onClick={() => onSelectPage(index)}
            >
              <div className="page-info">
                <div className="page-id">
                  Page ID: {page.pageId || 'Unnamed'}
                </div>
                {page.aboutPage && (
                  <div className="page-about">{page.aboutPage}</div>
                )}
                <div className="page-stats">
                  {page.pageFiles.length} field{page.pageFiles.length !== 1 ? 's' : ''}
                </div>
              </div>
              <button
                className="btn btn-danger btn-small"
                onClick={(e) => {
                  e.stopPropagation();
                  onDeletePage(index);
                }}
                title="Delete Page"
              >
                ×
              </button>
            </div>
          ))
        )}
      </div>
    </aside>
  );
};

export default LeftSidebar;
