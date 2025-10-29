const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('lingramiaAPI', {
  openLocbook: () => ipcRenderer.invoke('dialog:openLocbook'),
  saveLocbook: (payload) => ipcRenderer.invoke('dialog:saveLocbook', payload),
});
