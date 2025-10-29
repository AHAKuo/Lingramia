import { app, BrowserWindow, dialog, ipcMain, shell } from 'electron';
import fs from 'fs/promises';
import Store from 'electron-store';

const isDevelopment = process.env.NODE_ENV === 'development';

const store = new Store({ name: 'lingramia-settings' });

let mainWindow: BrowserWindow | null = null;

declare const MAIN_WINDOW_WEBPACK_ENTRY: string;
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string;

const createWindow = async () => {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1100,
    minHeight: 700,
    show: false,
    webPreferences: {
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
    },
  });

  mainWindow.once('ready-to-show', () => {
    mainWindow?.show();
  });

  await mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  if (isDevelopment) {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
};

app.whenReady().then(async () => {
  await createWindow();

  app.on('activate', async () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      await createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

ipcMain.handle('open-locbook', async () => {
  const result = await dialog.showOpenDialog(mainWindow ?? undefined, {
    filters: [{ name: 'Localization Books', extensions: ['locbook', 'json'] }],
    properties: ['openFile'],
  });

  if (result.canceled || result.filePaths.length === 0) {
    return null;
  }

  const filePath = result.filePaths[0];
  try {
    const raw = await fs.readFile(filePath, 'utf-8');
    const data = JSON.parse(raw);
    return { filePath, data };
  } catch (error) {
    dialog.showErrorBox('Failed to open file', (error as Error).message);
    return null;
  }
});

ipcMain.handle('save-locbook', async (_event, { filePath, data }: { filePath?: string; data: unknown }) => {
  let targetPath = filePath;

  if (!targetPath) {
    const result = await dialog.showSaveDialog(mainWindow ?? undefined, {
      filters: [{ name: 'Localization Books', extensions: ['locbook'] }],
      defaultPath: 'untitled.locbook',
    });

    if (result.canceled || !result.filePath) {
      return null;
    }

    targetPath = result.filePath;
  }

  try {
    await fs.writeFile(targetPath, JSON.stringify(data, null, 2), 'utf-8');
    return { filePath: targetPath };
  } catch (error) {
    dialog.showErrorBox('Failed to save file', (error as Error).message);
    return null;
  }
});

ipcMain.handle('reveal-in-folder', async (_event, filePath: string) => {
  if (filePath) {
    await shell.showItemInFolder(filePath);
  }
});

ipcMain.handle('settings:get', (_event, key: string) => {
  return store.get(key);
});

ipcMain.handle('settings:set', (_event, { key, value }: { key: string; value: unknown }) => {
  store.set(key, value);
});
