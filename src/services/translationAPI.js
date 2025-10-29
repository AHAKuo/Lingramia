export async function requestTranslation({ text, targetLanguage, apiKey }) {
  console.info(`Mock translating to ${targetLanguage}:`, text, apiKey ? '(using provided key)' : '(no key)');
  // Placeholder translation logic. Replace with OpenAI or other provider.
  return `[${targetLanguage}] ${text}`;
}
