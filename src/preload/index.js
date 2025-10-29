const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('lingramia', {
  openLocbook: () => ipcRenderer.invoke('dialog:open-locbook'),
  saveLocbook: (payload) => ipcRenderer.invoke('dialog:save-locbook', payload),
  readFile: (filePath) => ipcRenderer.invoke('file:read', filePath),
  writeFile: (payload) => ipcRenderer.invoke('file:write', payload),
});
