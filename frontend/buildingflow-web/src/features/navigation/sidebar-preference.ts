export const sidebarPreferenceKey = "buildingflow-sidebar-v1";
type StorageReader = {getItem: (key: string) => string | null};
type StorageWriter = {setItem: (key: string, value: string) => void};

// A fixed UI value only: never serialize identity, scope, route or search state.
export function readSidebarPreference(storage: StorageReader): boolean {
  try { return storage.getItem(sidebarPreferenceKey) === "collapsed"; }
  catch { return false; }
}
export function writeSidebarPreference(storage: StorageWriter, collapsed: boolean): void {
  try { storage.setItem(sidebarPreferenceKey, collapsed ? "collapsed" : "expanded"); }
  catch { /* The in-memory control remains usable when browser storage is unavailable. */ }
}
