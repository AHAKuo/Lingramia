import type { Locbook } from './types';

type LocbookOpenResult = {
  canceled: boolean;
  data?: Locbook;
  path?: string | null;
};

type LocbookSaveResult = {
  canceled: boolean;
  path?: string | null;
};

declare global {
  interface Window {
    lingramia: {
      getVersion: () => Promise<string>;
      newLocbook: () => Promise<LocbookOpenResult>;
      openLocbook: () => Promise<LocbookOpenResult>;
      saveLocbook: (payload: { data: Locbook; path: string | null }) => Promise<LocbookSaveResult>;
    };
  }
}

export {};
