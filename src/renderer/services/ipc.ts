import type { LocbookDocument } from '../types/locbook';

type OpenResult = { filePath: string; data: LocbookDocument } | null;

type SaveResult = { filePath: string } | null;

export const openLocbook = async (): Promise<OpenResult> => {
  const response = await window.api.openLocbook();
  return response;
};

export const saveLocbook = async (payload: { filePath?: string; data: LocbookDocument }): Promise<SaveResult> => {
  const response = await window.api.saveLocbook(payload);
  return response;
};

export const revealInFolder = (filePath: string) => {
  return window.api.revealInFolder(filePath);
};

export const getSetting = <T = unknown>(key: string): Promise<T> => {
  return window.api.getSetting(key) as Promise<T>;
};

export const setSetting = (key: string, value: unknown) => {
  return window.api.setSetting(key, value);
};
