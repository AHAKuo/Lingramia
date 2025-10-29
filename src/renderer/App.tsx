import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  getSetting,
  openLocbook,
  revealInFolder,
  saveLocbook,
  setSetting,
} from './services/ipc';
import type { EditorTab, LocbookDocument, LocbookEntry, LocbookPage, LocbookVariant } from './types/locbook';

const createBlankDocument = (): LocbookDocument => ({
  pages: [
    {
      pageId: 'page-1',
      aboutPage: 'Getting started page',
      pageFiles: [
        {
          key: 'greeting_hello',
          originalValue: 'Hello World',
          variants: [
            { language: 'en', _value: 'Hello World' },
            { language: 'es', _value: 'Hola Mundo' },
          ],
        },
      ],
    },
  ],
});

const createTab = (document: LocbookDocument, filePath?: string): EditorTab => ({
  id: typeof crypto !== 'undefined' && 'randomUUID' in crypto ? crypto.randomUUID() : `tab-${Date.now()}`,
  filePath,
  document,
  hasUnsavedChanges: false,
  lastOpened: Date.now(),
});

const getFirstPageId = (document: LocbookDocument) => document.pages[0]?.pageId ?? '';

const findEntryByKey = (page: LocbookPage | undefined, key: string) =>
  page?.pageFiles.find((entry) => entry.key === key);

const App = () => {
  const [tabs, setTabs] = useState<EditorTab[]>([createTab(createBlankDocument())]);
  const [activeTabId, setActiveTabId] = useState<string>(tabs[0].id);
  const [selectedPageId, setSelectedPageId] = useState<string>(getFirstPageId(tabs[0].document));
  const [selectedEntryKey, setSelectedEntryKey] = useState<string>('');
  const [languageFilter, setLanguageFilter] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState<string>('');
  const [logs, setLogs] = useState<string[]>(['Lingramia ready.']);
  const [settingsOpen, setSettingsOpen] = useState<boolean>(false);
  const [apiKey, setApiKey] = useState<string>('');

  const activeTab = useMemo(() => tabs.find((tab) => tab.id === activeTabId) ?? tabs[0], [tabs, activeTabId]);
  const activeDocument = activeTab.document;
  const activePage = useMemo(
    () => activeDocument.pages.find((page) => page.pageId === selectedPageId) ?? activeDocument.pages[0],
    [activeDocument, selectedPageId],
  );

  const selectedEntry = useMemo(() => findEntryByKey(activePage, selectedEntryKey) ?? activePage?.pageFiles[0], [
    activePage,
    selectedEntryKey,
  ]);

  useEffect(() => {
    const initialise = async () => {
      const savedApiKey = await getSetting<string>('openaiApiKey');
      if (savedApiKey) {
        setApiKey(savedApiKey);
      }
    };
    initialise();
  }, []);

  useEffect(() => {
    if (activePage && !selectedPageId) {
      setSelectedPageId(activePage.pageId);
    }
  }, [activePage, selectedPageId]);

  const appendLog = useCallback((message: string) => {
    setLogs((prev) => [new Date().toLocaleTimeString() + ' • ' + message, ...prev].slice(0, 20));
  }, []);

  const updateActiveDocument = useCallback(
    (updater: (document: LocbookDocument) => LocbookDocument) => {
      setTabs((prevTabs) =>
        prevTabs.map((tab) => {
          if (tab.id !== activeTabId) return tab;
          const updatedDocument = updater(tab.document);
          return {
            ...tab,
            document: updatedDocument,
            hasUnsavedChanges: true,
            lastOpened: Date.now(),
          };
        }),
      );
    },
    [activeTabId],
  );

  const handleNewTab = () => {
    const document = createBlankDocument();
    const tab = createTab(document);
    setTabs((prev) => [...prev, tab]);
    setActiveTabId(tab.id);
    setSelectedPageId(getFirstPageId(document));
    setSelectedEntryKey('');
    appendLog('Created new document.');
  };

  const handleOpen = async () => {
    const result = await openLocbook();
    if (!result) {
      appendLog('Open cancelled.');
      return;
    }

    const tab = createTab(result.data, result.filePath);
    setTabs((prev) => [...prev, tab]);
    setActiveTabId(tab.id);
    setSelectedPageId(getFirstPageId(result.data));
    setSelectedEntryKey('');
    appendLog(`Opened ${result.filePath}`);
  };

  const handleSave = async () => {
    if (!activeTab) return;
    const result = await saveLocbook({ filePath: activeTab.filePath, data: activeTab.document });
    if (result?.filePath) {
      appendLog(`Saved document → ${result.filePath}`);
      setTabs((prevTabs) =>
        prevTabs.map((tab) =>
          tab.id === activeTab.id
            ? { ...tab, filePath: result.filePath, hasUnsavedChanges: false }
            : tab,
        ),
      );
    }
  };

  const handleSaveAs = async () => {
    if (!activeTab) return;
    const result = await saveLocbook({ data: activeTab.document });
    if (result?.filePath) {
      appendLog(`Saved document as → ${result.filePath}`);
      setTabs((prevTabs) =>
        prevTabs.map((tab) =>
          tab.id === activeTab.id
            ? { ...tab, filePath: result.filePath, hasUnsavedChanges: false }
            : tab,
        ),
      );
    }
  };

  const handleExportJson = async () => {
    if (!activeTab?.filePath) return;
    await revealInFolder(activeTab.filePath);
    appendLog('Revealed file in system explorer.');
  };

  const handleCloseTab = (tabId: string) => {
    setTabs((prevTabs) => {
      const remaining = prevTabs.filter((tab) => tab.id !== tabId);
      if (remaining.length === 0) {
        const blank = createTab(createBlankDocument());
        setActiveTabId(blank.id);
        setSelectedPageId(getFirstPageId(blank.document));
        setSelectedEntryKey('');
        return [blank];
      }

      if (tabId === activeTabId) {
        const nextActive = remaining[remaining.length - 1];
        setActiveTabId(nextActive.id);
        setSelectedPageId(getFirstPageId(nextActive.document));
        setSelectedEntryKey('');
      }

      return remaining;
    });
  };

  const handleAddPage = () => {
    const newPage: LocbookPage = {
      pageId: `page-${Date.now()}`,
      aboutPage: '',
      pageFiles: [],
    };
    updateActiveDocument((document) => ({
      ...document,
      pages: [...document.pages, newPage],
    }));
    setSelectedPageId(newPage.pageId);
    setSelectedEntryKey('');
    appendLog('Added page to document.');
  };

  const handleDeletePage = (pageId: string) => {
    const remainingPages = activeDocument.pages.filter((page) => page.pageId !== pageId);
    updateActiveDocument((document) => ({
      ...document,
      pages: remainingPages,
    }));
    const fallbackPageId = remainingPages[0]?.pageId ?? '';
    setSelectedPageId((prev) => (prev === pageId ? fallbackPageId : prev));
    setSelectedEntryKey('');
    appendLog(`Deleted page ${pageId}`);
  };

  const handlePageChange = (pageId: string, field: keyof LocbookPage, value: string) => {
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === pageId
          ? {
              ...page,
              [field]: value,
            }
          : page,
      ),
    }));
  };

  const handleAddEntry = () => {
    if (!activePage) return;
    const newEntry: LocbookEntry = {
      key: `entry_${Date.now()}`,
      originalValue: '',
      variants: [],
    };

    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? { ...page, pageFiles: [...page.pageFiles, newEntry] }
          : page,
      ),
    }));
    setSelectedEntryKey(newEntry.key);
    appendLog('Created new entry.');
  };

  const handleDeleteEntry = (entryKey: string) => {
    if (!activePage) return;
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? { ...page, pageFiles: page.pageFiles.filter((entry) => entry.key !== entryKey) }
          : page,
      ),
    }));
    appendLog(`Removed entry ${entryKey}.`);
    setSelectedEntryKey((prev) => (prev === entryKey ? '' : prev));
  };

  const handleEntryChange = (entryKey: string, field: keyof LocbookEntry, value: string) => {
    if (!activePage) return;
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? {
              ...page,
              pageFiles: page.pageFiles.map((entry) =>
                entry.key === entryKey
                  ? {
                      ...entry,
                      [field]: value,
                    }
                  : entry,
              ),
            }
          : page,
      ),
    }));
  };

  const handleVariantChange = (entryKey: string, index: number, variant: Partial<LocbookVariant>) => {
    if (!activePage) return;
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? {
              ...page,
              pageFiles: page.pageFiles.map((entry) => {
                if (entry.key !== entryKey) return entry;
                const updatedVariants = [...entry.variants];
                updatedVariants[index] = { ...updatedVariants[index], ...variant };
                return { ...entry, variants: updatedVariants };
              }),
            }
          : page,
      ),
    }));
  };

  const handleAddVariant = (entryKey: string) => {
    if (!activePage) return;
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? {
              ...page,
              pageFiles: page.pageFiles.map((entry) =>
                entry.key === entryKey
                  ? {
                      ...entry,
                      variants: [...entry.variants, { language: '', _value: '' }],
                    }
                  : entry,
              ),
            }
          : page,
      ),
    }));
  };

  const handleRemoveVariant = (entryKey: string, index: number) => {
    if (!activePage) return;
    updateActiveDocument((document) => ({
      ...document,
      pages: document.pages.map((page) =>
        page.pageId === activePage.pageId
          ? {
              ...page,
              pageFiles: page.pageFiles.map((entry) =>
                entry.key === entryKey
                  ? {
                      ...entry,
                      variants: entry.variants.filter((_, variantIndex) => variantIndex !== index),
                    }
                  : entry,
              ),
            }
          : page,
      ),
    }));
  };

  const handleSaveSettings = async () => {
    await setSetting('openaiApiKey', apiKey);
    appendLog('API key saved to secure store.');
    setSettingsOpen(false);
  };

  const filteredEntries = useMemo(() => {
    if (!activePage) return [] as LocbookEntry[];
    return activePage.pageFiles.filter((entry) => {
      const matchesSearch = entry.key.toLowerCase().includes(searchTerm.toLowerCase());
      if (!languageFilter) return matchesSearch;
      return matchesSearch && entry.variants.some((variant) => variant.language === languageFilter);
    });
  }, [activePage, searchTerm, languageFilter]);

  return (
    <div className="app-shell">
      <header className="header">
        <div className="left">
          <div>
            <strong>Lingramia</strong>
            <span className="tag">v0.1.0</span>
          </div>
          <div className="tab-strip">
            {tabs.map((tab) => (
              <div
                key={tab.id}
                className={`tab ${tab.id === activeTabId ? 'active' : ''}`}
                onClick={() => {
                  setActiveTabId(tab.id);
                  setSelectedPageId(getFirstPageId(tab.document));
                  setSelectedEntryKey('');
                }}
              >
                <span>{tab.filePath ? tab.filePath.split(/[\\/]/).pop() : 'Untitled'}</span>
                {tab.hasUnsavedChanges && <span>●</span>}
                <button onClick={(event) => {
                  event.stopPropagation();
                  handleCloseTab(tab.id);
                }}>×</button>
              </div>
            ))}
          </div>
        </div>
        <div className="right">
          <button onClick={handleNewTab}>New</button>
          <button onClick={handleOpen}>Open</button>
          <button onClick={handleSave}>Save</button>
          <button onClick={handleSaveAs}>Save As…</button>
          <button onClick={handleExportJson}>Reveal File</button>
          <button onClick={() => setSettingsOpen(true)}>Settings</button>
        </div>
      </header>

      <aside className="sidebar">
        <div className="toolbar">
          <h2>Pages</h2>
          <button onClick={handleAddPage}>Add</button>
        </div>
        <div className="page-list">
          {activeDocument.pages.map((page) => (
            <div
              key={page.pageId}
              className={`page-item ${page.pageId === activePage?.pageId ? 'active' : ''}`}
              onClick={() => {
                setSelectedPageId(page.pageId);
                setSelectedEntryKey('');
              }}
            >
              <span>{page.pageId}</span>
              <button
                onClick={(event) => {
                  event.stopPropagation();
                  handleDeletePage(page.pageId);
                }}
              >
                Delete
              </button>
            </div>
          ))}
        </div>
      </aside>

      <main className="main-panel">
        <div className="toolbar">
          <button onClick={handleAddEntry}>Add Entry</button>
          <div className="filters">
            <input
              placeholder="Search key…"
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
            />
            <select value={languageFilter} onChange={(event) => setLanguageFilter(event.target.value)}>
              <option value="">All languages</option>
              {Array.from(
                new Set(activePage?.pageFiles.flatMap((entry) => entry.variants.map((variant) => variant.language)) ?? []),
              ).map((language) => (
                <option key={language} value={language}>
                  {language || '—'}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="editor-table">
          <table>
            <thead>
              <tr>
                <th>Key</th>
                <th>Original Value</th>
                <th>Language</th>
                <th>Translation</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredEntries.map((entry) =>
                entry.variants.length > 0 ? (
                  entry.variants.map((variant, index) => (
                    <tr key={`${entry.key}-${index}`} onClick={() => setSelectedEntryKey(entry.key)}>
                      {index === 0 && (
                        <>
                          <td rowSpan={entry.variants.length}>{entry.key}</td>
                          <td rowSpan={entry.variants.length}>{entry.originalValue}</td>
                        </>
                      )}
                      <td>{variant.language}</td>
                      <td>{variant._value}</td>
                      <td>
                        <button onClick={() => handleRemoveVariant(entry.key, index)}>Remove Variant</button>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr key={entry.key} onClick={() => setSelectedEntryKey(entry.key)}>
                    <td>{entry.key}</td>
                    <td>{entry.originalValue}</td>
                    <td colSpan={2}>No variants yet</td>
                    <td>
                      <button onClick={() => handleDeleteEntry(entry.key)}>Delete Entry</button>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        </div>
      </main>

      <aside className="inspector">
        {activePage && (
          <div className="scrollable">
            <h3>Page Inspector</h3>
            <div className="input-group">
              <label>Page ID</label>
              <input
                value={activePage.pageId}
                onChange={(event) => handlePageChange(activePage.pageId, 'pageId', event.target.value)}
              />
            </div>
            <div className="input-group">
              <label>About Page</label>
              <textarea
                rows={3}
                value={activePage.aboutPage ?? ''}
                onChange={(event) => handlePageChange(activePage.pageId, 'aboutPage', event.target.value)}
              />
            </div>
            <div className="input-group">
              <label>Fields</label>
              <div className="tag">{activePage.pageFiles.length} entries</div>
            </div>
            <button onClick={() => appendLog(`Exported page ${activePage.pageId} (stub).`)}>Export Page</button>
          </div>
        )}

        {selectedEntry && (
          <div className="scrollable">
            <h3>Field Inspector</h3>
            <div className="input-group">
              <label>Key</label>
              <input
                value={selectedEntry.key}
                onChange={(event) => handleEntryChange(selectedEntry.key, 'key', event.target.value)}
              />
            </div>
            <div className="input-group">
              <label>Original Value</label>
              <textarea
                rows={3}
                value={selectedEntry.originalValue}
                onChange={(event) => handleEntryChange(selectedEntry.key, 'originalValue', event.target.value)}
              />
            </div>

            <div className="input-group">
              <label>Variants</label>
              {selectedEntry.variants.map((variant, index) => (
                <div key={index} className="input-group">
                  <input
                    placeholder="Language code"
                    value={variant.language}
                    onChange={(event) => handleVariantChange(selectedEntry.key, index, { language: event.target.value })}
                  />
                  <textarea
                    rows={2}
                    placeholder="Translation"
                    value={variant._value}
                    onChange={(event) => handleVariantChange(selectedEntry.key, index, { _value: event.target.value })}
                  />
                </div>
              ))}
              <button onClick={() => handleAddVariant(selectedEntry.key)}>Add Variant</button>
            </div>
            <button onClick={() => handleDeleteEntry(selectedEntry.key)}>Delete Field</button>
            <button onClick={() => appendLog('Auto-translate triggered (stub).')}>Auto-translate (stub)</button>
          </div>
        )}
      </aside>

      <footer className="status-bar">
        <div className="status-section">
          <span className="status-title">File Status</span>
          <span>{activeTab.filePath ?? 'Untitled document'}</span>
          <span>{activeTab.hasUnsavedChanges ? 'Unsaved changes' : 'All changes saved'}</span>
        </div>
        <div className="status-section">
          <span className="status-title">API Connection</span>
          <span>{apiKey ? 'OpenAI key configured' : 'API key not set'}</span>
        </div>
        <div className="status-section">
          <span className="status-title">Activity Log</span>
          <div className="log">{logs.join('\n')}</div>
        </div>
      </footer>

      {settingsOpen && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(9, 12, 20, 0.75)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 10,
          }}
        >
          <div
            style={{
              width: 420,
              background: '#111722',
              borderRadius: 16,
              border: '1px solid rgba(62, 119, 255, 0.3)',
              padding: 24,
              display: 'flex',
              flexDirection: 'column',
              gap: 16,
            }}
          >
            <h2>Settings</h2>
            <div className="input-group">
              <label>OpenAI API Key</label>
              <input
                type="password"
                value={apiKey}
                onChange={(event) => setApiKey(event.target.value)}
                placeholder="sk-..."
              />
            </div>
            <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
              <button onClick={() => setSettingsOpen(false)}>Cancel</button>
              <button onClick={handleSaveSettings}>Save</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default App;
