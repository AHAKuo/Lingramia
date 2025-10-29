import React from 'react';

const BottomBar = ({ currentFilePath, isDirty, statusMessage }) => {
  return (
    <footer className="bottom-bar">
      <div className="status-left">
        <span className="status-item">
          {currentFilePath ? (
            <>
              <strong>File:</strong> {currentFilePath}
              {isDirty && <span className="dirty-indicator"> (Unsaved)</span>}
            </>
          ) : (
            <span className="no-file">No file open</span>
          )}
        </span>
      </div>
      
      <div className="status-right">
        <span className="status-message">{statusMessage}</span>
      </div>
    </footer>
  );
};

export default BottomBar;
