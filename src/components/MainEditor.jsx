import React from 'react';

const MainEditor = ({
  pages,
  selectedPageIndex,
  selectedField,
  onSelectField,
  onAddField,
  onUpdateField,
  onDeleteField,
  onAddVariant,
  onUpdateVariant,
  onDeleteVariant
}) => {
  if (selectedPageIndex === null || !pages[selectedPageIndex]) {
    return (
      <main className="main-editor">
        <div className="empty-state">
          Select a page from the sidebar to begin editing.
        </div>
      </main>
    );
  }

  const currentPage = pages[selectedPageIndex];
  const fields = currentPage.pageFiles || [];

  return (
    <main className="main-editor">
      <div className="editor-header">
        <h2>Page Fields</h2>
        <button className="btn btn-primary" onClick={onAddField}>
          + Add Field
        </button>
      </div>

      {fields.length === 0 ? (
        <div className="empty-state">
          No fields yet. Click "Add Field" to create one.
        </div>
      ) : (
        <div className="fields-container">
          {fields.map((field, fieldIndex) => (
            <div
              key={fieldIndex}
              className={`field-card ${
                selectedField?.pageIndex === selectedPageIndex &&
                selectedField?.fieldIndex === fieldIndex
                  ? 'selected'
                  : ''
              }`}
              onClick={() => onSelectField({ pageIndex: selectedPageIndex, fieldIndex })}
            >
              <div className="field-header">
                <div className="field-key-container">
                  <label>Key:</label>
                  <input
                    type="text"
                    className="field-key"
                    value={field.key}
                    onChange={(e) => {
                      e.stopPropagation();
                      onUpdateField(selectedPageIndex, fieldIndex, { key: e.target.value });
                    }}
                    onClick={(e) => e.stopPropagation()}
                  />
                </div>
                <div className="field-original-container">
                  <label>Original Value:</label>
                  <input
                    type="text"
                    className="field-original"
                    value={field.originalValue}
                    onChange={(e) => {
                      e.stopPropagation();
                      onUpdateField(selectedPageIndex, fieldIndex, {
                        originalValue: e.target.value
                      });
                    }}
                    onClick={(e) => e.stopPropagation()}
                  />
                </div>
                <button
                  className="btn btn-danger btn-small"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDeleteField(selectedPageIndex, fieldIndex);
                  }}
                  title="Delete Field"
                >
                  Delete
                </button>
              </div>

              <div className="variants-section">
                <div className="variants-header">
                  <h4>Variants ({field.variants.length})</h4>
                  <button
                    className="btn btn-small"
                    onClick={(e) => {
                      e.stopPropagation();
                      onAddVariant(selectedPageIndex, fieldIndex);
                    }}
                  >
                    + Add Variant
                  </button>
                </div>

                {field.variants.length === 0 ? (
                  <div className="variants-empty">No variants yet</div>
                ) : (
                  <div className="variants-list">
                    {field.variants.map((variant, variantIndex) => (
                      <div key={variantIndex} className="variant-item">
                        <div className="variant-language">
                          <label>Language:</label>
                          <input
                            type="text"
                            value={variant.language}
                            onChange={(e) => {
                              e.stopPropagation();
                              onUpdateVariant(selectedPageIndex, fieldIndex, variantIndex, {
                                language: e.target.value
                              });
                            }}
                            onClick={(e) => e.stopPropagation()}
                            placeholder="en"
                          />
                        </div>
                        <div className="variant-value">
                          <label>Translation:</label>
                          <input
                            type="text"
                            value={variant._value}
                            onChange={(e) => {
                              e.stopPropagation();
                              onUpdateVariant(selectedPageIndex, fieldIndex, variantIndex, {
                                _value: e.target.value
                              });
                            }}
                            onClick={(e) => e.stopPropagation()}
                          />
                        </div>
                        <button
                          className="btn btn-danger btn-small"
                          onClick={(e) => {
                            e.stopPropagation();
                            onDeleteVariant(selectedPageIndex, fieldIndex, variantIndex);
                          }}
                          title="Delete Variant"
                        >
                          ×
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </main>
  );
};

export default MainEditor;
