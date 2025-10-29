import { normalizeLocbook, serializeLocbook } from '../models/locbookModel';

export async function openLocbookFromDialog() {
  if (!window.lingramiaAPI?.openLocbook) {
    throw new Error('File dialog API not available');
  }
  const payload = await window.lingramiaAPI.openLocbook();
  if (!payload) {
    return null;
  }
  const { filePath, data } = payload;
  return {
    filePath,
    locbook: normalizeLocbook(JSON.parse(data)),
  };
}

export async function saveLocbookToDisk(filePath, locbook) {
  if (!window.lingramiaAPI?.saveLocbook) {
    throw new Error('Save dialog API not available');
  }
  const nextPath = await window.lingramiaAPI.saveLocbook({
    filePath,
    content: serializeLocbook(locbook),
  });
  return nextPath;
}
