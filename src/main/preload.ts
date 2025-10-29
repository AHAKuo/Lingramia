import { contextBridge, ipcRenderer } from 'electron';

contextBridge.exposeInMainWorld('api', {
  openLocbook: () => ipcRenderer.invoke('open-locbook'),
  saveLocbook: (payload: { filePath?: string; data: unknown }) =>
    ipcRenderer.invoke('save-locbook', payload),
  revealInFolder: (filePath: string) => ipcRenderer.invoke('reveal-in-folder', filePath),
  getSetting: (key: string) => ipcRenderer.invoke('settings:get', key),
  setSetting: (key: string, value: unknown) => ipcRenderer.invoke('settings:set', { key, value }),
  onUnsavedWarning: (handler: () => void) => {
    ipcRenderer.on('unsaved-warning', handler);
    return () => ipcRenderer.removeListener('unsaved-warning', handler);
  },
});

export type PreloadAPI = typeof window.api;
