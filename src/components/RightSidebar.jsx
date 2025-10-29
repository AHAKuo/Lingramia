import React from 'react';

const RightSidebar = ({
  pages,
  selectedPageIndex,
  selectedField,
  onUpdatePage,
  onUpdateField,
  onAddVariant,
  onUpdateVariant,
  onDeleteVariant
}) => {
  // Show page inspector
  if (selectedPageIndex !== null && !selectedField) {
    const page = pages[selectedPageIndex];
    if (!page) return null;

    return (
      <aside className="right-sidebar">
        <div className="inspector-header">
          <h2>Page Inspector</h2>
        </div>
        
        <div className="inspector-content">
          <div className="inspector-field">
            <label>Page ID</label>
            <input
              type="text"
              value={page.pageId}
              onChange={(e) => onUpdatePage(selectedPageIndex, { pageId: e.target.value })}
            />
          </div>

          <div className="inspector-field">
            <label>About Page</label>
            <textarea
              value={page.aboutPage}
              onChange={(e) => onUpdatePage(selectedPageIndex, { aboutPage: e.target.value })}
              placeholder="Description of this page..."
              rows={4}
            />
          </div>

          <div className="inspector-stats">
            <div className="stat-item">
              <span className="stat-label">Number of Fields:</span>
              <span className="stat-value">{page.pageFiles.length}</span>
            </div>
            <div className="stat-item">
              <span className="stat-label">Total Variants:</span>
              <span className="stat-value">
                {page.pageFiles.reduce((sum, field) => sum + field.variants.length, 0)}
              </span>
            </div>
          </div>
        </div>
      </aside>
    );
  }

  // Show field inspector
  if (selectedField) {
    const { pageIndex, fieldIndex } = selectedField;
    const field = pages[pageIndex]?.pageFiles[fieldIndex];
    if (!field) return null;

    return (
      <aside className="right-sidebar">
        <div className="inspector-header">
          <h2>Field Inspector</h2>
        </div>
        
        <div className="inspector-content">
          <div className="inspector-field">
            <label>Key</label>
            <input
              type="text"
              value={field.key}
              onChange={(e) =>
                onUpdateField(pageIndex, fieldIndex, { key: e.target.value })
              }
            />
          </div>

          <div className="inspector-field">
            <label>Original Value</label>
            <textarea
              value={field.originalValue}
              onChange={(e) =>
                onUpdateField(pageIndex, fieldIndex, { originalValue: e.target.value })
              }
              rows={3}
            />
          </div>

          <div className="inspector-section">
            <div className="section-header">
              <h3>Variants ({field.variants.length})</h3>
              <button
                className="btn btn-small"
                onClick={() => onAddVariant(pageIndex, fieldIndex)}
              >
                + Add
              </button>
            </div>

            {field.variants.length === 0 ? (
              <div className="empty-state-small">No variants</div>
            ) : (
              <div className="variants-inspector">
                {field.variants.map((variant, variantIndex) => (
                  <div key={variantIndex} className="variant-inspector-item">
                    <div className="inspector-field">
                      <label>Language</label>
                      <input
                        type="text"
                        value={variant.language}
                        onChange={(e) =>
                          onUpdateVariant(pageIndex, fieldIndex, variantIndex, {
                            language: e.target.value
                          })
                        }
                      />
                    </div>
                    <div className="inspector-field">
                      <label>Translation</label>
                      <textarea
                        value={variant._value}
                        onChange={(e) =>
                          onUpdateVariant(pageIndex, fieldIndex, variantIndex, {
                            _value: e.target.value
                          })
                        }
                        rows={2}
                      />
                    </div>
                    <button
                      className="btn btn-danger btn-small btn-full"
                      onClick={() => onDeleteVariant(pageIndex, fieldIndex, variantIndex)}
                    >
                      Delete Variant
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </aside>
    );
  }

  // Default view
  return (
    <aside className="right-sidebar">
      <div className="inspector-header">
        <h2>Inspector</h2>
      </div>
      <div className="inspector-content">
        <div className="empty-state">
          Select a page or field to view details.
        </div>
      </div>
    </aside>
  );
};

export default RightSidebar;
