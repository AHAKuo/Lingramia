/**
 * Lightweight reader class for .locbook files.
 * Provides a simple API for reading localization data from JSON-based .locbook files.
 * 
 * Namespace equivalent: AHAKuo.Lingramia.API
 */

const fs = require('fs');
const path = require('path');

class L {
    /**
     * Creates a new L instance.
     */
    constructor() {
        this._resourcePath = null;
        this._currentLanguage = 'en';
        this._cache = {};
        this._keyIndex = {};
    }

    /**
     * Sets the resource path containing .locbook files.
     * Loads all .locbook files from the specified directory into cache.
     * 
     * @param {string} pathToResources - Path to the folder containing .locbook files
     * @throws {Error} If path is null or empty
     * @throws {Error} If directory does not exist
     */
    setResourcePath(pathToResources) {
        if (!pathToResources || !pathToResources.trim()) {
            throw new Error('Resource path cannot be null or empty.');
        }

        if (!fs.existsSync(pathToResources) || !fs.statSync(pathToResources).isDirectory()) {
            throw new Error(`Directory not found: ${pathToResources}`);
        }

        this._resourcePath = pathToResources;
        this._cache = {};
        this._keyIndex = {};

        // Load all .locbook files from the directory
        const files = fs.readdirSync(pathToResources);
        for (const filename of files) {
            if (filename.endsWith('.locbook')) {
                const filePath = path.join(pathToResources, filename);
                try {
                    this._loadLocbookFile(filePath);
                } catch (ex) {
                    // Log warning but don't crash - continue loading other files
                    console.warn(`Warning: Failed to load ${filePath}: ${ex.message}`);
                }
            }
        }
    }

    /**
     * Gets the current active language code.
     * 
     * @returns {string} The current language code (e.g., "en", "jp", "ar")
     */
    getLanguage() {
        return this._currentLanguage;
    }

    /**
     * Sets the active language for translations.
     * 
     * @param {string} code - Language code (e.g., "en", "jp", "ar")
     * @throws {Error} If code is null or empty
     */
    setLanguage(code) {
        if (!code || !code.trim()) {
            throw new Error('Language code cannot be null or empty.');
        }

        this._currentLanguage = code;
    }

    /**
     * Looks up a translation value by key.
     * 
     * @param {string} key - The key to look up
     * @param {boolean} hybridKey - If true, tries key, then originalValue, then aliases as fallback
     * @returns {string|null} The translated value for the current language, or null if not found
     */
    key(key, hybridKey = false) {
        if (!key || !key.trim()) {
            return null;
        }

        // Standard mode: lookup by key only
        if (!hybridKey) {
            return this._lookupByKey(key);
        }

        // Hybrid mode: try key, then originalValue, then aliases
        let result = this._lookupByKey(key);
        if (result !== null) {
            return result;
        }

        // Try originalValue
        const byOriginalValue = this._lookupByOriginalValue(key);
        if (byOriginalValue !== null) {
            return byOriginalValue;
        }

        // Try aliases
        const byAlias = this._lookupByAlias(key);
        if (byAlias !== null) {
            return byAlias;
        }

        // Fallback: return null
        return null;
    }

    /**
     * Internal method to lookup by key.
     * @private
     */
    _lookupByKey(key) {
        for (const fileName in this._keyIndex) {
            const pageFiles = this._keyIndex[fileName];
            if (pageFiles.hasOwnProperty(key)) {
                const pageFile = pageFiles[key];
                return this._getTranslationForLanguage(pageFile, this._currentLanguage);
            }
        }
        return null;
    }

    /**
     * Internal method to lookup by originalValue.
     * @private
     */
    _lookupByOriginalValue(originalValue) {
        for (const fileName in this._keyIndex) {
            const pageFiles = this._keyIndex[fileName];
            for (const key in pageFiles) {
                const pageFile = pageFiles[key];
                if (pageFile.originalValue && 
                    pageFile.originalValue.toLowerCase() === originalValue.toLowerCase()) {
                    return this._getTranslationForLanguage(pageFile, this._currentLanguage);
                }
            }
        }
        return null;
    }

    /**
     * Internal method to lookup by alias.
     * @private
     */
    _lookupByAlias(alias) {
        for (const fileName in this._keyIndex) {
            const pageFiles = this._keyIndex[fileName];
            for (const key in pageFiles) {
                const pageFile = pageFiles[key];
                const aliases = pageFile.aliases || [];
                if (aliases.some(a => a && a.toLowerCase() === alias.toLowerCase())) {
                    return this._getTranslationForLanguage(pageFile, this._currentLanguage);
                }
            }
        }
        return null;
    }

    /**
     * Internal method to get translation for a specific language.
     * @private
     */
    _getTranslationForLanguage(pageFile, language) {
        const variants = pageFile.variants || [];

        if (variants.length === 0) {
            // Fallback to original value
            return pageFile.originalValue || null;
        }

        // Find variant matching the language
        for (const variant of variants) {
            if (variant.language && variant.language.toLowerCase() === language.toLowerCase()) {
                const value = variant._value;
                if (value) {
                    return value;
                }
            }
        }

        // Fallback to original value if translation not found
        return pageFile.originalValue || null;
    }

    /**
     * Internal method to load a .locbook file into cache.
     * @private
     */
    _loadLocbookFile(filePath) {
        const json = fs.readFileSync(filePath, 'utf8');
        const locbook = JSON.parse(json);

        if (!locbook || !locbook.pages) {
            return;
        }

        const fileName = path.basename(filePath, '.locbook');
        this._cache[fileName] = locbook;

        // Build key index for fast lookup
        const pages = locbook.pages || [];
        for (const page of pages) {
            const pageFiles = page.pageFiles || [];
            if (pageFiles.length === 0) {
                continue;
            }

            if (!this._keyIndex[fileName]) {
                this._keyIndex[fileName] = {};
            }

            for (const pageFile of pageFiles) {
                const key = (pageFile.key || '').trim();
                if (key) {
                    this._keyIndex[fileName][key] = pageFile;
                }
            }
        }
    }
}

// Export for Node.js
if (typeof module !== 'undefined' && module.exports) {
    module.exports = L;
}

// Export for ES6 modules
if (typeof window !== 'undefined') {
    window.L = L;
}

