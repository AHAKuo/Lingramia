#!/usr/bin/env python3
"""
Example usage of the L reader class.
"""

from l import L

def main():
    # Create a new reader instance
    reader = L()
    
    # Set the resource path containing .locbook files
    reader.set_resource_path("./Resources")
    
    # Set the active language
    reader.set_language("en")
    
    # Look up translations
    play_text = reader.key("menu_play")
    print(f"Play: {play_text}")
    
    # Switch to Japanese
    reader.set_language("jp")
    play_text_jp = reader.key("menu_play")
    print(f"Play (JP): {play_text_jp}")
    
    # Use hybrid mode to find by original value
    reader.set_language("en")
    settings_text = reader.key("Settings", hybrid_key=True)
    print(f"Settings: {settings_text}")

if __name__ == "__main__":
    main()

