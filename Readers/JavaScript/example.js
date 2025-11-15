/**
 * Example usage of the L reader class.
 */

const L = require('./l.js');

function main() {
    // Create a new reader instance
    const reader = new L();
    
    // Set the resource path containing .locbook files
    reader.setResourcePath('./Resources');
    
    // Set the active language
    reader.setLanguage('en');
    
    // Look up translations
    const playText = reader.key('menu_play');
    console.log(`Play: ${playText}`);
    
    // Switch to Japanese
    reader.setLanguage('jp');
    const playTextJp = reader.key('menu_play');
    console.log(`Play (JP): ${playTextJp}`);
    
    // Use hybrid mode to find by original value
    reader.setLanguage('en');
    const settingsText = reader.key('Settings', true);
    console.log(`Settings: ${settingsText}`);
}

main();

