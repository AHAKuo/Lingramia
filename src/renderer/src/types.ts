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
