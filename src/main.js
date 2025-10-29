const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('node:path');
const fs = require('fs').promises;

if (require('electron-squirrel-startup')) {
  app.quit();
}

let mainWindow = null;

const createWindow = () => {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 1000,
    minHeight: 600,
    webPreferences: {
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  if (process.env.NODE_ENV === 'development') {
    mainWindow.webContents.openDevTools();
  }
};

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

ipcMain.handle('file:open', async (event, filePath) => {
  try {
    let filePathToOpen = filePath;
    
    if (!filePathToOpen) {
      const result = await dialog.showOpenDialog(mainWindow, {
        properties: ['openFile'],
        filters: [
          { name: 'Locbook Files', extensions: ['locbook'] },
          { name: 'JSON Files', extensions: ['json'] },
          { name: 'All Files', extensions: ['*'] }
        ]
      });
      
      if (result.canceled || result.filePaths.length === 0) {
        return { success: false, error: 'No file selected' };
      }
      
      filePathToOpen = result.filePaths[0];
    }
    
    const data = await fs.readFile(filePathToOpen, 'utf-8');
    const jsonData = JSON.parse(data);
    
    return {
      success: true,
      data: jsonData,
      filePath: filePathToOpen
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
});

ipcMain.handle('file:save', async (event, { filePath, data }) => {
  try {
    const jsonString = JSON.stringify(data, null, 2);
    await fs.writeFile(filePath, jsonString, 'utf-8');
    
    return {
      success: true,
      filePath: filePath
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
});

ipcMain.handle('file:saveAs', async (event, data) => {
  try {
    const result = await dialog.showSaveDialog(mainWindow, {
      filters: [
        { name: 'Locbook Files', extensions: ['locbook'] },
        { name: 'JSON Files', extensions: ['json'] }
      ],
      defaultPath: 'untitled.locbook'
    });
    
    if (result.canceled || !result.filePath) {
      return { success: false, error: 'Save canceled' };
    }
    
    const jsonString = JSON.stringify(data, null, 2);
    await fs.writeFile(result.filePath, jsonString, 'utf-8');
    
    return {
      success: true,
      filePath: result.filePath
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
});

if (process.argv.length > 1) {
  const filePath = process.argv.find(arg => arg.endsWith('.locbook'));
  if (filePath) {
    app.whenReady().then(() => {
      createWindow();
      setTimeout(() => {
        mainWindow.webContents.send('file:open-from-args', filePath);
      }, 1000);
    });
  }
}
