import { useCallback, useMemo, useState } from 'react';
import { parseLocbook, serializeLocbook } from '../../models/locbookModel';
import { getConfigValue } from '../../services/configManager';
import { requestTranslation } from '../../services/translationAPI';

const demoLocbook = {
  pages: [
    {
      aboutPage: 'Demo page to illustrate Lingramia layout.',
      pageId: '-4302',
      pageFiles: [
        {
          key: 'greeting_hello',
          originalValue: 'Hello World',
          variants: [
            { _value: 'Hello World', language: 'en' },
            { _value: 'こんにちは', language: 'jp' },
            { _value: 'أهلا و سهلا', language: 'ar' },
          ],
        },
      ],
    },
  ],
};

const createId = () =>
  (window?.crypto?.randomUUID ? window.crypto.randomUUID() : `tab-${Date.now()}-${Math.random().toString(16).slice(2)}`);

const makeTabFromLocbook = ({ data, path }) => ({
  id: createId(),
  name: path ? path.split(/[/\\]/).pop() : 'Untitled.locbook',
  path,
  data,
  dirty: false,
});

export default function useLocbook() {
  const initialTab = useMemo(() => makeTabFromLocbook({ data: demoLocbook, path: null }), []);
  const [tabs, setTabs] = useState([initialTab]);
  const [activeTabId, setActiveTabId] = useState(initialTab.id);
  const [selectedPageId, setSelectedPageId] = useState(initialTab.data.pages?.[0]?.pageId ?? null);
  const [selectedFieldKey, setSelectedFieldKey] = useState(null);

  const activeTab = useMemo(() => tabs.find((tab) => tab.id === activeTabId), [tabs, activeTabId]);

  const selectTab = useCallback((tabId) => {
    setActiveTabId(tabId);
    const nextTab = tabs.find((tab) => tab.id === tabId);
    if (nextTab?.data?.pages?.length) {
      setSelectedPageId(nextTab.data.pages[0].pageId);
      setSelectedFieldKey(null);
    }
  }, [tabs]);

  const newLocbook = useCallback(() => {
    const blankLocbook = { pages: [] };
    const newTab = makeTabFromLocbook({ data: blankLocbook, path: null });
    setTabs((prev) => [...prev, newTab]);
    setActiveTabId(newTab.id);
    setSelectedPageId(null);
    setSelectedFieldKey(null);
  }, []);

  const openLocbook = useCallback(async () => {
    if (!window?.lingramia?.openLocbook) {
      console.warn('Open dialog unavailable in current environment.');
      return;
    }

    const result = await window.lingramia.openLocbook();
    if (!result) return;

    const parsed = parseLocbook(result.content);
    const tab = makeTabFromLocbook({ data: parsed, path: result.path });
    setTabs((prev) => [...prev, tab]);
    setActiveTabId(tab.id);
    if (parsed.pages?.length) {
      setSelectedPageId(parsed.pages[0].pageId);
    }
  }, []);

  const closeTab = useCallback((tabId) => {
    setTabs((prev) => prev.filter((tab) => tab.id !== tabId));
    if (activeTabId === tabId && tabs.length > 1) {
      const nextTab = tabs.find((tab) => tab.id !== tabId);
      if (nextTab) {
        setActiveTabId(nextTab.id);
        setSelectedPageId(nextTab.data?.pages?.[0]?.pageId ?? null);
        setSelectedFieldKey(null);
      }
    }
  }, [activeTabId, tabs]);

  const updateActiveTab = useCallback((updateFn) => {
    setTabs((prevTabs) => prevTabs.map((tab) => {
      if (tab.id !== activeTabId) return tab;
      const updatedData = updateFn(tab.data);
      return {
        ...tab,
        data: updatedData,
        dirty: true,
      };
    }));
  }, [activeTabId]);

  const saveLocbook = useCallback(async () => {
    if (!activeTab) return null;
    const serialized = serializeLocbook(activeTab.data);

    if (window?.lingramia?.saveLocbook) {
      const filePath = await window.lingramia.saveLocbook({
        defaultPath: activeTab.path || activeTab.name,
        content: serialized,
      });
      if (filePath) {
        setTabs((prevTabs) => prevTabs.map((tab) => (
          tab.id === activeTabId
            ? { ...tab, dirty: false, path: filePath, name: filePath.split(/[/\\]/).pop() }
            : tab
        )));
      }
      return filePath;
    }

    return null;
  }, [activeTab, activeTabId]);

  const addPage = useCallback(() => {
    updateActiveTab((data) => {
      const pageId = `page-${Date.now()}`;
      const newPage = {
        aboutPage: '',
        pageId,
        pageFiles: [],
      };
      setSelectedPageId(pageId);
      return {
        ...data,
        pages: [...(data.pages || []), newPage],
      };
    });
  }, [updateActiveTab]);

  const updatePageMeta = useCallback((pageId, partial) => {
    updateActiveTab((data) => ({
      ...data,
      pages: data.pages.map((page) => (page.pageId === pageId ? { ...page, ...partial } : page)),
    }));
  }, [updateActiveTab]);

  const addFieldToPage = useCallback((pageId) => {
    updateActiveTab((data) => ({
      ...data,
      pages: data.pages.map((page) => {
        if (page.pageId !== pageId) return page;
        const newField = {
          key: `new_key_${Date.now()}`,
          originalValue: '',
          variants: [],
        };
        setSelectedFieldKey(newField.key);
        return { ...page, pageFiles: [...page.pageFiles, newField] };
      }),
    }));
  }, [updateActiveTab, setSelectedFieldKey]);

  const updateField = useCallback((pageId, fieldKey, partial) => {
    updateActiveTab((data) => ({
      ...data,
      pages: data.pages.map((page) => {
        if (page.pageId !== pageId) return page;
        return {
          ...page,
          pageFiles: page.pageFiles.map((field) =>
            field.key === fieldKey ? { ...field, ...partial } : field
          ),
        };
      }),
    }));
    if (partial?.key) {
      setSelectedFieldKey(partial.key);
    }
  }, [updateActiveTab, setSelectedFieldKey]);

  const upsertVariant = useCallback((pageId, fieldKey, variant) => {
    updateActiveTab((data) => ({
      ...data,
      pages: data.pages.map((page) => {
        if (page.pageId !== pageId) return page;
        return {
          ...page,
          pageFiles: page.pageFiles.map((field) => {
            if (field.key !== fieldKey) return field;
            const existingIndex = field.variants.findIndex((v) => v.language === variant.language);
            if (existingIndex >= 0) {
              const variants = [...field.variants];
              variants[existingIndex] = variant;
              return { ...field, variants };
            }
            return { ...field, variants: [...field.variants, variant] };
          }),
        };
      }),
    }));
  }, [updateActiveTab]);

  const removeVariant = useCallback((pageId, fieldKey, language) => {
    updateActiveTab((data) => ({
      ...data,
      pages: data.pages.map((page) => {
        if (page.pageId !== pageId) return page;
        return {
          ...page,
          pageFiles: page.pageFiles.map((field) =>
            field.key === fieldKey
              ? { ...field, variants: field.variants.filter((v) => v.language !== language) }
              : field
          ),
        };
      }),
    }));
  }, [updateActiveTab]);

  const autoTranslateVariant = useCallback(async (pageId, fieldKey, language, sourceText) => {
    const apiKey = await getConfigValue('openai.key');
    if (!apiKey) {
      console.warn('OpenAI API key not configured. Using mock translation response.');
    }

    const translation = await requestTranslation({
      text: sourceText,
      targetLanguage: language,
      apiKey,
    });

    upsertVariant(pageId, fieldKey, { language, _value: translation });
  }, [upsertVariant]);

  return {
    tabs,
    activeTab,
    activeTabId,
    selectedPageId,
    selectedFieldKey,
    setSelectedPageId,
    setSelectedFieldKey,
    selectTab,
    newLocbook,
    openLocbook,
    closeTab,
    saveLocbook,
    addPage,
    updatePageMeta,
    addFieldToPage,
    updateField,
    upsertVariant,
    removeVariant,
    autoTranslateVariant,
  };
}
