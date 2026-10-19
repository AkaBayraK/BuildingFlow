// Validate before the Router decodes untrusted native URL input. Never log authentication URLs.
export const MAX_INCOMING_LENGTH=4096;
export function safeIncomingPath(path:string):string{
 if(!path||path.length>MAX_INCOMING_LENGTH||/[\u0000-\u0020\u007f]/.test(path))return '/invalid-link';
 try{decodeURIComponent(path);}catch{return '/invalid-link';}
 return path;
}
