const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  openFile: (filePath) => ipcRenderer.invoke('file:open', filePath),
  saveFile: (filePath, data) => ipcRenderer.invoke('file:save', { filePath, data }),
  saveFileAs: (data) => ipcRenderer.invoke('file:saveAs', data),
  onFileOpenFromArgs: (callback) => ipcRenderer.on('file:open-from-args', callback)
});
