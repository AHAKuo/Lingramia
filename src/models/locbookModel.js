export function createEmptyLocbook() {
  return {
    pages: [],
  };
}

export function createEmptyPage() {
  return {
    aboutPage: '',
    pageId: `${Date.now()}-${Math.random().toString(16).slice(2, 6)}`,
    pageFiles: [createEmptyField()],
  };
}

export function createEmptyField() {
  return {
    entryId: `entry-${Date.now()}-${Math.random().toString(16).slice(2, 6)}`,
    key: '',
    originalValue: '',
    variants: [createVariant('en')],
  };
}

export function createVariant(language = 'en') {
  return {
    _value: '',
    language,
  };
}

export function normalizeLocbook(raw) {
  const locbook = {
    pages: Array.isArray(raw?.pages) ? raw.pages : [],
  };

  return {
    ...locbook,
    pages: locbook.pages.map((page) => ({
      aboutPage: page.aboutPage ?? '',
      pageId: page.pageId?.toString() ?? `${Date.now()}-${Math.random()}`,
      pageFiles: Array.isArray(page.pageFiles)
        ? page.pageFiles.map((file) => ({
            entryId: file.entryId ?? `entry-${Date.now()}-${Math.random().toString(16).slice(2, 6)}`,
            key: file.key ?? '',
            originalValue: file.originalValue ?? '',
            variants: Array.isArray(file.variants)
              ? file.variants.map((variant) => ({
                  _value: variant?._value ?? '',
                  language: variant?.language ?? 'en',
                }))
              : [createVariant('en')],
          }))
        : [],
    })),
  };
}

export function updatePageInLocbook(locbook, pageId, updater) {
  return {
    ...locbook,
    pages: locbook.pages.map((page) => (page.pageId === pageId ? updater(page) : page)),
  };
}

export function serializeLocbook(locbook) {
  return JSON.stringify(locbook, null, 2);
}
