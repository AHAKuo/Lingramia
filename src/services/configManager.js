class ConfigManager {
  constructor() {
    this.config = this.loadConfig();
  }

  loadConfig() {
    try {
      const stored = localStorage.getItem('lingramia_config');
      if (stored) {
        return JSON.parse(stored);
      }
    } catch (error) {
      console.error('Failed to load config:', error);
    }
    
    return {
      theme: 'light',
      openaiApiKey: '',
      recentFiles: []
    };
  }

  saveConfig() {
    try {
      localStorage.setItem('lingramia_config', JSON.stringify(this.config));
    } catch (error) {
      console.error('Failed to save config:', error);
    }
  }

  get(key) {
    return this.config[key];
  }

  set(key, value) {
    this.config[key] = value;
    this.saveConfig();
  }
}

module.exports = ConfigManager;
