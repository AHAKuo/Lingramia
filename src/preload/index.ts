import { contextBridge, ipcRenderer } from 'electron';
import type { Locbook } from '../main/main';

type OpenResult = {
  canceled: boolean;
  data?: Locbook;
  path?: string | null;
};

type SaveResult = {
  canceled: boolean;
  path?: string | null;
};

contextBridge.exposeInMainWorld('lingramia', {
  getVersion: () => ipcRenderer.invoke('app:getVersion') as Promise<string>,
  newLocbook: () => ipcRenderer.invoke('locbook:new') as Promise<OpenResult>,
  openLocbook: () => ipcRenderer.invoke('locbook:open') as Promise<OpenResult>,
  saveLocbook: (payload: { data: Locbook; path: string | null }) =>
    ipcRenderer.invoke('locbook:save', payload) as Promise<SaveResult>,
});

declare global {
  interface Window {
    lingramia: {
      getVersion: () => Promise<string>;
      newLocbook: () => Promise<OpenResult>;
      openLocbook: () => Promise<OpenResult>;
      saveLocbook: (payload: { data: Locbook; path: string | null }) => Promise<SaveResult>;
    };
  }
}
