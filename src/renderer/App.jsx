import React, { useState } from 'react';
import Header from './components/Header';
import PageSidebar from './components/PageSidebar';
import EditorPanel from './components/EditorPanel';
import InspectorPanel from './components/InspectorPanel';
import StatusBar from './components/StatusBar';
import useLocbook from './hooks/useLocbook';

export default function App() {
  const [showSettings, setShowSettings] = useState(false);
  const [logMessage, setLogMessage] = useState('Ready.');
  const [apiStatus, setApiStatus] = useState('Offline');

  const {
    tabs,
    activeTab,
    activeTabId,
    selectedPageId,
    selectedFieldKey,
    setSelectedPageId,
    setSelectedFieldKey,
    selectTab,
    newLocbook,
    openLocbook,
    closeTab,
    saveLocbook,
    addPage,
    updatePageMeta,
    addFieldToPage,
    updateField,
    upsertVariant,
    removeVariant,
    autoTranslateVariant,
  } = useLocbook();

  const selectedPage = activeTab?.data?.pages?.find((page) => page.pageId === selectedPageId) ?? null;
  const selectedField = selectedPage?.pageFiles?.find((field) => field.key === selectedFieldKey) ?? null;

  const handleSave = async () => {
    setLogMessage('Saving...');
    const filePath = await saveLocbook();
    if (filePath) {
      setLogMessage(`Saved to ${filePath}`);
    } else {
      setLogMessage('Save cancelled.');
    }
  };

  const handleAutoTranslate = async (pageId, fieldKey, language, sourceText) => {
    setLogMessage(`Translating ${language}...`);
    try {
      await autoTranslateVariant(pageId, fieldKey, language, sourceText);
      setApiStatus('Online');
      setLogMessage(`Translated ${language}.`);
    } catch (error) {
      console.error(error);
      setApiStatus('Error');
      setLogMessage('Translation failed. Check API settings.');
    }
  };

  return (
    <div className="app-shell">
      <Header
        tabs={tabs}
        activeTabId={activeTabId}
        onSelectTab={selectTab}
        onCloseTab={closeTab}
        onNew={newLocbook}
        onOpen={openLocbook}
        onSave={handleSave}
        onToggleSettings={() => setShowSettings(true)}
      />

      <div className="app-body">
        <PageSidebar
          pages={activeTab?.data?.pages || []}
          selectedPageId={selectedPageId}
          onSelectPage={setSelectedPageId}
          onAddPage={addPage}
        />

        <EditorPanel
          page={selectedPage}
          selectedFieldKey={selectedFieldKey}
          onSelectField={setSelectedFieldKey}
          onAddField={addFieldToPage}
          onUpdateField={updateField}
          onUpsertVariant={upsertVariant}
          onRemoveVariant={removeVariant}
          onAutoTranslate={handleAutoTranslate}
        />

        <InspectorPanel
          page={selectedPage}
          field={selectedField}
          onUpdatePage={updatePageMeta}
          onUpsertVariant={upsertVariant}
          onRemoveVariant={removeVariant}
        />
      </div>

      <StatusBar activeTab={activeTab} apiStatus={apiStatus} logMessage={logMessage} />

      {showSettings && (
        <div className="modal-backdrop" onClick={() => setShowSettings(false)}>
          <div className="modal" onClick={(event) => event.stopPropagation()}>
            <h2>Settings</h2>
            <p>Store your API keys and customize the workspace. (Coming soon)</p>
            <button onClick={() => setShowSettings(false)}>Close</button>
          </div>
        </div>
      )}
    </div>
  );
}
