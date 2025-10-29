const STORAGE_KEY = 'lingramia.config';

function readStorage() {
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : {};
  } catch (error) {
    console.error('Failed to read config storage', error);
    return {};
  }
}

function writeStorage(config) {
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
  } catch (error) {
    console.error('Failed to write config storage', error);
  }
}

export async function getConfigValue(key) {
  const config = readStorage();
  return key.split('.').reduce((acc, part) => (acc ? acc[part] : undefined), config);
}

export async function setConfigValue(key, value) {
  const config = readStorage();
  const parts = key.split('.');
  let current = config;
  parts.forEach((part, index) => {
    if (index === parts.length - 1) {
      current[part] = value;
    } else {
      current[part] = current[part] || {};
      current = current[part];
    }
  });
  writeStorage(config);
  return value;
}
