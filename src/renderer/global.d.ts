import type { PreloadAPI } from '../main/preload';

declare global {
  interface Window {
    api: PreloadAPI;
  }
}

export {};
