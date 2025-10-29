const NOT_IMPLEMENTED_MESSAGE = 'Translation service requires API configuration.';

export async function autoTranslateText(text, targetLanguage) {
  console.warn(NOT_IMPLEMENTED_MESSAGE, { text, targetLanguage });
  return text;
}

export function getAPIStatus() {
  return {
    connected: false,
    message: NOT_IMPLEMENTED_MESSAGE,
  };
}
