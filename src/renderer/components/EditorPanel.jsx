import PropTypes from 'prop-types';
import { createEmptyField, createVariant } from '../../models/locbookModel';

function EditorPanel({ page, onUpdatePage, selection, onSelectEntry }) {
  if (!page) {
    return (
      <div className="panel-placeholder">
        <h2>No page selected</h2>
        <p>Create or select a page to begin editing translations.</p>
      </div>
    );
  }

  const handleAddField = () => {
    const field = createEmptyField();
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: [...currentPage.pageFiles, field],
    }));
    onSelectEntry(page.pageId, field.entryId);
  };

  const handleUpdateField = (entryId, payload) => {
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: currentPage.pageFiles.map((file) =>
        file.entryId === entryId
          ? {
              ...file,
              ...payload,
            }
          : file,
      ),
    }));

    onSelectEntry(page.pageId, entryId);
  };

  const handleRemoveField = (entryId) => {
    if (!window.confirm('Delete this entry?')) return;
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: currentPage.pageFiles.filter((file) => file.entryId !== entryId),
    }));
  };

  const handleAddVariant = (entryId) => {
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: currentPage.pageFiles.map((file) =>
        file.entryId === entryId ? { ...file, variants: [...file.variants, createVariant()] } : file,
      ),
    }));
  };

  const handleUpdateVariant = (entryId, index, variantPatch) => {
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: currentPage.pageFiles.map((file) => {
        if (file.entryId !== entryId) return file;
        const variants = file.variants.map((variant, idx) =>
          idx === index ? { ...variant, ...variantPatch } : variant,
        );
        return { ...file, variants };
      }),
    }));
  };

  const handleRemoveVariant = (entryId, index) => {
    onUpdatePage(page.pageId, (currentPage) => ({
      ...currentPage,
      pageFiles: currentPage.pageFiles.map((file) => {
        if (file.entryId !== entryId) return file;
        const variants = file.variants.filter((_, idx) => idx !== index);
        return { ...file, variants };
      }),
    }));
  };

  return (
    <section className="editor-panel">
      <header className="editor-header">
        <div>
          <h2>Page: {page.aboutPage || page.pageId}</h2>
          <p className="page-meta">{page.pageFiles.length} entries</p>
        </div>
        <button type="button" onClick={handleAddField} className="primary">
          Add entry
        </button>
      </header>
      <div className="editor-table-wrapper">
        <table className="editor-table">
          <thead>
            <tr>
              <th>Key</th>
              <th>Original Value</th>
              <th>Variants</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {page.pageFiles.map((file, index) => {
              const rowKey = file.entryId ?? `${page.pageId}-entry-${index}`;
              const isSelected = selection?.entryId === file.entryId;
              return (
                <tr key={rowKey} className={isSelected ? 'selected' : ''}>
                  <td>
                    <input
                      type="text"
                      value={file.key}
                      onChange={(event) => handleUpdateField(file.entryId, { key: event.target.value })}
                      onFocus={() => onSelectEntry(page.pageId, file.entryId)}
                      placeholder="key_identifier"
                    />
                  </td>
                  <td>
                    <textarea
                      value={file.originalValue}
                      onChange={(event) => handleUpdateField(file.entryId, { originalValue: event.target.value })}
                      placeholder="Original text"
                    />
                  </td>
                  <td>
                    <div className="variants-grid">
                      {file.variants.map((variant, index) => (
                        <div key={`${file.entryId}-${variant.language}-${index}`} className="variant-card">
                          <div className="variant-header">
                            <input
                              type="text"
                              value={variant.language}
                              onChange={(event) =>
                                handleUpdateVariant(file.entryId, index, { language: event.target.value })
                              }
                              placeholder="lang"
                            />
                            <button type="button" onClick={() => handleRemoveVariant(file.entryId, index)}>
                              ×
                            </button>
                          </div>
                          <textarea
                            value={variant._value}
                            onChange={(event) =>
                              handleUpdateVariant(file.entryId, index, { _value: event.target.value })
                            }
                            placeholder="Translation"
                          />
                          <button
                            type="button"
                            className="ghost"
                            disabled
                            title="Auto-translate coming soon"
                          >
                            💬 Auto
                          </button>
                        </div>
                      ))}
                      <button type="button" className="ghost add-variant" onClick={() => handleAddVariant(file.entryId)}>
                        + Variant
                      </button>
                    </div>
                  </td>
                  <td>
                    <button type="button" className="ghost" onClick={() => handleRemoveField(file.entryId)}>
                      Delete
                    </button>
                  </td>
                </tr>
              );
            })}
            {page.pageFiles.length === 0 && (
              <tr>
                <td colSpan={4} className="empty-state">
                  No entries yet. Click "Add entry" to create your first translation key.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

EditorPanel.propTypes = {
  page: PropTypes.shape({
    pageId: PropTypes.string.isRequired,
    aboutPage: PropTypes.string,
    pageFiles: PropTypes.arrayOf(
      PropTypes.shape({
        entryId: PropTypes.string.isRequired,
        key: PropTypes.string.isRequired,
        originalValue: PropTypes.string,
        variants: PropTypes.arrayOf(
          PropTypes.shape({
            _value: PropTypes.string,
            language: PropTypes.string,
          }),
        ),
      }),
    ),
  }),
  onUpdatePage: PropTypes.func.isRequired,
  selection: PropTypes.shape({ entryId: PropTypes.string }),
  onSelectEntry: PropTypes.func.isRequired,
};

EditorPanel.defaultProps = {
  page: null,
  selection: null,
};

export default EditorPanel;
