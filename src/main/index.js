const { app, BrowserWindow, dialog, ipcMain } = require('electron');
const fs = require('fs/promises');

let mainWindow;

async function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1024,
    minHeight: 720,
    title: 'Lingramia',
    webPreferences: {
      contextIsolation: true,
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
    },
  });

  await mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  if (!app.isPackaged) {
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  }

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

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

ipcMain.handle('dialog:open-locbook', async () => {
  const { canceled, filePaths } = await dialog.showOpenDialog(mainWindow, {
    filters: [{ name: 'Locbook JSON', extensions: ['locbook', 'json'] }],
    properties: ['openFile'],
  });

  if (canceled || !filePaths?.length) {
    return null;
  }

  const filePath = filePaths[0];
  const fileContent = await fs.readFile(filePath, 'utf-8');

  return {
    path: filePath,
    content: fileContent,
  };
});

ipcMain.handle('dialog:save-locbook', async (_event, { defaultPath, content }) => {
  const { canceled, filePath } = await dialog.showSaveDialog(mainWindow, {
    title: 'Save Locbook',
    defaultPath,
    filters: [{ name: 'Locbook JSON', extensions: ['locbook', 'json'] }],
  });

  if (canceled || !filePath) {
    return null;
  }

  await fs.writeFile(filePath, content, 'utf-8');
  return filePath;
});

ipcMain.handle('file:read', async (_event, filePath) => {
  const fileContent = await fs.readFile(filePath, 'utf-8');
  return fileContent;
});

ipcMain.handle('file:write', async (_event, { filePath, content }) => {
  await fs.writeFile(filePath, content, 'utf-8');
  return true;
});
