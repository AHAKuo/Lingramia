export interface LocbookVariant {
  _value: string;
  language: string;
}

export interface LocbookEntry {
  key: string;
  originalValue: string;
  variants: LocbookVariant[];
}

export interface LocbookPage {
  aboutPage?: string;
  pageId: string;
  pageFiles: LocbookEntry[];
}

export interface LocbookDocument {
  pages: LocbookPage[];
}

export interface EditorTab {
  id: string;
  filePath?: string;
  document: LocbookDocument;
  hasUnsavedChanges: boolean;
  lastOpened: number;
}
