"""
Minimal unit tests for the L reader class.
Run with: python -m pytest test_l.py
Or: python test_l.py
"""

import unittest
import os
import tempfile
import json
from l import L


class TestL(unittest.TestCase):
    
    def setUp(self):
        """Set up test fixtures."""
        # Create a temporary test directory
        self.test_dir = tempfile.mkdtemp()
        
        # Create a sample .locbook file
        sample_locbook = {
            "pages": [
                {
                    "pageId": "intro",
                    "aboutPage": "Introduction and main menu localization",
                    "pageFiles": [
                        {
                            "key": "menu_play",
                            "originalValue": "Play",
                            "variants": [
                                {
                                    "language": "en",
                                    "_value": "Play"
                                },
                                {
                                    "language": "jp",
                                    "_value": "プレイ"
                                }
                            ],
                            "aliases": ["play_button"]
                        }
                    ]
                }
            ]
        }
        
        with open(os.path.join(self.test_dir, "test.locbook"), "w", encoding="utf-8") as f:
            json.dump(sample_locbook, f)
    
    def tearDown(self):
        """Clean up test fixtures."""
        import shutil
        shutil.rmtree(self.test_dir)
    
    def test_set_resource_path_valid_path_loads_files(self):
        """Test that SetResourcePath loads files correctly."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        # Should not raise exception
        self.assertIsNotNone(reader)
    
    def test_set_resource_path_invalid_path_raises_exception(self):
        """Test that SetResourcePath raises exception for invalid path."""
        reader = L()
        with self.assertRaises(FileNotFoundError):
            reader.set_resource_path("nonexistent/path")
    
    def test_get_language_default_returns_en(self):
        """Test that GetLanguage returns 'en' by default."""
        reader = L()
        self.assertEqual("en", reader.get_language())
    
    def test_set_language_valid_code_sets_language(self):
        """Test that SetLanguage sets the language correctly."""
        reader = L()
        reader.set_language("jp")
        self.assertEqual("jp", reader.get_language())
    
    def test_key_standard_mode_returns_translation(self):
        """Test that Key returns translation in standard mode."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        reader.set_language("en")
        
        result = reader.key("menu_play")
        self.assertEqual("Play", result)
    
    def test_key_different_language_returns_correct_translation(self):
        """Test that Key returns correct translation for different language."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        reader.set_language("jp")
        
        result = reader.key("menu_play")
        self.assertEqual("プレイ", result)
    
    def test_key_hybrid_mode_with_alias_returns_translation(self):
        """Test that Key in hybrid mode finds translation by alias."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        reader.set_language("en")
        
        result = reader.key("play_button", hybrid_key=True)
        self.assertEqual("Play", result)
    
    def test_key_nonexistent_key_returns_none(self):
        """Test that Key returns None for nonexistent key."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        
        result = reader.key("nonexistent_key")
        self.assertIsNone(result)
    
    def test_key_hybrid_mode_with_original_value_returns_translation(self):
        """Test that Key in hybrid mode finds translation by originalValue."""
        reader = L()
        reader.set_resource_path(self.test_dir)
        reader.set_language("en")
        
        result = reader.key("Play", hybrid_key=True)
        self.assertEqual("Play", result)


if __name__ == "__main__":
    unittest.main()

