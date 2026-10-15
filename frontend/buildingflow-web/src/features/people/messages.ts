export type PersonMessage={id:string;propertyId:string;personId:string;title:string;body:string;createdAt:string;readAt:string|null};
export type MessagePage={items:PersonMessage[];page:number;pageSize:number;totalCount:number};
export function parseMessages(value:unknown,property?:string,person?:string):MessagePage{
 const row=value as Partial<MessagePage>|null;
 const guid=(value:unknown)=>typeof value==='string'&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value);
 if(!row||!Array.isArray(row.items)||!Number.isSafeInteger(row.page)||Number(row.page)<1||row.pageSize!==10||!Number.isSafeInteger(row.totalCount)||Number(row.totalCount)<row.items.length||row.items.length>10)throw new Error('Invalid messages');
 for(const item of row.items)if(!item||!guid(item.id)||!guid(item.propertyId)||!guid(item.personId)||property&&item.propertyId!==property||person&&item.personId!==person||typeof item.title!=='string'||!item.title.trim()||item.title.length>200||typeof item.body!=='string'||!item.body.trim()||item.body.length>2000||typeof item.createdAt!=='string'||!Number.isFinite(Date.parse(item.createdAt))||item.readAt!==null&&(typeof item.readAt!=='string'||!Number.isFinite(Date.parse(item.readAt))))throw new Error('Invalid message scope');
 if(new Set(row.items.map(item=>item.id)).size!==row.items.length)throw new Error('Duplicate messages');return row as MessagePage;
}
