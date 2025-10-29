/* global MAIN_WINDOW_WEBPACK_ENTRY, MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY */
const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const fs = require('fs/promises');

const isDev = process.env.NODE_ENV === 'development';

async function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: 960,
    minHeight: 640,
    title: 'Lingramia',
    webPreferences: {
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
    },
  });

  await mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  if (isDev) {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }
}

app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

ipcMain.handle('dialog:openLocbook', async () => {
  const { canceled, filePaths } = await dialog.showOpenDialog({
    filters: [{ name: 'Locbook Files', extensions: ['locbook', 'json'] }],
    properties: ['openFile'],
  });

  if (canceled || filePaths.length === 0) {
    return null;
  }

  const filePath = filePaths[0];
  const data = await fs.readFile(filePath, 'utf-8');
  return { filePath, data };
});

ipcMain.handle('dialog:saveLocbook', async (event, { filePath, content }) => {
  if (!filePath) {
    const { canceled, filePath: selectedPath } = await dialog.showSaveDialog({
      filters: [{ name: 'Locbook Files', extensions: ['locbook', 'json'] }],
    });

    if (canceled || !selectedPath) {
      return null;
    }

    filePath = selectedPath;
  }

  await fs.writeFile(filePath, content, 'utf-8');
  return filePath;
});
