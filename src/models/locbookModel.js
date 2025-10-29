class LocbookModel {
  constructor(data = null) {
    if (data) {
      this.pages = data.pages || [];
    } else {
      this.pages = [];
    }
    this.filePath = null;
    this.isDirty = false;
  }

  static fromJSON(jsonData, filePath = null) {
    const model = new LocbookModel(jsonData);
    model.filePath = filePath;
    model.isDirty = false;
    return model;
  }

  toJSON() {
    return {
      pages: this.pages
    };
  }

  addPage(pageId = null, aboutPage = '') {
    const newPage = {
      aboutPage: aboutPage,
      pageId: pageId || this.generatePageId(),
      pageFiles: []
    };
    this.pages.push(newPage);
    this.isDirty = true;
    return newPage;
  }

  generatePageId() {
    return Math.floor(Math.random() * 100000).toString();
  }
}

module.exports = LocbookModel;
