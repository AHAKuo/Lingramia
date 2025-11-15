/**
 * Minimal unit tests for the L reader class.
 * Run with: node l.test.js
 * Or use a test framework like Jest, Mocha, etc.
 */

const L = require('./l.js');
const fs = require('fs');
const path = require('path');
const os = require('os');

// Simple test runner
function runTests() {
    let passed = 0;
    let failed = 0;
    const tests = [];

    function test(name, fn) {
        tests.push({ name, fn });
    }

    function assert(condition, message) {
        if (!condition) {
            throw new Error(message || 'Assertion failed');
        }
    }

    // Create temporary test directory
    const testDir = fs.mkdtempSync(path.join(os.tmpdir(), 'locbook-test-'));

    // Create sample .locbook file
    const sampleLocbook = {
        pages: [
            {
                pageId: "intro",
                aboutPage: "Introduction and main menu localization",
                pageFiles: [
                    {
                        key: "menu_play",
                        originalValue: "Play",
                        variants: [
                            {
                                language: "en",
                                _value: "Play"
                            },
                            {
                                language: "jp",
                                _value: "プレイ"
                            }
                        ],
                        aliases: ["play_button"]
                    }
                ]
            }
        ]
    };

    fs.writeFileSync(
        path.join(testDir, "test.locbook"),
        JSON.stringify(sampleLocbook, null, 2)
    );

    // Test: SetResourcePath with valid path
    test('SetResourcePath with valid path loads files', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        assert(reader !== null, 'Reader should be created');
    });

    // Test: SetResourcePath with invalid path throws
    test('SetResourcePath with invalid path throws error', () => {
        const reader = new L();
        let threw = false;
        try {
            reader.setResourcePath('nonexistent/path');
        } catch (e) {
            threw = true;
        }
        assert(threw, 'Should throw error for invalid path');
    });

    // Test: GetLanguage returns default 'en'
    test('GetLanguage returns default "en"', () => {
        const reader = new L();
        assert(reader.getLanguage() === 'en', 'Default language should be "en"');
    });

    // Test: SetLanguage sets language correctly
    test('SetLanguage sets language correctly', () => {
        const reader = new L();
        reader.setLanguage('jp');
        assert(reader.getLanguage() === 'jp', 'Language should be set to "jp"');
    });

    // Test: Key returns translation in standard mode
    test('Key returns translation in standard mode', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        reader.setLanguage('en');
        const result = reader.key('menu_play');
        assert(result === 'Play', 'Should return "Play" for menu_play');
    });

    // Test: Key returns correct translation for different language
    test('Key returns correct translation for different language', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        reader.setLanguage('jp');
        const result = reader.key('menu_play');
        assert(result === 'プレイ', 'Should return Japanese translation');
    });

    // Test: Key in hybrid mode finds by alias
    test('Key in hybrid mode finds translation by alias', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        reader.setLanguage('en');
        const result = reader.key('play_button', true);
        assert(result === 'Play', 'Should find by alias in hybrid mode');
    });

    // Test: Key returns null for nonexistent key
    test('Key returns null for nonexistent key', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        const result = reader.key('nonexistent_key');
        assert(result === null, 'Should return null for nonexistent key');
    });

    // Test: Key in hybrid mode finds by originalValue
    test('Key in hybrid mode finds translation by originalValue', () => {
        const reader = new L();
        reader.setResourcePath(testDir);
        reader.setLanguage('en');
        const result = reader.key('Play', true);
        assert(result === 'Play', 'Should find by originalValue in hybrid mode');
    });

    // Run all tests
    console.log('Running tests...\n');
    for (const { name, fn } of tests) {
        try {
            fn();
            console.log(`✓ ${name}`);
            passed++;
        } catch (error) {
            console.error(`✗ ${name}`);
            console.error(`  ${error.message}`);
            failed++;
        }
    }

    // Cleanup
    fs.rmSync(testDir, { recursive: true, force: true });

    // Summary
    console.log(`\n${passed} passed, ${failed} failed`);
    process.exit(failed > 0 ? 1 : 0);
}

// Run tests if executed directly
if (require.main === module) {
    runTests();
}

module.exports = { runTests };

