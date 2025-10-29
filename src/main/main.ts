import { app, BrowserWindow, dialog, ipcMain } from 'electron';
import path from 'path';
import fs from 'fs/promises';

export type Variant = {
  _value: string;
  language: string;
};

export type PageFile = {
  key: string;
  originalValue: string;
  variants: Variant[];
};

export type Page = {
  aboutPage: string;
  pageId: string;
  pageFiles: PageFile[];
};

export type Locbook = {
  pages: Page[];
};

const defaultLocbook: Locbook = {
  pages: [
    {
      aboutPage: 'Landing page copy and greetings',
      pageId: '-4302',
      pageFiles: [
        {
          key: 'greeting_hello',
          originalValue: 'Hello World',
          variants: [
            { _value: 'Hello World', language: 'en' },
            { _value: 'こんにちは', language: 'jp' },
            { _value: 'أهلاً وسهلاً', language: 'ar' },
          ],
        },
      ],
    },
  ],
};

let mainWindow: BrowserWindow | null = null;

const createWindow = async () => {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    show: false,
    title: 'Lingramia',
    webPreferences: {
      preload: path.join(__dirname, '../preload/preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  mainWindow.on('ready-to-show', () => {
    mainWindow?.show();
  });

  const pageUrl =
    process.env.ELECTRON_RENDERER_URL ??
    new URL('../renderer/index.html', `file://${__dirname}/`).toString();

  await mainWindow.loadURL(pageUrl);
};

app.whenReady().then(() => {
  createWindow().catch((error) => {
    console.error('Failed to create window', error);
  });

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow().catch((error) => console.error(error));
    }
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

ipcMain.handle('app:getVersion', () => app.getVersion());

ipcMain.handle('locbook:new', () => ({
  data: defaultLocbook,
  path: null,
}));

ipcMain.handle('locbook:open', async () => {
  const { canceled, filePaths } = await dialog.showOpenDialog(mainWindow ?? undefined, {
    filters: [{ name: 'Locbook', extensions: ['locbook', 'json'] }],
    properties: ['openFile'],
  });

  if (canceled || filePaths.length === 0) {
    return { canceled: true };
  }

  try {
    const filePath = filePaths[0];
    const file = await fs.readFile(filePath, 'utf-8');
    const data: Locbook = JSON.parse(file);
    return { canceled: false, data, path: filePath };
  } catch (error) {
    dialog.showErrorBox('Failed to open file', (error as Error).message);
    return { canceled: true };
  }
});

ipcMain.handle(
  'locbook:save',
  async (
    _event,
    payload: {
      data: Locbook;
      path: string | null;
    },
  ) => {
    const { data, path: filePath } = payload;

    try {
      let finalPath = filePath;
      if (!finalPath) {
        const { canceled, filePath: savePath } = await dialog.showSaveDialog(mainWindow ?? undefined, {
          filters: [{ name: 'Locbook', extensions: ['locbook'] }],
          defaultPath: 'untitled.locbook',
        });

        if (canceled || !savePath) {
          return { canceled: true };
        }

        finalPath = savePath;
      }

      if (!finalPath) {
        return { canceled: true };
      }

      await fs.writeFile(finalPath, JSON.stringify(data, null, 2), 'utf-8');
      return { canceled: false, path: finalPath };
    } catch (error) {
      dialog.showErrorBox('Failed to save file', (error as Error).message);
      return { canceled: true };
    }
  },
);
