const { ipcRenderer } = require('electron');

class FileHandler {
  static async openFile(filePath = null) {
    return await ipcRenderer.invoke('file:open', filePath);
  }

  static async saveFile(filePath, data) {
    return await ipcRenderer.invoke('file:save', { filePath, data });
  }

  static async saveFileAs(data) {
    return await ipcRenderer.invoke('file:saveAs', data);
  }

  static createNew() {
    return {
      pages: []
    };
  }
}

module.exports = FileHandler;
