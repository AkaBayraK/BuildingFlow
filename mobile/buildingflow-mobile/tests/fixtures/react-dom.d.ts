// Test fixture uses only the installed React DOM root API; no new type package.
declare module 'react-dom/client' {
 export function createRoot(container:Element):{render(children:import('react').ReactNode):void};
}
