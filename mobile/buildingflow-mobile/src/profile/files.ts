export interface PrivateFiles {write(name:string,value:string):Promise<void>;read(name:string):Promise<string>;drop(name:string):Promise<void>;}
export type PrivateIdentity={name:string;keyId:string};
