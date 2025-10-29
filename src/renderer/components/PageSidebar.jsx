import React from 'react';

export default function PageSidebar({ pages, selectedPageId, onSelectPage, onAddPage }) {
  return (
    <aside className="page-sidebar">
      <div className="sidebar-header">
        <h2>Pages</h2>
        <button onClick={onAddPage}>＋</button>
      </div>
      <ul>
        {pages.map((page) => (
          <li
            key={page.pageId}
            className={page.pageId === selectedPageId ? 'active' : ''}
            onClick={() => onSelectPage(page.pageId)}
          >
            <span>{page.pageId}</span>
            <span className="page-count">{page.pageFiles.length} fields</span>
          </li>
        ))}
        {pages.length === 0 && <li className="empty">No pages yet</li>}
      </ul>
    </aside>
  );
}
