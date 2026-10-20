import type {NavigationLink} from "./navigation.ts";

// Menu labels only. No entity data, network, logging or persistent query state.
export function normalizeNavigationText(value: string, locale: string): string {
  return value.toLocaleLowerCase(locale === "tr" ? "tr-TR" : "en-US")
    .normalize("NFD").replace(/\p{M}/gu, "").replace(/ı/g, "i").trim();
}

export function searchNavigation<T extends NavigationLink>(visibleItems: readonly T[], query: string, label: (item: T) => string, locale: string): T[] {
  const terms = normalizeNavigationText(query, locale).split(/\s+/u).filter(Boolean);
  return visibleItems.filter(item => {
    const text = normalizeNavigationText(label(item), locale);
    return terms.every(term => text.includes(term));
  });
}
