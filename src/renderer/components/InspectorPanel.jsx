import React from 'react';

function VariantList({ pageId, field, onUpsertVariant, onRemoveVariant }) {
  if (!field) return null;

  return (
    <div className="inspector-section">
      <h3>Variants</h3>
      {field.variants.map((variant) => (
        <div key={variant.language} className="inspector-variant">
          <div className="variant-header">
            <strong>{variant.language.toUpperCase()}</strong>
            <button onClick={() => onRemoveVariant(pageId, field.key, variant.language)}>Remove</button>
          </div>
          <textarea
            value={variant._value}
            onChange={(event) =>
              onUpsertVariant(pageId, field.key, {
                language: variant.language,
                _value: event.target.value,
              })
            }
          />
        </div>
      ))}
      {field.variants.length === 0 && <p className="empty">No variants selected.</p>}
    </div>
  );
}

export default function InspectorPanel({
  page,
  field,
  onUpdatePage,
  onUpsertVariant,
  onRemoveVariant,
}) {
  return (
    <aside className="inspector-panel">
      {!page && (
        <div className="inspector-empty">
          <p>Select a page to edit metadata.</p>
        </div>
      )}

      {page && (
        <div className="inspector-section">
          <h2>Page Settings</h2>
          <label>
            Page ID
            <input
              value={page.pageId}
              onChange={(event) => onUpdatePage(page.pageId, { pageId: event.target.value })}
            />
          </label>
          <label>
            About Page
            <textarea
              value={page.aboutPage ?? ''}
              onChange={(event) => onUpdatePage(page.pageId, { aboutPage: event.target.value })}
            />
          </label>
          <p className="meta">Fields: {page.pageFiles.length}</p>
          <button className="secondary" disabled>
            Export Page (Coming Soon)
          </button>
        </div>
      )}

      {field && (
        <div className="inspector-section">
          <h2>Field Details</h2>
          <p><strong>Key:</strong> {field.key}</p>
          <p><strong>Original Value:</strong></p>
          <p className="field-original">{field.originalValue || '—'}</p>
        </div>
      )}

      <VariantList
        pageId={page?.pageId}
        field={field}
        onUpsertVariant={onUpsertVariant}
        onRemoveVariant={onRemoveVariant}
      />
    </aside>
  );
}
