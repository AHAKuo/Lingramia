export function parseLocbook(content) {
  try {
    const parsed = JSON.parse(content);
    if (!parsed.pages) {
      return { pages: [] };
    }
    return parsed;
  } catch (error) {
    console.error('Failed to parse locbook file', error);
    return { pages: [] };
  }
}

export function serializeLocbook(data) {
  try {
    return JSON.stringify(data, null, 2);
  } catch (error) {
    console.error('Failed to serialize locbook data', error);
    return '{}';
  }
}
