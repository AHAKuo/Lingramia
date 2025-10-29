const CONFIG_KEY = 'lingramia.settings';

export function loadSettings() {
  try {
    const stored = localStorage.getItem(CONFIG_KEY);
    return stored ? JSON.parse(stored) : {};
  } catch (error) {
    console.error('Failed to load settings', error);
    return {};
  }
}

export function saveSettings(settings) {
  try {
    localStorage.setItem(CONFIG_KEY, JSON.stringify(settings));
  } catch (error) {
    console.error('Failed to save settings', error);
  }
}
