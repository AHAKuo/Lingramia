import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { Locbook, Page, PageFile, Variant } from './types';

type EditorTab = {
  id: string;
  path: string | null;
  label: string;
  data: Locbook;
  isDirty: boolean;
};

type TabViewState = {
  selectedPageId: string | null;
  selectedFieldKey: string | null;
  languageFilter: string;
  search: string;
};

const defaultViewState: TabViewState = {
  selectedPageId: null,
  selectedFieldKey: null,
  languageFilter: 'all',
  search: '',
};

const createUntitledLabel = (index: number) => `Untitled ${index}`;

const cloneLocbook = (data: Locbook): Locbook => JSON.parse(JSON.stringify(data));

const App: React.FC = () => {
  const [tabs, setTabs] = useState<EditorTab[]>([]);
  const [tabViews, setTabViews] = useState<Record<string, TabViewState>>({});
  const [activeTabId, setActiveTabId] = useState<string | null>(null);
  const [untitledCount, setUntitledCount] = useState(1);
  const [appVersion, setAppVersion] = useState<string>('0.0.0');
  const [statusMessage, setStatusMessage] = useState<string>('Ready');

  const activeTab = useMemo(() => tabs.find((tab) => tab.id === activeTabId) ?? null, [tabs, activeTabId]);
  const activeView = useMemo(() => {
    if (!activeTabId) {
      return defaultViewState;
    }
    return tabViews[activeTabId] ?? defaultViewState;
  }, [activeTabId, tabViews]);

  const selectedPage = useMemo(() => {
    if (!activeTab || !activeView.selectedPageId) return undefined;
    return activeTab.data.pages.find((page) => page.pageId === activeView.selectedPageId);
  }, [activeTab, activeView.selectedPageId]);

  const selectedField = useMemo(() => {
    if (!selectedPage || !activeView.selectedFieldKey) return undefined;
    return selectedPage.pageFiles.find((file) => file.key === activeView.selectedFieldKey);
  }, [selectedPage, activeView.selectedFieldKey]);

  const setTabViewState = useCallback((tabId: string, updater: (state: TabViewState) => TabViewState) => {
    setTabViews((prev) => ({
      ...prev,
      [tabId]: updater(prev[tabId] ?? defaultViewState),
    }));
  }, []);

  const ensureSelectionForTab = useCallback((tabId: string, data: Locbook) => {
    setTabViews((prev) => {
      const current = prev[tabId] ?? defaultViewState;
      if (current.selectedPageId && data.pages.some((page) => page.pageId === current.selectedPageId)) {
        return prev;
      }
      const firstPageId = data.pages[0]?.pageId ?? null;
      return {
        ...prev,
        [tabId]: {
          ...current,
          selectedPageId: firstPageId,
          selectedFieldKey: data.pages[0]?.pageFiles[0]?.key ?? null,
        },
      };
    });
  }, []);



  useEffect(() => {
    const bootstrap = async () => {
      const [version, newFile] = await Promise.all([
        window.lingramia.getVersion(),
        window.lingramia.newLocbook(),
      ]);

      setAppVersion(version);

      if (!newFile.canceled && newFile.data) {
        const data = newFile.data as Locbook;
        setTabs(() => {
          const tabId = crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2);
          const label = createUntitledLabel(1);
          setActiveTabId(tabId);
          ensureSelectionForTab(tabId, data);
          setUntitledCount(2);
          return [
            {
              id: tabId,
              label,
              path: null,
              data,
              isDirty: false,
            },
          ];
        });
      }
    };

    bootstrap().catch((error) => {
      console.error('Failed to bootstrap Lingramia', error);
      setStatusMessage('Failed to initialise application');
    });
  }, [ensureSelectionForTab]);

  useEffect(() => {
    if (!activeTab) {
      const nextActive = tabs[0]?.id ?? null;
      setActiveTabId(nextActive);
      if (nextActive && tabs[0]) {
        ensureSelectionForTab(nextActive, tabs[0].data);
      }
    } else {
      ensureSelectionForTab(activeTab.id, activeTab.data);
    }
  }, [activeTab, tabs, ensureSelectionForTab]);

  const updateTabData = useCallback(
    (tabId: string, mutator: (data: Locbook) => Locbook) => {
      setTabs((prev) =>
        prev.map((tab) => {
          if (tab.id !== tabId) return tab;
          const nextData = mutator(cloneLocbook(tab.data));
          return {
            ...tab,
            data: nextData,
            isDirty: true,
          };
        }),
      );
    },
    [],
  );

  const handleSetActiveTab = useCallback((tabId: string) => {
    setActiveTabId(tabId);
    const tab = tabs.find((t) => t.id === tabId);
    if (tab) {
      ensureSelectionForTab(tabId, tab.data);
    }
  }, [tabs, ensureSelectionForTab]);

  const handleNewFile = useCallback(async () => {
    const result = await window.lingramia.newLocbook();
    if (result.canceled || !result.data) {
      return;
    }
    const data = result.data as Locbook;
    const tabId = crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2);
    setTabs((prev) => {
      const next = [
        ...prev,
        {
          id: tabId,
          label: createUntitledLabel(untitledCount),
          path: null,
          data,
          isDirty: false,
        },
      ];
      return next;
    });
    setUntitledCount((count) => count + 1);
    setActiveTabId(tabId);
    ensureSelectionForTab(tabId, data);
    setStatusMessage('Created new locbook');
  }, [ensureSelectionForTab, untitledCount]);

  const handleOpenFile = useCallback(async () => {
    const result = await window.lingramia.openLocbook();
    if (result.canceled || !result.data) {
      return;
    }
    const data = result.data as Locbook;
    const path = result.path ?? null;
    const tabId = crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2);
    setTabs((prev) => {
      const existingIndex = path ? prev.findIndex((tab) => tab.path === path) : -1;
      const label = path ? path.split(/[/\\]/).pop() ?? 'Untitled' : createUntitledLabel(untitledCount);
      const newTab: EditorTab = {
        id: tabId,
        path,
        label,
        data,
        isDirty: false,
      };
      if (existingIndex >= 0) {
        const existingTab = prev[existingIndex];
        const nextTabs = [...prev];
        nextTabs[existingIndex] = { ...existingTab, data, isDirty: false };
        setActiveTabId(existingTab.id);
        ensureSelectionForTab(existingTab.id, data);
        return nextTabs;
      }
      setActiveTabId(tabId);
      ensureSelectionForTab(tabId, data);
      return [...prev, newTab];
    });
    setStatusMessage(`Opened ${path ?? 'locbook from template'}`);
  }, [ensureSelectionForTab, untitledCount]);

  const handleSaveFile = useCallback(async () => {
    if (!activeTab) return;
    const response = await window.lingramia.saveLocbook({ data: activeTab.data, path: activeTab.path });
    if (response.canceled) {
      setStatusMessage('Save cancelled');
      return;
    }

    const nextPath = response.path ?? activeTab.path;
    setTabs((prev) =>
      prev.map((tab) => {
        if (tab.id !== activeTab.id) return tab;
        return {
          ...tab,
          path: nextPath,
          label: nextPath ? nextPath.split(/[/\\]/).pop() ?? tab.label : tab.label,
          isDirty: false,
        };
      }),
    );

    setStatusMessage(`Saved ${nextPath ?? activeTab.label}`);
  }, [activeTab]);

  const handleCloseTab = useCallback(
    (tabId: string) => {
      const tab = tabs.find((t) => t.id === tabId);
      if (!tab) return;
      if (tab.isDirty) {
        const confirmClose = window.confirm('This tab has unsaved changes. Close anyway?');
        if (!confirmClose) return;
      }

      setTabs((prev) => prev.filter((t) => t.id !== tabId));
      setTabViews((prev) => {
        const next = { ...prev };
        delete next[tabId];
        return next;
      });

      if (activeTabId === tabId) {
        setActiveTabId((prevActive) => {
          if (prevActive !== tabId) return prevActive;
          const remaining = tabs.filter((t) => t.id !== tabId);
          return remaining[0]?.id ?? null;
        });
      }
    },
    [tabs, activeTabId],
  );

  const handleAddPage = useCallback(() => {
    if (!activeTab) return;
    const newPage: Page = {
      pageId: `page-${Date.now()}`,
      aboutPage: '',
      pageFiles: [],
    };
    updateTabData(activeTab.id, (data) => ({
      ...data,
      pages: [...data.pages, newPage],
    }));
    setTabViewState(activeTab.id, (state) => ({
      ...state,
      selectedPageId: newPage.pageId,
      selectedFieldKey: null,
    }));
  }, [activeTab, updateTabData, setTabViewState]);

  const handleDeletePage = useCallback(
    (pageId: string) => {
      if (!activeTab) return;
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.filter((page) => page.pageId !== pageId),
      }));
      setTabViewState(activeTab.id, (state) => ({
        ...state,
        selectedPageId: state.selectedPageId === pageId ? null : state.selectedPageId,
        selectedFieldKey: state.selectedPageId === pageId ? null : state.selectedFieldKey,
      }));
    },
    [activeTab, updateTabData, setTabViewState],
  );

  const handleAddField = useCallback(() => {
    if (!activeTab || !selectedPage) return;
    const newField: PageFile = {
      key: `key_${Date.now()}`,
      originalValue: '',
      variants: [],
    };
    updateTabData(activeTab.id, (data) => ({
      ...data,
      pages: data.pages.map((page) =>
        page.pageId === selectedPage.pageId
          ? {
              ...page,
              pageFiles: [...page.pageFiles, newField],
            }
          : page,
      ),
    }));
    setTabViewState(activeTab.id, (state) => ({
      ...state,
      selectedFieldKey: newField.key,
    }));
  }, [activeTab, selectedPage, updateTabData, setTabViewState]);

  const handleSelectPage = useCallback(
    (pageId: string) => {
      if (!activeTab) return;
      setTabViewState(activeTab.id, (state) => ({
        ...state,
        selectedPageId: pageId,
        selectedFieldKey: null,
      }));
    },
    [activeTab, setTabViewState],
  );

  const handleSelectField = useCallback(
    (fieldKey: string) => {
      if (!activeTab) return;
      setTabViewState(activeTab.id, (state) => ({
        ...state,
        selectedFieldKey: fieldKey,
      }));
    },
    [activeTab, setTabViewState],
  );

  const handleUpdatePageDetails = useCallback(
    (pageId: string, updates: Partial<Page>) => {
      if (!activeTab) return;
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.map((page) =>
          page.pageId === pageId
            ? {
                ...page,
                ...updates,
              }
            : page,
        ),
      }));
      if (typeof updates.pageId === 'string') {
        setTabViewState(activeTab.id, (state) => ({
          ...state,
          selectedPageId: state.selectedPageId === pageId ? updates.pageId! : state.selectedPageId,
        }));
      }
    },
    [activeTab, updateTabData, setTabViewState],
  );

  const handleUpdateField = useCallback(
    (fieldKey: string, updates: Partial<PageFile>) => {
      if (!activeTab || !selectedPage) return;
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.map((page) =>
          page.pageId === selectedPage.pageId
            ? {
                ...page,
                pageFiles: page.pageFiles.map((file) =>
                  file.key === fieldKey
                    ? {
                        ...file,
                        ...updates,
                      }
                    : file,
                ),
              }
            : page,
        ),
      }));
      if (typeof updates.key === 'string') {
        setTabViewState(activeTab.id, (state) => ({
          ...state,
          selectedFieldKey: state.selectedFieldKey === fieldKey ? updates.key! : state.selectedFieldKey,
        }));
      }
    },
    [activeTab, selectedPage, updateTabData, setTabViewState],
  );

  const handleUpdateVariant = useCallback(
    (fieldKey: string, variantIndex: number, updates: Partial<Variant>) => {
      if (!activeTab || !selectedPage) return;
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.map((page) =>
          page.pageId === selectedPage.pageId
            ? {
                ...page,
                pageFiles: page.pageFiles.map((file) => {
                  if (file.key !== fieldKey) return file;
                  const nextVariants = file.variants.map((variant, index) =>
                    index === variantIndex
                      ? {
                          ...variant,
                          ...updates,
                        }
                      : variant,
                  );
                  return {
                    ...file,
                    variants: nextVariants,
                  };
                }),
              }
            : page,
        ),
      }));
    },
    [activeTab, selectedPage, updateTabData],
  );

  const handleAddVariant = useCallback(
    (fieldKey: string) => {
      if (!activeTab || !selectedPage) return;
      const newVariant: Variant = { language: '', _value: '' };
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.map((page) =>
          page.pageId === selectedPage.pageId
            ? {
                ...page,
                pageFiles: page.pageFiles.map((file) =>
                  file.key === fieldKey
                    ? {
                        ...file,
                        variants: [...file.variants, newVariant],
                      }
                    : file,
                ),
              }
            : page,
        ),
      }));
    },
    [activeTab, selectedPage, updateTabData],
  );

  const handleDeleteVariant = useCallback(
    (fieldKey: string, index: number) => {
      if (!activeTab || !selectedPage) return;
      updateTabData(activeTab.id, (data) => ({
        ...data,
        pages: data.pages.map((page) =>
          page.pageId === selectedPage.pageId
            ? {
                ...page,
                pageFiles: page.pageFiles.map((file) =>
                  file.key === fieldKey
                    ? {
                        ...file,
                        variants: file.variants.filter((_, variantIndex) => variantIndex !== index),
                      }
                    : file,
                ),
              }
            : page,
        ),
      }));
    },
    [activeTab, selectedPage, updateTabData],
  );

  const filteredFields = useMemo(() => {
    if (!selectedPage) return [] as PageFile[];
    return selectedPage.pageFiles.filter((file) => {
      const matchesSearch = activeView.search
        ? file.key.toLowerCase().includes(activeView.search.toLowerCase()) ||
          file.originalValue.toLowerCase().includes(activeView.search.toLowerCase())
        : true;
      const matchesLanguage =
        activeView.languageFilter === 'all' ||
        file.variants.some((variant) => variant.language === activeView.languageFilter);
      return matchesSearch && matchesLanguage;
    });
  }, [selectedPage, activeView.search, activeView.languageFilter]);

  const languages = useMemo(() => {
    if (!activeTab) return [] as string[];
    const set = new Set<string>();
    activeTab.data.pages.forEach((page) => {
      page.pageFiles.forEach((file) => {
        file.variants.forEach((variant) => set.add(variant.language));
      });
    });
    return Array.from(set).filter(Boolean).sort();
  }, [activeTab]);

  return (
    <div className="app-shell">
      <header className="top-bar">
        <div className="brand">
          <span className="brand-name">Lingramia</span>
          <span className="brand-version">v{appVersion}</span>
        </div>
        <div className="toolbar">
          <button onClick={handleNewFile}>New</button>
          <button onClick={handleOpenFile}>Open</button>
          <button onClick={handleSaveFile} disabled={!activeTab}>
            Save
          </button>
        </div>
        <div className="tabs">
          {tabs.map((tab) => (
            <div
              key={tab.id}
              className={`tab ${tab.id === activeTabId ? 'active' : ''}`}
              onClick={() => handleSetActiveTab(tab.id)}
            >
              <span className="tab-label">{tab.label}</span>
              {tab.isDirty && <span className="tab-indicator">●</span>}
              <button
                className="tab-close"
                onClick={(event) => {
                  event.stopPropagation();
                  handleCloseTab(tab.id);
                }}
              >
                ×
              </button>
            </div>
          ))}
        </div>
        <button className="settings-btn" type="button" title="Settings coming soon">
          ⚙️
        </button>
      </header>

      <section className="workspace">
        <aside className="sidebar pages">
          <div className="sidebar-header">
            <h2>Pages</h2>
            <button onClick={handleAddPage}>Add Page</button>
          </div>
          <ul className="page-list">
            {activeTab?.data.pages.map((page) => (
              <li
                key={page.pageId}
                className={page.pageId === activeView.selectedPageId ? 'selected' : ''}
              >
                <button onClick={() => handleSelectPage(page.pageId)} className="page-entry">
                  <span>{page.pageId}</span>
                  <span className="page-count">{page.pageFiles.length} fields</span>
                </button>
                <button
                  className="page-delete"
                  onClick={(event) => {
                    event.stopPropagation();
                    handleDeletePage(page.pageId);
                  }}
                >
                  🗑️
                </button>
              </li>
            ))}
            {!activeTab && <li className="empty">No file loaded</li>}
          </ul>
        </aside>

        <main className="editor">
          {selectedPage ? (
            <div className="editor-content">
              <div className="editor-toolbar">
                <button onClick={handleAddField}>Add Field</button>
                <div className="filters">
                  <input
                    type="search"
                    placeholder="Search key or text"
                    value={activeView.search}
                    onChange={(event) =>
                      activeTab &&
                      setTabViewState(activeTab.id, (state) => ({ ...state, search: event.target.value }))
                    }
                  />
                  <select
                    value={activeView.languageFilter}
                    onChange={(event) =>
                      activeTab &&
                      setTabViewState(activeTab.id, (state) => ({
                        ...state,
                        languageFilter: event.target.value,
                      }))
                    }
                  >
                    <option value="all">All languages</option>
                    {languages.map((language) => (
                      <option key={language} value={language}>
                        {language}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="field-table">
                <div className="field-table-header">
                  <span>Key</span>
                  <span>Original</span>
                  <span>Language</span>
                  <span>Translation</span>
                  <span>Actions</span>
                </div>
                <div className="field-table-body">
                  {filteredFields.map((file) => (
                    <React.Fragment key={file.key}>
                      {file.variants.length === 0 ? (
                        <div className={`field-row ${file.key === activeView.selectedFieldKey ? 'selected' : ''}`}>
                          <span>{file.key}</span>
                          <span>{file.originalValue}</span>
                          <span>-</span>
                          <span>-</span>
                          <span className="row-actions">
                            <button
                              onClick={(event) => {
                                event.stopPropagation();
                                handleSelectField(file.key);
                              }}
                            >
                              Select
                            </button>
                            <button
                              onClick={(event) => {
                                event.stopPropagation();
                                handleAddVariant(file.key);
                              }}
                            >
                              Add Variant
                            </button>
                          </span>
                        </div>
                      ) : (
                        file.variants.map((variant, index) => (
                          <div
                            key={`${file.key}-${index}`}
                            className={`field-row ${file.key === activeView.selectedFieldKey ? 'selected' : ''}`}
                            onClick={() => handleSelectField(file.key)}
                          >
                            <span>{file.key}</span>
                            <span>{file.originalValue}</span>
                            <span>{variant.language || '—'}</span>
                            <span>{variant._value || '—'}</span>
                            <span className="row-actions">
                              <button
                                onClick={(event) => {
                                  event.stopPropagation();
                                  handleAddVariant(file.key);
                                }}
                              >
                                ＋
                              </button>
                              <button
                                onClick={(event) => {
                                  event.stopPropagation();
                                  handleDeleteVariant(file.key, index);
                                }}
                              >
                                ✖
                              </button>
                            </span>
                          </div>
                        ))
                      )}
                    </React.Fragment>
                  ))}
                </div>
              </div>
            </div>
          ) : (
            <div className="empty-state">
              <p>Select or create a page to start editing translations.</p>
            </div>
          )}
        </main>

        <aside className="sidebar inspector">
          {selectedPage ? (
            <div className="inspector-content">
              <h2>Inspector</h2>
              <div className="inspector-section">
                <h3>Page Details</h3>
                <label>
                  Page ID
                  <input
                    value={selectedPage.pageId}
                    onChange={(event) => handleUpdatePageDetails(selectedPage.pageId, { pageId: event.target.value })}
                  />
                </label>
                <label>
                  About
                  <textarea
                    value={selectedPage.aboutPage}
                    onChange={(event) => handleUpdatePageDetails(selectedPage.pageId, { aboutPage: event.target.value })}
                  />
                </label>
                <p className="inspector-meta">{selectedPage.pageFiles.length} fields</p>
              </div>
              {selectedField ? (
                <div className="inspector-section">
                  <h3>Field</h3>
                  <label>
                    Key
                    <input
                      value={selectedField.key}
                      onChange={(event) => handleUpdateField(selectedField.key, { key: event.target.value })}
                    />
                  </label>
                  <label>
                    Original Value
                    <textarea
                      value={selectedField.originalValue}
                      onChange={(event) => handleUpdateField(selectedField.key, { originalValue: event.target.value })}
                    />
                  </label>
                  <div className="variant-list">
                    <h4>Variants</h4>
                    {selectedField.variants.map((variant, index) => (
                      <div key={`${variant.language}-${index}`} className="variant-row">
                        <input
                          placeholder="Language"
                          value={variant.language}
                          onChange={(event) =>
                            handleUpdateVariant(selectedField.key, index, { language: event.target.value })
                          }
                        />
                        <textarea
                          placeholder="Translation"
                          value={variant._value}
                          onChange={(event) =>
                            handleUpdateVariant(selectedField.key, index, { _value: event.target.value })
                          }
                        />
                      </div>
                    ))}
                    <button onClick={() => handleAddVariant(selectedField.key)}>Add Variant</button>
                  </div>
                </div>
              ) : (
                <div className="inspector-section">
                  <p>Select a field to inspect translations</p>
                </div>
              )}
            </div>
          ) : (
            <div className="inspector-placeholder">
              <p>No page selected</p>
            </div>
          )}
        </aside>
      </section>

      <footer className="status-bar">
        <span>{statusMessage}</span>
        {activeTab ? (
          <span>
            {activeTab.path ?? 'Unsaved file'} {activeTab.isDirty ? '(unsaved changes)' : '(saved)'}
          </span>
        ) : (
          <span>No file loaded</span>
        )}
      </footer>
    </div>
  );
};

export default App;
