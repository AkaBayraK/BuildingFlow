export const marker = key => `<!-- buildingflow-mobile:${key} -->`;
export function managedBody(key,body,existing='') {
 const start=marker(key),end=`<!-- /buildingflow-mobile:${key} -->`;
 if(!existing)return `${start}\n${body.trim()}\n${end}\n`;
 const a=existing.indexOf(start),b=existing.indexOf(end);
 if(a<0||b<a||existing.indexOf(start,a+start.length)>=0||existing.indexOf(end,b+end.length)>=0)throw Error('Yönetilen alan eksik veya yineleniyor: '+key);
 return existing.slice(0,a)+`${start}\n${body.trim()}\n${end}`+existing.slice(b+end.length);
}
export function matchItems(plans,items) {
 const result=new Map();
 for(const p of plans){const found=items.filter(i=>i.content?.body?.includes(marker(p.key)));if(found.length>1)throw Error('Yinelenen pano anahtarı: '+p.key);if(!found.length&&items.some(i=>i.title===p.title||i.content?.title===p.title))throw Error('Aynı başlıklı sahiplenilmemiş kayıt var: '+p.key);if(found[0]&&found[0].content?.type!=='DraftIssue')throw Error('Taslak olmayan kayıt değiştirilemez: '+p.key);result.set(p.key,found[0]??null);}
 return result;
}
export function parsePhases(markdown){const phases=[...markdown.matchAll(/^## Faz (\d{2}) — (.+)\n([\s\S]*?)(?=^## Faz \d{2} — |$(?![\s\S]))/gm)].map(m=>({number:Number(m[1]),title:m[2].trim(),scope:m[3].trim()}));if(phases.length!==15||phases.some((p,i)=>p.number!==i+1))throw Error('Kanonik 15 faz planı doğrulanamadı.');return phases;}
export function validatePlans(plans,fields){const keys=new Set();const statuses=fields.find(f=>f.name==='Status');const priority=fields.find(f=>f.name==='Priority');if(!statuses?.options)throw Error('Pano Status alanı eksik.');for(const p of plans){if(keys.has(p.key))throw Error('Plan anahtarı tekrarlı.');keys.add(p.key);if(!statuses.options.some(o=>o.name===p.status))throw Error('Bilinmeyen durum: '+p.status);if(p.priority&&!priority?.options?.some(o=>o.name===p.priority))throw Error('Bilinmeyen öncelik.');if(/gh[pousr]_[a-zA-Z0-9]{20,}|github_pat_|Bearer\s+[a-zA-Z0-9._-]{20,}|\/Users\/|00008140-|192\.168\./.test(p.body))throw Error('Paylaşılmaması gereken yerel/kimlik bilgisi bulundu.');}return {statuses,priority};}

export function assertCreationSafe(current,journal,key){if(!current&&(journal?.pending||journal?.itemId))throw Error('Önceki oluşturmanın sonucu belirsiz; yeni kayıt açılmadı: '+key);}
