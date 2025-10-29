import React from 'react';

const Header = ({ onNew, onOpen, onSave, onSaveAs, isDirty, currentFilePath }) => {
  return (
    <header className="header">
      <div className="header-left">
        <h1 className="app-title">Lingramia <span className="version">v1.0.0</span></h1>
      </div>
      
      <div className="header-center">
        <button className="btn btn-primary" onClick={onNew} title="New (Ctrl+N)">
          New
        </button>
        <button className="btn btn-primary" onClick={onOpen} title="Open (Ctrl+O)">
          Open
        </button>
        <button className="btn btn-primary" onClick={onSave} title="Save (Ctrl+S)">
          Save {isDirty && '●'}
        </button>
        <button className="btn btn-secondary" onClick={onSaveAs}>
          Save As...
        </button>
      </div>

      <div className="header-right">
        <div className="current-file">
          {currentFilePath ? (
            <span title={currentFilePath}>
              {currentFilePath.split('/').pop()}
              {isDirty && ' ●'}
            </span>
          ) : (
            <span className="no-file">No file open</span>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;
