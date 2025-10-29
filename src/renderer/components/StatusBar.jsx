import React from 'react';

export default function StatusBar({ activeTab, apiStatus = 'Offline', logMessage = 'Ready.' }) {
  return (
    <footer className="status-bar">
      <div>
        <strong>File:</strong> {activeTab?.path || activeTab?.name || '—'}
        {activeTab?.dirty && <span className="status-dirty"> (unsaved)</span>}
      </div>
      <div>
        <strong>API:</strong> {apiStatus}
      </div>
      <div className="status-log">{logMessage}</div>
    </footer>
  );
}
