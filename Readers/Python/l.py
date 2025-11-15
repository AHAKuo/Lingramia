"""
Lightweight reader class for .locbook files.
Provides a simple API for reading localization data from JSON-based .locbook files.
"""

import json
import os
from typing import Optional, Dict, List, Any


class L:
    """
    Reader class for .locbook files.
    Namespace equivalent: AHAKuo.Lingramia.API
    """
    
    def __init__(self):
        self._resource_path: Optional[str] = None
        self._current_language: str = "en"
        self._cache: Dict[str, Dict[str, Any]] = {}
        self._key_index: Dict[str, Dict[str, Dict[str, Any]]] = {}
    
    def set_resource_path(self, path_to_resources: str) -> None:
        """
        Sets the resource path containing .locbook files.
        Loads all .locbook files from the specified directory into cache.
        
        Args:
            path_to_resources: Path to the folder containing .locbook files
            
        Raises:
            ValueError: If path is None or empty
            FileNotFoundError: If directory does not exist
        """
        if not path_to_resources or not path_to_resources.strip():
            raise ValueError("Resource path cannot be None or empty.")
        
        if not os.path.isdir(path_to_resources):
            raise FileNotFoundError(f"Directory not found: {path_to_resources}")
        
        self._resource_path = path_to_resources
        self._cache.clear()
        self._key_index.clear()
        
        # Load all .locbook files from the directory
        for filename in os.listdir(path_to_resources):
            if filename.endswith('.locbook'):
                file_path = os.path.join(path_to_resources, filename)
                try:
                    self._load_locbook_file(file_path)
                except Exception as ex:
                    # Log warning but don't crash - continue loading other files
                    print(f"Warning: Failed to load {file_path}: {ex}")
    
    def get_language(self) -> str:
        """
        Gets the current active language code.
        
        Returns:
            The current language code (e.g., "en", "jp", "ar")
        """
        return self._current_language
    
    def set_language(self, code: str) -> None:
        """
        Sets the active language for translations.
        
        Args:
            code: Language code (e.g., "en", "jp", "ar")
            
        Raises:
            ValueError: If code is None or empty
        """
        if not code or not code.strip():
            raise ValueError("Language code cannot be None or empty.")
        
        self._current_language = code
    
    def key(self, key: str, hybrid_key: bool = False) -> Optional[str]:
        """
        Looks up a translation value by key.
        
        Args:
            key: The key to look up
            hybrid_key: If True, tries key, then originalValue, then aliases as fallback
            
        Returns:
            The translated value for the current language, or None if not found
        """
        if not key or not key.strip():
            return None
        
        # Standard mode: lookup by key only
        if not hybrid_key:
            return self._lookup_by_key(key)
        
        # Hybrid mode: try key, then originalValue, then aliases
        result = self._lookup_by_key(key)
        if result is not None:
            return result
        
        # Try originalValue
        by_original_value = self._lookup_by_original_value(key)
        if by_original_value is not None:
            return by_original_value
        
        # Try aliases
        by_alias = self._lookup_by_alias(key)
        if by_alias is not None:
            return by_alias
        
        # Fallback: return None
        return None
    
    def _lookup_by_key(self, key: str) -> Optional[str]:
        """Internal method to lookup by key."""
        for file_name, page_files in self._key_index.items():
            if key in page_files:
                page_file = page_files[key]
                return self._get_translation_for_language(page_file, self._current_language)
        return None
    
    def _lookup_by_original_value(self, original_value: str) -> Optional[str]:
        """Internal method to lookup by originalValue."""
        for file_name, page_files in self._key_index.items():
            for page_file in page_files.values():
                if page_file.get('originalValue', '').lower() == original_value.lower():
                    return self._get_translation_for_language(page_file, self._current_language)
        return None
    
    def _lookup_by_alias(self, alias: str) -> Optional[str]:
        """Internal method to lookup by alias."""
        for file_name, page_files in self._key_index.items():
            for page_file in page_files.values():
                aliases = page_file.get('aliases', [])
                if aliases and any(a.lower() == alias.lower() for a in aliases if a):
                    return self._get_translation_for_language(page_file, self._current_language)
        return None
    
    def _get_translation_for_language(self, page_file: Dict[str, Any], language: str) -> Optional[str]:
        """Internal method to get translation for a specific language."""
        variants = page_file.get('variants', [])
        
        if not variants:
            # Fallback to original value
            return page_file.get('originalValue')
        
        # Find variant matching the language
        for variant in variants:
            if variant.get('language', '').lower() == language.lower():
                value = variant.get('_value')
                if value:
                    return value
        
        # Fallback to original value if translation not found
        return page_file.get('originalValue')
    
    def _load_locbook_file(self, file_path: str) -> None:
        """Internal method to load a .locbook file into cache."""
        with open(file_path, 'r', encoding='utf-8') as f:
            locbook = json.load(f)
        
        if not locbook or 'pages' not in locbook:
            return
        
        file_name = os.path.splitext(os.path.basename(file_path))[0]
        self._cache[file_name] = locbook
        
        # Build key index for fast lookup
        pages = locbook.get('pages', [])
        for page in pages:
            page_files = page.get('pageFiles', [])
            if not page_files:
                continue
            
            if file_name not in self._key_index:
                self._key_index[file_name] = {}
            
            for page_file in page_files:
                key = page_file.get('key', '').strip()
                if key:
                    self._key_index[file_name][key] = page_file

