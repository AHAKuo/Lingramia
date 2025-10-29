export async function openLocbookDialog() {
  if (!window?.lingramia?.openLocbook) {
    throw new Error('Open dialog is not available.');
  }

  return window.lingramia.openLocbook();
}

export async function saveLocbookDialog({ defaultPath, content }) {
  if (!window?.lingramia?.saveLocbook) {
    throw new Error('Save dialog is not available.');
  }

  return window.lingramia.saveLocbook({ defaultPath, content });
}

export async function readLocbook(path) {
  if (!window?.lingramia?.readFile) {
    throw new Error('File read bridge missing.');
  }

  return window.lingramia.readFile(path);
}

export async function writeLocbook({ filePath, content }) {
  if (!window?.lingramia?.writeFile) {
    throw new Error('File write bridge missing.');
  }

  return window.lingramia.writeFile({ filePath, content });
}
