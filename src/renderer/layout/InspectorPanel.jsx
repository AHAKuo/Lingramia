import PropTypes from 'prop-types';

function InspectorPanel({ page, entry }) {
  return (
    <aside className="inspector">
      <h2>Inspector</h2>
      {!page && (
        <p className="empty-state">Select a page to view its details.</p>
      )}
      {page && (
        <section className="inspector-section">
          <h3>Page details</h3>
          <dl>
            <dt>Page ID</dt>
            <dd>{page.pageId}</dd>
            <dt>About</dt>
            <dd>{page.aboutPage || '—'}</dd>
            <dt>Entries</dt>
            <dd>{page.pageFiles.length}</dd>
          </dl>
        </section>
      )}
      {entry && (
        <section className="inspector-section">
          <h3>Entry details</h3>
          <dl>
            <dt>Key</dt>
            <dd>{entry.key || '—'}</dd>
            <dt>Original value</dt>
            <dd>{entry.originalValue || '—'}</dd>
            <dt>Variants</dt>
            <dd>{entry.variants.length}</dd>
          </dl>
          <button type="button" className="ghost" disabled>
            Bulk translate
          </button>
          <button type="button" className="ghost" disabled>
            Duplicate entry
          </button>
        </section>
      )}
      {!entry && page && (
        <p className="empty-state">Select an entry to inspect translations.</p>
      )}
    </aside>
  );
}

InspectorPanel.propTypes = {
  page: PropTypes.shape({
    pageId: PropTypes.string,
    aboutPage: PropTypes.string,
    pageFiles: PropTypes.array,
  }),
  entry: PropTypes.shape({
    key: PropTypes.string,
    originalValue: PropTypes.string,
    variants: PropTypes.array,
  }),
};

InspectorPanel.defaultProps = {
  page: null,
  entry: null,
};

export default InspectorPanel;
