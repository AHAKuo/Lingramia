import React from 'react';

export default function EditorPanel({
  page,
  selectedFieldKey,
  onSelectField,
  onAddField,
  onUpdateField,
  onUpsertVariant,
  onRemoveVariant,
  onAutoTranslate,
}) {
  if (!page) {
    return (
      <section className="editor-panel empty">
        <p>Select or create a page to begin editing.</p>
      </section>
    );
  }

  return (
    <section className="editor-panel">
      <header className="editor-header">
        <h2>{page.pageId}</h2>
        <button onClick={() => onAddField(page.pageId)}>Add Field</button>
      </header>
      <div className="field-table">
        {page.pageFiles.map((field) => {
          const isSelected = selectedFieldKey === field.key;
          return (
            <div
              key={field.key}
              className={`field-row ${isSelected ? 'selected' : ''}`}
              onClick={() => onSelectField(field.key)}
            >
              <div className="field-primary">
                <label>
                  Key
                  <input
                    value={field.key}
                    onChange={(event) => onUpdateField(page.pageId, field.key, { key: event.target.value })}
                  />
                </label>
                <label>
                  Original Value
                  <textarea
                    value={field.originalValue}
                    onChange={(event) => onUpdateField(page.pageId, field.key, { originalValue: event.target.value })}
                  />
                </label>
              </div>
              <div className="variants">
                <div className="variants-header">
                  <h3>Variants</h3>
                  <button
                    onClick={(event) => {
                      event.stopPropagation();
                      const language = window.prompt('Language code (e.g. en, jp)?');
                      if (!language) return;
                      const value = window.prompt('Translation value?') ?? '';
                      onUpsertVariant(page.pageId, field.key, { language, _value: value });
                    }}
                  >
                    Add Variant
                  </button>
                </div>
                {field.variants.length === 0 && <p className="empty">No variants yet.</p>}
                {field.variants.map((variant) => (
                  <div key={variant.language} className="variant-row">
                    <div className="variant-language">{variant.language}</div>
                    <textarea
                      value={variant._value}
                      onChange={(event) =>
                        onUpsertVariant(page.pageId, field.key, {
                          language: variant.language,
                          _value: event.target.value,
                        })
                      }
                    />
                    <div className="variant-actions">
                      <button
                        onClick={(event) => {
                          event.stopPropagation();
                          onAutoTranslate(page.pageId, field.key, variant.language, field.originalValue);
                        }}
                      >
                        Auto-translate
                      </button>
                      <button
                        onClick={(event) => {
                          event.stopPropagation();
                          onRemoveVariant(page.pageId, field.key, variant.language);
                        }}
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          );
        })}
        {page.pageFiles.length === 0 && <p className="empty">Add your first field to start translating.</p>}
      </div>
    </section>
  );
}
