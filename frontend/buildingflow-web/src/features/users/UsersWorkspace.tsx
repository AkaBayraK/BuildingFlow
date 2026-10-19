"use client";
import {useEffect,useRef,useState} from "react";
import {personName as normalizedName} from "@/lib/person-names";
import {useLocale,useTranslations} from "next-intl";
import type {ColumnDef} from "@tanstack/react-table";
import {UserCog,Plus,Pencil,KeyRound,Link2,Unlink,Search,UserPen} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage,type Property} from "@/services/api";
import {parsePropertyPage,parsePerson} from "@/services/management-contracts";
import {useNotification} from "@/components/feedback/Notifications";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {Badge} from "@/components/ui/badge";
import {Dialog,DialogHeader,DialogTitle,DialogDescription,DialogFooter} from "@/components/ui/dialog";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {RecordList} from "@/components/management/RecordList";
import {WorkspaceHeading,ListToolbar,toolbarSelectClass,RoleBadge,StatusBadge} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useSearch} from "@/features/people/useRows";
import {personName,type Person} from "@/features/people/types";
import {EntityEditor} from "@/features/hierarchy/EntityEditor";
import {AccountAvatar} from "@/components/AccountAvatar";
import {EmailInput} from "@/components/forms/ContactInputs";
import {normalizeEmail,validEmail} from "@/lib/contact";
import {readUser,readUserPage,displayName,validUsername,strongPassword,allRoles,siteRoles,type ManagedUser,type UserPage,type Role} from "./model";

function useSaving(){const lock=useRef(false);const [busy,setBusy]=useState(false);
 async function run(action:()=>Promise<void>){if(lock.current)return;lock.current=true;setBusy(true);try{await action();}finally{lock.current=false;setBusy(false);}}
 return {busy,run};}

function useProperties(){
 const api=useAuthorizedApi();const [items,setItems]=useState<Property[]>([]);
 useEffect(()=>{const c=new AbortController();void api<unknown>("/api/v1/properties?page=1&pageSize=100&sortBy=name&sortDirection=asc",{signal:c.signal}).then(v=>{if(!c.signal.aborted)setItems(parsePropertyPage(v).items);}).catch(()=>{});return()=>c.abort();},[api]);
 return items;
}

export function UsersWorkspace(){
 const t=useTranslations("users"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity(),notice=useNotification();
 const superAdmin=identity.hasRealmRole("SUPER_ADMIN"),allowed=superAdmin||identity.hasRealmRole("SITE_ADMIN");
 const [term,setTerm]=useState(""),search=useSearch(term),[page,setPage]=useState(1),[data,setData]=useState<UserPage|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 const [editor,setEditor]=useState<{user:ManagedUser|null}|null>(null),[linking,setLinking]=useState<ManagedUser|null>(null);const {busy,run}=useSaving();
 const properties=useProperties();
 useEffect(()=>{setPage(1);},[search]);
 useEffect(()=>{const c=new AbortController();setLoading(allowed);setError("");if(allowed)void api<unknown>("/api/v1/users?"+new URLSearchParams({page:String(page),pageSize:"20",...(search?{search}:{})}),{signal:c.signal}).then(v=>{if(!c.signal.aborted)setData(readUserPage(v));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,page,search,revision,locale,allowed]);
 const refresh=()=>setRevision(n=>n+1);
 function reset(user:ManagedUser){void run(async()=>{try{await api(`/api/v1/users/${encodeURIComponent(user.id)}/password-reset`,{method:"POST"});notice.success(t("resetSent"));}catch(e){notice.error(errorMessage(e,locale));}});}
 const columns:ColumnDef<ManagedUser>[]=[
  {id:"user",header:t("user"),cell:({row})=><div className="flex min-w-0 items-center gap-3"><AccountAvatar key={row.original.photoId??"none"} path={row.original.photoId?`/api/v1/users/${encodeURIComponent(row.original.id)}/photo`:null} name={displayName(row.original)} className="size-9 text-xs"/><div className="min-w-0"><strong className="block truncate">{displayName(row.original)}</strong><span className="text-xs text-muted-foreground">{row.original.username}</span></div></div>},
  {id:"email",header:t("email"),cell:({row})=><span className="wrap-anywhere">{row.original.email??"—"}</span>},
  {id:"roles",header:t("roles"),cell:({row})=><RoleBadges roles={row.original.roles}/>},
  {id:"people",header:t("linkedPeople"),cell:({row})=><LinkedSummary user={row.original}/>},
  {id:"status",header:ui("status"),cell:({row})=><StatusBadge enabled={row.original.enabled}/>},
  {id:"actions",header:ui("actions"),cell:({row})=><UserActions user={row.original} busy={busy} onEdit={()=>setEditor({user:row.original})} onLink={()=>setLinking(row.original)} onReset={()=>reset(row.original)}/>}
 ];
 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;
 return <div className="py-8"><WorkspaceHeading title={t("title")} description={t("help")} scope={t(superAdmin?"scopeAll":"scopeOwn")} icon={UserCog} action={<Button onClick={()=>setEditor({user:null})}><Plus/>{t("newUser")}</Button>}/>
  <section className="overflow-hidden rounded-2xl border bg-card shadow-sm"><ListToolbar term={term} onTerm={setTerm} searchLabel={t("search")} placeholder={t("searchPlaceholder")} loading={loading} onRefresh={refresh} onReset={()=>setTerm("")} resetEnabled={!!term}/>
   <RecordList compact data={data} columns={columns} page={page} onPage={setPage} loading={loading} error={error} onRetry={refresh} empty={t("empty")} card={u=><><div className="flex items-start justify-between gap-2"><div className="flex min-w-0 items-center gap-3"><AccountAvatar key={u.photoId??"none"} path={u.photoId?`/api/v1/users/${encodeURIComponent(u.id)}/photo`:null} name={displayName(u)} className="size-10 text-sm"/><div className="min-w-0"><strong>{displayName(u)}</strong><p className="text-xs text-muted-foreground">{u.username} · {u.email??"—"}</p></div></div><StatusBadge enabled={u.enabled}/></div><RoleBadges roles={u.roles}/><LinkedSummary user={u}/><UserActions user={u} busy={busy} onEdit={()=>setEditor({user:u})} onLink={()=>setLinking(u)} onReset={()=>reset(u)}/></>}/>
  </section>
  {editor&&<UserDialog user={editor.user} superAdmin={superAdmin} properties={properties} onClose={()=>setEditor(null)} onSaved={refresh}/>}
  {linking&&<LinkDialog user={linking} properties={properties} onClose={()=>setLinking(null)} onChanged={updated=>{setLinking(updated);refresh();}}/>}
 </div>;
}

function RoleBadges({roles}:{roles:Role[]}){const t=useTranslations("users");
 return <span className="flex flex-wrap gap-1">{roles.length===0?<span className="text-xs text-muted-foreground">{t("noRole")}</span>:roles.map(r=><RoleBadge key={r} role={r} label={t(`role_${r}`)}/>)}</span>;}

function LinkedSummary({user}:{user:ManagedUser}){const t=useTranslations("users");
 if(user.linkedPeople.length===0)return <Badge variant="secondary">{t("noLinkedPerson")}</Badge>;
 return <ul className="space-y-0.5 text-xs">{user.linkedPeople.map(p=><li key={p.personId}><strong>{p.fullName}</strong> · {p.propertyName}</li>)}</ul>;}

function UserActions({user,busy,onEdit,onLink,onReset}:{user:ManagedUser;busy:boolean;onEdit:()=>void;onLink:()=>void;onReset:()=>void}){
 const t=useTranslations("users");
 return <div className="flex flex-wrap justify-end gap-1"><Button variant="edit" size="icon-sm" disabled={!user.canEdit||busy} aria-label={t("editUser",{name:user.username})} title={t("editUser",{name:user.username})} onClick={onEdit}><Pencil/></Button><Button variant="outline" size="icon-sm" disabled={!user.canEdit||busy} aria-label={t("linkPerson",{name:user.username})} title={t("linkPerson",{name:user.username})} onClick={onLink}><Link2/></Button><Button variant="warn" size="icon-sm" disabled={!user.canEdit||busy||!user.email} aria-label={t("sendReset",{name:user.username})} title={t("sendReset",{name:user.username})} onClick={onReset}><KeyRound/></Button></div>;
}

function UserDialog({user,superAdmin,properties,onClose,onSaved}:{user:ManagedUser|null;superAdmin:boolean;properties:Property[];onClose:()=>void;onSaved:()=>void}){
 const t=useTranslations("users"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const assignable:readonly Role[]=superAdmin?allRoles:siteRoles;
 const [username,setUsername]=useState(user?.username??""),[email,setEmail]=useState(user?.email??""),[firstName,setFirstName]=useState(user?.firstName??""),[lastName,setLastName]=useState(user?.lastName??""),[enabled,setEnabled]=useState(user?.enabled??true);
 const [roles,setRoles]=useState<Role[]>(user?.roles.filter(r=>assignable.includes(r))??["RESIDENT"]),[property,setProperty]=useState(""),[password,setPassword]=useState(""),[error,setError]=useState("");
 function toggle(role:Role){setRoles(current=>current.includes(role)?current.filter(r=>r!==role):[...current,role]);}
 function save(e:React.FormEvent){e.preventDefault();setError("");
  if(!user&&!validUsername(username.trim().toLowerCase())||!validEmail(email)||!firstName.trim()||!lastName.trim()){setError(t("invalid"));return;}
  if(!user&&!superAdmin&&!property){setError(t("propertyRequired"));return;}
  if(!user&&password&&!strongPassword(password)){setError(t("passwordWeak"));return;}
  void run(async()=>{try{
   const names=normalizedName(firstName,lastName);const body=user?{email:normalizeEmail(email),...names,enabled,roles}:{username:username.trim().toLowerCase(),email:normalizeEmail(email),...names,enabled,roles,propertyId:property||null,temporaryPassword:password||null};
   readUser(await api(user?`/api/v1/users/${encodeURIComponent(user.id)}`:"/api/v1/users",{method:user?"PUT":"POST",body:JSON.stringify(body)}));
   notice.success(t(user?"saved":password?"createdWithPassword":"createdWithEmail"));onSaved();onClose();
  }catch(reason){setError(errorMessage(reason,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{user?t("editUser",{name:user.username}):t("newUser")}</DialogTitle><DialogDescription>{t(user?"editHelp":"createHelp")}</DialogDescription></DialogHeader>
  <form onSubmit={save} className="space-y-4"><fieldset disabled={busy} className="space-y-4">{error&&<MessageBanner>{error}</MessageBanner>}
   <Label htmlFor="user-username">{t("username")} *</Label><Input id="user-username" autoComplete="off" disabled={!!user} maxLength={50} value={username} onChange={e=>setUsername(e.target.value)} aria-describedby="user-username-help"/><p id="user-username-help" className="text-xs text-muted-foreground">{t("usernameHelp")}</p>
   <Label htmlFor="user-email">{t("email")} *</Label><EmailInput id="user-email" autoComplete="off" required hint value={email} onValueChange={setEmail}/>
   <div className="grid gap-3 sm:grid-cols-2"><div className="space-y-2"><Label htmlFor="user-first">{t("firstName")} *</Label><Input id="user-first" maxLength={100} value={firstName} onChange={e=>setFirstName(e.target.value)}/></div><div className="space-y-2"><Label htmlFor="user-last">{t("lastName")} *</Label><Input id="user-last" maxLength={100} value={lastName} onChange={e=>setLastName(e.target.value)}/></div></div>
   <fieldset className="space-y-2"><legend className="text-sm font-medium">{t("roles")}</legend><div className="flex flex-wrap gap-3">{assignable.map(role=><label key={role} className="flex items-center gap-2 text-sm"><input type="checkbox" checked={roles.includes(role)} onChange={()=>toggle(role)}/>{t(`role_${role}`)}</label>)}</div>{user&&user.roles.some(r=>!assignable.includes(r))&&<p className="text-xs text-muted-foreground">{t("otherRolesKept")}</p>}</fieldset>
   {!user&&<><Label htmlFor="user-property">{t("property")}{superAdmin?"":" *"}</Label><select id="user-property" className={toolbarSelectClass+" w-full"} value={property} onChange={e=>setProperty(e.target.value)}><option value="">{t(superAdmin?"noProperty":"chooseProperty")}</option>{properties.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select>
    <Label htmlFor="user-password">{t("temporaryPassword")}</Label><Input id="user-password" type="password" autoComplete="new-password" maxLength={128} value={password} onChange={e=>setPassword(e.target.value)} aria-describedby="user-password-help"/><p id="user-password-help" className="text-xs text-muted-foreground">{t("temporaryPasswordHelp")}</p></>}
   <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={enabled} onChange={e=>setEnabled(e.target.checked)}/>{ui("active")}</label>
  </fieldset><DialogFooter><Button type="button" variant="outline" disabled={busy} onClick={onClose}>{ui("cancel")}</Button><Button type="submit" disabled={busy}>{busy?ui("saving"):ui("save")}</Button></DialogFooter></form></ManagementPanel></Dialog>;
}

function LinkDialog({user,properties,onClose,onChanged}:{user:ManagedUser;properties:Property[];onClose:()=>void;onChanged:(user:ManagedUser)=>void}){
 const t=useTranslations("users"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),notice=useNotification();const {busy,run}=useSaving();
 const [property,setProperty]=useState(properties[0]?.id??""),[term,setTerm]=useState(""),search=useSearch(term),[people,setPeople]=useState<Person[]>([]),[loading,setLoading]=useState(false),[error,setError]=useState(""),[completing,setCompleting]=useState<Person|null>(null),[peopleRevision,setPeopleRevision]=useState(0);
 useEffect(()=>{if(!property&&properties[0])setProperty(properties[0].id);},[properties,property]);
 useEffect(()=>{const c=new AbortController();setPeople([]);if(!property)return;setLoading(true);void api<{items:unknown[]}>(`/api/v1/properties/${property}/people?`+new URLSearchParams({page:"1",pageSize:"10",...(search?{search}:{})}),{signal:c.signal}).then(v=>{if(!c.signal.aborted)setPeople(v.items.map(row=>parsePerson(row,property)));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,property,search,locale,peopleRevision]);
 function link(person:Person){setError("");void run(async()=>{try{const updated=readUser(await api(`/api/v1/users/${encodeURIComponent(user.id)}/people`,{method:"POST",body:JSON.stringify({propertyId:person.propertyId,personId:person.id,version:person.version})}));notice.success(t("linked",{name:personName(person)}));onChanged(updated);}catch(e){setError(errorMessage(e,locale));}});}
 function unlink(personId:string,propertyId:string,version:number){setError("");void run(async()=>{try{await api(`/api/v1/users/${encodeURIComponent(user.id)}/people/${personId}?`+new URLSearchParams({propertyId,version:String(version)}),{method:"DELETE"});notice.success(t("unlinked"));onChanged({...user,linkedPeople:user.linkedPeople.filter(p=>p.personId!==personId)});}catch(e){setError(errorMessage(e,locale));}});}
 return <Dialog open onOpenChange={open=>{if(!open&&!busy)onClose();}}><ManagementPanel showCloseButton={!busy}><DialogHeader><DialogTitle>{t("linkTitle",{name:displayName(user)})}</DialogTitle><DialogDescription>{t("linkHelp")}</DialogDescription></DialogHeader>
  {error&&<MessageBanner>{error}</MessageBanner>}
  <section><h3 className="text-sm font-semibold">{t("currentLinks")}</h3>{user.linkedPeople.length===0?<p className="text-sm text-muted-foreground">{t("noLinkedPerson")}</p>:<ul className="mt-2 space-y-2">{user.linkedPeople.map(p=><li key={p.personId} className="flex items-center justify-between gap-2 rounded-lg border p-2 text-sm"><span><strong>{p.fullName}</strong> · {p.propertyName}</span><Button variant="danger" size="sm" disabled={busy} onClick={()=>unlink(p.personId,p.propertyId,p.version)}><Unlink/>{t("unlink")}</Button></li>)}</ul>}</section>
  <section className="space-y-3"><h3 className="text-sm font-semibold">{t("addLink")}</h3>
   <Label htmlFor="link-property">{t("property")}</Label><select id="link-property" className={toolbarSelectClass+" w-full"} value={property} onChange={e=>setProperty(e.target.value)}>{properties.map(p=><option key={p.id} value={p.id}>{p.name}</option>)}</select>
   <Label htmlFor="link-search">{t("searchPerson")}</Label><div className="relative"><Search aria-hidden="true" className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"/><Input id="link-search" className="pl-9" value={term} onChange={e=>setTerm(e.target.value)}/></div>
   {loading?<p role="status" className="text-sm">{ui("loading")}</p>:people.length===0?<p className="text-sm text-muted-foreground">{t("noPeople")}</p>:<ul className="space-y-2">{people.map(person=>{const taken=!!person.linkedUserId&&person.linkedUserId!==user.id;return <li key={person.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border p-2 text-sm"><span className="min-w-0"><strong>{personName(person)}</strong><span className="block text-xs text-muted-foreground">{person.units?.map(u=>`${u.number} · ${t(`unitRole_${u.role}`)}`).join(", ")||"—"}{taken?` · ${t("linkedElsewhere")}`:person.linkedUserId===user.id?` · ${t("alreadyLinked")}`:""}</span>{!person.personKind&&<span className="mt-1 block text-xs text-amber-700">{t("profileIncomplete")}</span>}</span>{!person.personKind?<Button size="sm" variant="edit" disabled={busy} onClick={()=>setCompleting(person)}><UserPen/>{t("completeProfile")}</Button>:<Button size="sm" disabled={busy||taken||person.linkedUserId===user.id} onClick={()=>link(person)}><Link2/>{t("link")}</Button>}</li>;})}</ul>}
  </section>
  <DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{ui("close")}</Button></DialogFooter></ManagementPanel>{completing&&<EntityEditor kind="person" url={`/api/v1/properties/${completing.propertyId}/people`} target={completing} onClose={()=>setCompleting(null)} onSaved={()=>{setPeopleRevision(n=>n+1);notice.success(t("profileCompleted"));}}/>}</Dialog>;
}
