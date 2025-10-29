import { useMemo, useState } from 'react';
import TopBar from './layout/TopBar';
import PageSidebar from './layout/PageSidebar';
import EditorPanel from './components/EditorPanel';
import InspectorPanel from './layout/InspectorPanel';
import StatusBar from './layout/StatusBar';
import {
  createEmptyLocbook,
  createEmptyPage,
  updatePageInLocbook,
} from '../models/locbookModel';
import { openLocbookFromDialog, saveLocbookToDisk } from '../services/fileHandler';

const untitledLabel = (index) => `Untitled ${index}`;

const createTab = (index) => {
  const page = createEmptyPage();
  const firstEntryId = page.pageFiles[0]?.entryId ?? null;
  return {
    id: `tab-${Date.now()}-${index}`,
    title: untitledLabel(index),
    filePath: null,
    data: {
      ...createEmptyLocbook(),
      pages: [page],
    },
    dirty: false,
    selection: { type: 'page', pageId: page.pageId, entryId: firstEntryId },
  };
};

const getActiveTabIndex = (tabs, id) => tabs.findIndex((tab) => tab.id === id);

function App() {
  const [tabs, setTabs] = useState([createTab(1)]);
  const [activeTabId, setActiveTabId] = useState(tabs[0].id);
  const [untitledCounter, setUntitledCounter] = useState(2);
  const [statusMessage, setStatusMessage] = useState('Ready');

  const activeTab = useMemo(
    () => tabs.find((tab) => tab.id === activeTabId) ?? tabs[0],
    [tabs, activeTabId],
  );

  const activePage = useMemo(() => {
    if (!activeTab) return null;
    return activeTab.data.pages.find((page) => page.pageId === activeTab.selection?.pageId) ?? null;
  }, [activeTab]);

  const activeEntry = useMemo(() => {
    if (!activeTab || !activePage) return null;
    if (!activeTab.selection?.entryId) return null;
    return activePage.pageFiles.find((file) => file.entryId === activeTab.selection.entryId) ?? null;
  }, [activeTab, activePage]);

  const updateActiveTab = (mutator) => {
    setTabs((current) =>
      current.map((tab) => {
        if (tab.id !== activeTabId) {
          return tab;
        }
        return mutator(tab);
      }),
    );
  };

  const handleNewFile = () => {
    const newIndex = untitledCounter;
    const newTab = createTab(newIndex);
    setTabs((prev) => [...prev, newTab]);
    setActiveTabId(newTab.id);
    setUntitledCounter((prev) => prev + 1);
    setStatusMessage(`Created ${newTab.title}`);
  };

  const handleOpenFile = async () => {
    try {
      const result = await openLocbookFromDialog();
      if (!result) {
        setStatusMessage('File open canceled.');
        return;
      }

      const { filePath, locbook } = result;
      const tab = {
        id: `tab-${Date.now()}`,
        title: pathFromFile(filePath),
        filePath,
        data: locbook,
        dirty: false,
        selection: locbook.pages.length
          ? {
              type: 'page',
              pageId: locbook.pages[0].pageId,
              entryId: locbook.pages[0].pageFiles[0]?.entryId ?? null,
            }
          : { type: 'page', pageId: null, entryId: null },
      };

      setTabs((prev) => [...prev, tab]);
      setActiveTabId(tab.id);
      setStatusMessage(`Opened ${filePath}`);
    } catch (error) {
      console.error(error);
      setStatusMessage(error.message || 'Failed to open file.');
    }
  };

  const handleSaveFile = async () => {
    if (!activeTab) return;
    try {
      const savedPath = await saveLocbookToDisk(activeTab.filePath, activeTab.data);
      if (!savedPath) {
        setStatusMessage('Save canceled.');
        return;
      }

      updateActiveTab((tab) => ({
        ...tab,
        filePath: savedPath,
        title: pathFromFile(savedPath),
        dirty: false,
      }));
      setStatusMessage(`Saved to ${savedPath}`);
    } catch (error) {
      console.error(error);
      setStatusMessage(error.message || 'An error occurred during save.');
    }
  };

  const handleSelectTab = (id) => {
    setActiveTabId(id);
  };

  const handleCloseTab = (id) => {
    setTabs((prev) => {
      const filtered = prev.filter((tab) => tab.id !== id);
      if (filtered.length === 0) {
        const replacement = createTab(untitledCounter);
        setUntitledCounter((value) => value + 1);
        setActiveTabId(replacement.id);
        return [replacement];
      }

      if (activeTabId === id) {
        const currentIndex = getActiveTabIndex(prev, id);
        const fallback = currentIndex > 0 ? filtered[currentIndex - 1].id : filtered[0].id;
        setActiveTabId(fallback);
      }

      return filtered;
    });
  };

  const handleAddPage = () => {
    if (!activeTab) return;
    const newPage = createEmptyPage();
    const updated = {
      ...activeTab,
      data: {
        ...activeTab.data,
        pages: [...activeTab.data.pages, newPage],
      },
      dirty: true,
      selection: {
        type: 'page',
        pageId: newPage.pageId,
        entryId: newPage.pageFiles[0]?.entryId ?? null,
      },
    };
    setTabs((prev) => prev.map((tab) => (tab.id === activeTab.id ? updated : tab)));
    setStatusMessage('Page added');
  };

  const handleUpdatePage = (pageId, updater) => {
    updateActiveTab((tab) => {
      const nextData = updatePageInLocbook(tab.data, pageId, updater);
      return {
        ...tab,
        data: nextData,
        dirty: true,
      };
    });
  };

  const handleSelectPage = (pageId) => {
    updateActiveTab((tab) => ({
      ...tab,
      selection: {
        type: 'page',
        pageId,
        entryId:
          tab.data.pages.find((page) => page.pageId === pageId)?.pageFiles[0]?.entryId ?? null,
      },
    }));
  };

  const handleSelectEntry = (pageId, entryId) => {
    updateActiveTab((tab) => ({
      ...tab,
      selection: { type: 'entry', pageId, entryId },
    }));
  };

  const handleRenamePage = (pageId, title) => {
    handleUpdatePage(pageId, (page) => ({
      ...page,
      aboutPage: title,
    }));
  };

  const handleDeletePage = (pageId) => {
    if (!window.confirm('Delete this page?')) return;
    updateActiveTab((tab) => {
      const pages = tab.data.pages.filter((page) => page.pageId !== pageId);
      const nextSelection = pages.length
        ? {
            type: 'page',
            pageId: pages[0].pageId,
            entryId: pages[0].pageFiles[0]?.entryId ?? null,
          }
        : { type: 'page', pageId: null, entryId: null };
      return {
        ...tab,
        data: { ...tab.data, pages },
        selection: nextSelection,
        dirty: true,
      };
    });
    setStatusMessage('Page deleted');
  };

  return (
    <div className="app-shell">
      <TopBar
        tabs={tabs}
        activeTabId={activeTabId}
        onSelectTab={handleSelectTab}
        onCloseTab={handleCloseTab}
        onNewFile={handleNewFile}
        onOpenFile={handleOpenFile}
        onSaveFile={handleSaveFile}
      />
      <div className="workspace">
        <PageSidebar
          pages={activeTab?.data.pages ?? []}
          selection={activeTab?.selection}
          onAddPage={handleAddPage}
          onSelectPage={handleSelectPage}
          onRenamePage={handleRenamePage}
          onDeletePage={handleDeletePage}
        />
        <main className="main-panel">
          <EditorPanel
            page={activePage}
            onUpdatePage={handleUpdatePage}
            selection={activeTab?.selection}
            onSelectEntry={handleSelectEntry}
          />
        </main>
        <InspectorPanel page={activePage} entry={activeEntry} />
      </div>
      <StatusBar
        filePath={activeTab?.filePath}
        dirty={activeTab?.dirty}
        statusMessage={statusMessage}
      />
    </div>
  );
}

function pathFromFile(filePath) {
  if (!filePath) return 'Untitled';
  return filePath.split(/\\|\//).pop();
}

export default App;
