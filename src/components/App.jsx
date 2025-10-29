import React, { useState, useEffect } from 'react';
import Header from './Header';
import LeftSidebar from './LeftSidebar';
import MainEditor from './MainEditor';
import RightSidebar from './RightSidebar';
import BottomBar from './BottomBar';

const App = () => {
  const [locbookData, setLocbookData] = useState({ pages: [] });
  const [currentFilePath, setCurrentFilePath] = useState(null);
  const [isDirty, setIsDirty] = useState(false);
  const [selectedPageIndex, setSelectedPageIndex] = useState(null);
  const [selectedField, setSelectedField] = useState(null);
  const [statusMessage, setStatusMessage] = useState('Ready');

  const handleNew = () => {
    if (isDirty) {
      if (!confirm('You have unsaved changes. Create new file anyway?')) {
        return;
      }
    }
    setLocbookData({ pages: [] });
    setCurrentFilePath(null);
    setIsDirty(false);
    setSelectedPageIndex(null);
    setSelectedField(null);
    setStatusMessage('New file created');
  };

  const handleOpen = async () => {
    try {
      const result = await window.electronAPI.openFile();
      if (result.success) {
        setLocbookData(result.data);
        setCurrentFilePath(result.filePath);
        setIsDirty(false);
        setSelectedPageIndex(null);
        setSelectedField(null);
        setStatusMessage(`Opened: ${result.filePath}`);
      } else if (result.error !== 'No file selected') {
        setStatusMessage(`Error: ${result.error}`);
      }
    } catch (error) {
      setStatusMessage(`Error opening file: ${error.message}`);
    }
  };

  const handleSave = async () => {
    try {
      let filePath = currentFilePath;
      
      if (!filePath) {
        const result = await window.electronAPI.saveFileAs(locbookData);
        if (result.success) {
          setCurrentFilePath(result.filePath);
          setIsDirty(false);
          setStatusMessage(`Saved: ${result.filePath}`);
        }
        return;
      }

      const result = await window.electronAPI.saveFile(filePath, locbookData);
      if (result.success) {
        setIsDirty(false);
        setStatusMessage(`Saved: ${filePath}`);
      } else {
        setStatusMessage(`Error: ${result.error}`);
      }
    } catch (error) {
      setStatusMessage(`Error saving file: ${error.message}`);
    }
  };

  const handleSaveAs = async () => {
    try {
      const result = await window.electronAPI.saveFileAs(locbookData);
      if (result.success) {
        setCurrentFilePath(result.filePath);
        setIsDirty(false);
        setStatusMessage(`Saved: ${result.filePath}`);
      }
    } catch (error) {
      setStatusMessage(`Error saving file: ${error.message}`);
    }
  };

  const handleAddPage = () => {
    const newPage = {
      aboutPage: '',
      pageId: Math.floor(Math.random() * 100000).toString(),
      pageFiles: []
    };
    const newData = { ...locbookData, pages: [...locbookData.pages, newPage] };
    setLocbookData(newData);
    setIsDirty(true);
    setSelectedPageIndex(newData.pages.length - 1);
    setStatusMessage('Page added');
  };

  const handleDeletePage = (pageIndex) => {
    if (!confirm('Delete this page?')) return;
    
    const newPages = [...locbookData.pages];
    newPages.splice(pageIndex, 1);
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
    
    if (selectedPageIndex === pageIndex) {
      setSelectedPageIndex(null);
      setSelectedField(null);
    } else if (selectedPageIndex > pageIndex) {
      setSelectedPageIndex(selectedPageIndex - 1);
    }
    setStatusMessage('Page deleted');
  };

  const handleUpdatePage = (pageIndex, updates) => {
    const newPages = [...locbookData.pages];
    newPages[pageIndex] = { ...newPages[pageIndex], ...updates };
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
  };

  const handleAddField = () => {
    if (selectedPageIndex === null) {
      setStatusMessage('Select a page first');
      return;
    }

    const newField = {
      key: 'new_key',
      originalValue: '',
      variants: []
    };

    const newPages = [...locbookData.pages];
    newPages[selectedPageIndex].pageFiles.push(newField);
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
    setStatusMessage('Field added');
  };

  const handleUpdateField = (pageIndex, fieldIndex, updates) => {
    const newPages = [...locbookData.pages];
    newPages[pageIndex].pageFiles[fieldIndex] = {
      ...newPages[pageIndex].pageFiles[fieldIndex],
      ...updates
    };
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
  };

  const handleDeleteField = (pageIndex, fieldIndex) => {
    if (!confirm('Delete this field?')) return;

    const newPages = [...locbookData.pages];
    newPages[pageIndex].pageFiles.splice(fieldIndex, 1);
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
    
    if (selectedField && selectedField.pageIndex === pageIndex && selectedField.fieldIndex === fieldIndex) {
      setSelectedField(null);
    }
    setStatusMessage('Field deleted');
  };

  const handleAddVariant = (pageIndex, fieldIndex) => {
    const newVariant = {
      _value: '',
      language: 'en'
    };

    const newPages = [...locbookData.pages];
    newPages[pageIndex].pageFiles[fieldIndex].variants.push(newVariant);
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
    setStatusMessage('Variant added');
  };

  const handleUpdateVariant = (pageIndex, fieldIndex, variantIndex, updates) => {
    const newPages = [...locbookData.pages];
    newPages[pageIndex].pageFiles[fieldIndex].variants[variantIndex] = {
      ...newPages[pageIndex].pageFiles[fieldIndex].variants[variantIndex],
      ...updates
    };
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
  };

  const handleDeleteVariant = (pageIndex, fieldIndex, variantIndex) => {
    if (!confirm('Delete this variant?')) return;

    const newPages = [...locbookData.pages];
    newPages[pageIndex].pageFiles[fieldIndex].variants.splice(variantIndex, 1);
    setLocbookData({ ...locbookData, pages: newPages });
    setIsDirty(true);
    setStatusMessage('Variant deleted');
  };

  useEffect(() => {
    const handleKeyDown = (e) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        handleSave();
      }
      if ((e.ctrlKey || e.metaKey) && e.key === 'n') {
        e.preventDefault();
        handleNew();
      }
      if ((e.ctrlKey || e.metaKey) && e.key === 'o') {
        e.preventDefault();
        handleOpen();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [locbookData, currentFilePath, isDirty]);

  return (
    <div className="app">
      <Header
        onNew={handleNew}
        onOpen={handleOpen}
        onSave={handleSave}
        onSaveAs={handleSaveAs}
        isDirty={isDirty}
        currentFilePath={currentFilePath}
      />
      
      <div className="main-content">
        <LeftSidebar
          pages={locbookData.pages}
          selectedPageIndex={selectedPageIndex}
          onSelectPage={setSelectedPageIndex}
          onAddPage={handleAddPage}
          onDeletePage={handleDeletePage}
        />

        <MainEditor
          pages={locbookData.pages}
          selectedPageIndex={selectedPageIndex}
          selectedField={selectedField}
          onSelectField={setSelectedField}
          onAddField={handleAddField}
          onUpdateField={handleUpdateField}
          onDeleteField={handleDeleteField}
          onAddVariant={handleAddVariant}
          onUpdateVariant={handleUpdateVariant}
          onDeleteVariant={handleDeleteVariant}
        />

        <RightSidebar
          pages={locbookData.pages}
          selectedPageIndex={selectedPageIndex}
          selectedField={selectedField}
          onUpdatePage={handleUpdatePage}
          onUpdateField={handleUpdateField}
          onAddVariant={handleAddVariant}
          onUpdateVariant={handleUpdateVariant}
          onDeleteVariant={handleDeleteVariant}
        />
      </div>

      <BottomBar
        currentFilePath={currentFilePath}
        isDirty={isDirty}
        statusMessage={statusMessage}
      />
    </div>
  );
};

export default App;
