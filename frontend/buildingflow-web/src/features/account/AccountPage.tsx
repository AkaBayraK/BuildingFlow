"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {BookOpenCheck,Building2,Camera,CheckCircle2,CircleDashed,FileText,KeyRound,ShieldCheck,Trash2,TriangleAlert,Undo2,UserRound,XCircle,type LucideIcon} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage,ApiError} from "@/services/api";
import {useNotification} from "@/components/feedback/Notifications";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {RoleBadge,ToneBadge,WorkspaceHeading,type Tone} from "@/components/management/WorkspacePresentation";
import {AccountAvatar} from "@/components/AccountAvatar";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {PhoneInput} from "@/components/forms/ContactInputs";
import {formatPhone,validPhone} from "@/lib/contact";
import {Label} from "@/components/ui/label";
import {Skeleton} from "@/components/ui/skeleton";
import {Dialog,DialogContent,DialogDescription,DialogFooter,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {formatDateTime} from "@/lib/format";
import {personName} from "@/lib/person-names";
import {ConsentDialog} from "./ConsentDialog";
import {accountChanged,useAccount} from "./useAccount";
import {accountName,allows,approveCommand,parseConsents,parseProfile,profileCommand,withdrawCommand,type AccountConsent,type PhotoAction} from "./model";
import {PhotoError,photoTypes,preparePhoto} from "./photo";

const roleKeys:Record<string,string>={SUPER_ADMIN:"roleSuperAdmin",SITE_ADMIN:"roleSiteAdmin",MANAGER:"roleManager",ACCOUNTANT:"roleAccountant",RESIDENT:"roleResident",OWNER:"roleOwner",TENANT:"roleTenant"};
const stateTones:Record<string,{tone:Tone;icon:LucideIcon}>={ACTIVE:{tone:"success",icon:CheckCircle2},OUTDATED:{tone:"warning",icon:TriangleAlert},NONE:{tone:"info",icon:CircleDashed},WITHDRAWN:{tone:"error",icon:XCircle}};
type Prompt={consent:AccountConsent;purpose?:string;then?:()=>void};

// İŞ19 "Hesap bilgilerim" (TraceFlow profile page counterpart): own photo, name and phone, account, password and consents.
export function AccountPage(){
 const t=useTranslations("account"),ui=useTranslations("ui"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity(),notify=useNotification();
 const {account,failed}=useAccount();const userId=identity.subject??"";
 const [consents,setConsents]=useState<AccountConsent[]|null>(null),[consentError,setConsentError]=useState("");
 const [first,setFirst]=useState(""),[last,setLast]=useState(""),[phone,setPhone]=useState(""),[loadedVersion,setLoadedVersion]=useState(-1);
 const [busy,setBusy]=useState(false),[error,setError]=useState(""),[prompt,setPrompt]=useState<Prompt|null>(null),[promptError,setPromptError]=useState(""),[removing,setRemoving]=useState(false);
 const file=useRef<HTMLInputElement>(null);
 const profile=account?.profile??null;
 // The form follows the stored profile whenever a newer version arrives (after a save here or on mobile).
 // A profile never saved (version 0) has no name yet; the sign-in names are offered, normalised by the name rule.
 if(profile&&profile.version!==loadedVersion){const token=identity.tokenParsed??{},fresh=profile.version===0&&!profile.firstName&&!profile.lastName;
  const names=fresh?personName(String(token.given_name??""),String(token.family_name??"")):{firstName:profile.firstName,lastName:profile.lastName};
  setLoadedVersion(profile.version);setFirst(names.firstName);setLast(names.lastName);setPhone(profile.phone??"");}
 useEffect(()=>{if(!userId)return;const c=new AbortController();
  void api<unknown>("/api/v1/account/consents",{signal:c.signal}).then(v=>{if(!c.signal.aborted){setConsents(parseConsents(v,userId));setConsentError("");}}).catch(e=>{if(!c.signal.aborted)setConsentError(errorMessage(e,locale));});
  return()=>c.abort();},[api,userId,locale,account]);
 const photoConsent=consents?.find(c=>c.feature==="PROFILE_PHOTO");
 const name=accountName(profile,identity.tokenParsed?.name??account?.username??identity.tokenParsed?.preferred_username??"");

 async function save(action:PhotoAction,photo:string|null=null,values={firstName:first,lastName:last,phone}){
  if(!profile||busy)return false;const names=personName(values.firstName,values.lastName);
  if(!names.firstName||!names.lastName){setError(t("nameRequired"));return false;}
  const number=formatPhone(values.phone);if(number&&!validPhone(number)){setError(ui("phoneFieldInvalid"));return false;}
  setBusy(true);setError("");
  try{
   const body=profileCommand(profile,crypto.randomUUID(),{...names,phone:number},action,photo);
   const result=await api<{profile:unknown}>("/api/v1/account/profile/commands",{method:"POST",body:JSON.stringify(body)},"json",[409]);
   parseProfile(result.profile,userId);accountChanged();return true;
  }catch(e){
   if(e instanceof ApiError&&e.status===409){setError(t("staleProfile"));accountChanged();}
   else if(e instanceof ApiError&&e.feedback?.code==="BF_CONSENT_REQUIRED"&&photoConsent){setPrompt({consent:photoConsent,purpose:t("photoNeedsConsent")});}
   else setError(errorMessage(e,locale));
   return false;
  }finally{setBusy(false);}
 }
 async function decide(consent:AccountConsent,action:"APPROVE"|"WITHDRAW"){
  const command=action==="APPROVE"?approveCommand(consent,crypto.randomUUID()):withdrawCommand(consent,crypto.randomUUID());
  await api("/api/v1/account/consents/commands",{method:"POST",body:JSON.stringify(command)});accountChanged();
 }
 async function approvePrompt(){
  if(!prompt)return;setBusy(true);setPromptError("");
  try{await decide(prompt.consent,"APPROVE");const next=prompt.then;setPrompt(null);notify.success(t("consentSaved"));next?.();}
  catch(e){setPromptError(errorMessage(e,locale));}finally{setBusy(false);}
 }
 function choosePhoto(){
  if(!photoConsent?.text){setError(t("photoTextMissing"));return;}
  // The file picker opens only with an active consent; the approval click itself opens it.
  if(!allows(photoConsent)){setPromptError("");setPrompt({consent:photoConsent,purpose:t("photoNeedsConsent"),then:()=>file.current?.click()});return;}
  file.current?.click();
 }
 async function picked(selected:File|undefined){
  if(!selected)return;setError("");
  try{const prepared=await preparePhoto(selected);URL.revokeObjectURL(prepared.previewUrl);if(await save("REPLACE",prepared.base64))notify.success(t("photoSaved"));}
  catch(e){setError(e instanceof PhotoError?t("photo_"+e.reason):errorMessage(e,locale));}
 }
 async function withdraw(consent:AccountConsent){
  const effect=consent.feature==="PROFILE_PHOTO"?t("withdrawPhotoEffect"):consent.feature==="REQUEST_PHOTO"?t("withdrawRequestEffect"):t("withdrawEffect");
  if(!window.confirm(t("withdrawConfirm",{name:locale==="en"?consent.nameEn:consent.nameTr})+"\n\n"+effect))return;
  setBusy(true);try{await decide(consent,"WITHDRAW");notify.success(t("consentWithdrawn"));}catch(e){setConsentError(errorMessage(e,locale));}finally{setBusy(false);}
 }

 if(failed)return <div className="py-10"><MessageBanner action={<Button variant="view" onClick={()=>accountChanged()}>{t("tryAgain")}</Button>}>{errorMessage(failed,locale)}</MessageBanner></div>;
 if(!account||!profile)return <div className="py-10" role="status"><span className="sr-only">{t("loading")}</span><Skeleton className="h-64"/></div>;
 const superAdmin=account.roles.includes("SUPER_ADMIN");
 return <div className="py-8">
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={name} icon={UserRound}/>
  <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
   <section aria-labelledby="account-photo" className="rounded-2xl border bg-card p-5 shadow-sm">
    <h2 id="account-photo" className="flex items-center gap-2 text-base font-semibold"><Camera className="size-4"/>{t("photoTitle")}</h2>
    <div className="mt-4 flex flex-wrap items-center gap-5">
     <AccountAvatar key={profile.photoId??"none"} path={profile.photoId?`/api/v1/account/profile/photo/${profile.photoId}`:null} name={name} className="size-24 text-2xl"/>
     <div className="flex min-w-0 flex-1 flex-col gap-2">
      <p className="text-sm text-muted-foreground">{profile.photoId?t("photoShared"):t("photoHint")}</p>
      <p className="text-xs text-muted-foreground">{photoConsent?(allows(photoConsent)?t("photoConsentActive",{version:photoConsent.approvedVersion??0}):t("photoConsentMissing")):t("photoTextMissing")}</p>
      <div className="flex flex-wrap gap-2">
       <input ref={file} type="file" accept={photoTypes.join(",")} className="hidden" onChange={e=>{void picked(e.target.files?.[0]);e.target.value="";}}/>
       <Button disabled={busy} onClick={choosePhoto}><Camera/>{profile.photoId?t("changePhoto"):t("addPhoto")}</Button>
       {profile.photoId&&<Button variant="danger" disabled={busy} onClick={()=>setRemoving(true)}><Trash2/>{t("removePhoto")}</Button>}
      </div>
     </div>
    </div>
   </section>
   <section aria-labelledby="account-personal" className="rounded-2xl border bg-card p-5 shadow-sm">
    <h2 id="account-personal" className="flex items-center gap-2 text-base font-semibold"><UserRound className="size-4"/>{t("personalTitle")}</h2>
    <form className="mt-4 grid gap-3 sm:grid-cols-2" onSubmit={e=>{e.preventDefault();void save("KEEP").then(ok=>{if(ok)notify.success(t("profileSaved"));});}}>
     <div className="space-y-2"><Label htmlFor="account-first">{t("firstName")} *</Label><Input id="account-first" maxLength={80} autoComplete="given-name" value={first} onChange={e=>setFirst(e.target.value)} onBlur={()=>setFirst(v=>personName(v,last).firstName)}/></div>
     <div className="space-y-2"><Label htmlFor="account-last">{t("lastName")} *</Label><Input id="account-last" maxLength={80} autoComplete="family-name" value={last} onChange={e=>setLast(e.target.value)} onBlur={()=>setLast(v=>personName(first,v).lastName)}/></div>
     <p className="-mt-1 text-xs text-muted-foreground sm:col-span-2">{t("nameRule")}</p>
     <div className="space-y-2"><Label htmlFor="account-phone">{t("phone")}</Label><PhoneInput id="account-phone" value={phone} onValueChange={setPhone}/></div>
     <div className="space-y-2"><Label htmlFor="account-email">{t("email")}</Label><Input id="account-email" type="email" readOnly value={account.email??""} placeholder={t("emailMissing")} aria-describedby="account-email-help" className="bg-muted/40"/><p id="account-email-help" className="text-xs text-muted-foreground">{t("emailReadOnly")}</p></div>
     {!!error&&<div className="sm:col-span-2"><MessageBanner presentation="inline">{error}</MessageBanner></div>}
     <div className="sm:col-span-2 flex flex-wrap items-center justify-between gap-2"><small className="text-muted-foreground">{profile.modifiedAt?t("lastSaved",{date:formatDateTime(profile.modifiedAt,locale)}):t("neverSaved")}</small><Button type="submit" disabled={busy}>{busy?t("saving"):t("save")}</Button></div>
    </form>
   </section>
   <section aria-labelledby="account-info" className="rounded-2xl border bg-card p-5 shadow-sm">
    <h2 id="account-info" className="flex items-center gap-2 text-base font-semibold"><Building2 className="size-4"/>{t("accountTitle")}</h2>
    <dl className="mt-3 divide-y text-sm">
     <div className="flex flex-wrap gap-2 py-2"><dt className="w-36 text-muted-foreground">{t("username")}</dt><dd className="font-medium">{account.username??"—"}</dd></div>
     <div className="flex flex-wrap gap-2 py-2"><dt className="w-36 text-muted-foreground">{t("roles")}</dt><dd className="flex flex-wrap gap-1">{account.roles.length?account.roles.map(r=><RoleBadge key={r} role={r} label={roleKeys[r]?t(roleKeys[r]):r}/>):"—"}</dd></div>
    </dl>
    <h3 className="mt-4 text-sm font-semibold">{t("propertiesTitle")}</h3>
    {superAdmin?<p className="mt-2 text-sm text-muted-foreground">{t("allProperties")}</p>
     :account.contexts.length===0?<p className="mt-2 text-sm text-muted-foreground">{t("noProperties")}</p>
     :<ul className="mt-2 grid gap-2 sm:grid-cols-2">{account.contexts.map(c=><li key={c.propertyId+(c.unitId??"")+c.role} className="rounded-lg border p-3"><span className="block truncate font-medium">{c.propertyName}</span><span className="mt-1 flex flex-wrap items-center gap-2"><RoleBadge role={c.role} label={roleKeys[c.role]?t(roleKeys[c.role]):c.role}/>{c.unitNumber&&<small className="text-muted-foreground">{t("unitValue",{unit:c.unitNumber})}</small>}</span></li>)}</ul>}
    {account.hasMore&&<p className="mt-2 text-xs text-muted-foreground">{t("moreProperties")}</p>}
   </section>
   <section aria-labelledby="account-security" className="rounded-2xl border bg-card p-5 shadow-sm">
    <h2 id="account-security" className="flex items-center gap-2 text-base font-semibold"><KeyRound className="size-4"/>{t("passwordTitle")}</h2>
    <p className="mt-2 text-sm text-muted-foreground">{t("passwordHint")}</p>
    <Button variant="warn" className="mt-4" onClick={()=>void identity.login({action:"UPDATE_PASSWORD",redirectUri:window.location.href,locale})}><KeyRound/>{t("changePassword")}</Button>
   </section>
   <section aria-labelledby="account-consents" className="rounded-2xl border bg-card p-5 shadow-sm lg:col-span-2">
    <h2 id="account-consents" className="flex items-center gap-2 text-base font-semibold"><ShieldCheck className="size-4"/>{t("consentsTitle")}</h2>
    <p className="mt-2 text-sm text-muted-foreground">{t("consentsHint")}</p>
    {consentError&&<div className="mt-3"><MessageBanner presentation="inline">{consentError}</MessageBanner></div>}
    {!consents&&!consentError&&<Skeleton className="mt-3 h-16"/>}
    {consents&&<ul className="mt-3 grid gap-2 md:grid-cols-3">{consents.map(c=>{const label=locale==="en"?c.nameEn:c.nameTr,info=c.kind==="INFORMATION";
     return <li key={c.typeCode} className="flex flex-col gap-2 rounded-lg border p-3">
      <span className="flex flex-wrap items-center justify-between gap-2"><span className="font-medium">{label}</span><ToneBadge tone={stateTones[c.state].tone} icon={stateTones[c.state].icon}>{t((info?"notice_":"state_")+c.state)}</ToneBadge></span>
      <small className="text-muted-foreground">{c.approvedVersion&&c.approvedAt?t("approvedValue",{version:c.approvedVersion,date:formatDateTime(c.approvedAt,locale)}):c.text?t("currentValue",{version:c.text.textVersion}):t("noText")}</small>
      <span className="flex flex-wrap gap-2">{c.text&&<Button size="sm" variant={c.state==="ACTIVE"?"view":"default"} disabled={busy} onClick={()=>{setPromptError("");setPrompt({consent:c});}}>{c.state==="ACTIVE"?<><FileText/>{t("readText")}</>:<><BookOpenCheck/>{info?t("readAndMark"):t("readAndApprove")}</>}</Button>}
       {!info&&(c.state==="ACTIVE"||c.state==="OUTDATED")&&<Button size="sm" variant="danger" disabled={busy} onClick={()=>void withdraw(c)}><Undo2/>{t("withdraw")}</Button>}</span>
     </li>;})}</ul>}
   </section>
  </div>
  {prompt&&<ConsentDialog consent={prompt.consent} purpose={prompt.purpose} busy={busy} error={promptError} onApprove={()=>void approvePrompt()} onClose={()=>{if(!busy){setPrompt(null);if(prompt.purpose)setError(t("photoNotAdded"));}}}/>}
  {removing&&<Dialog open onOpenChange={open=>{if(!open&&!busy)setRemoving(false);}}><DialogContent><DialogHeader><DialogTitle>{t("removePhoto")}</DialogTitle><DialogDescription>{t("removePhotoHelp")}</DialogDescription></DialogHeader>
   <DialogFooter><Button variant="outline" disabled={busy} onClick={()=>setRemoving(false)}>{t("cancel")}</Button><Button variant="destructive" disabled={busy} onClick={()=>void save("REMOVE").then(ok=>{if(ok){setRemoving(false);notify.success(t("photoRemoved"));}})}>{t("removePhoto")}</Button></DialogFooter></DialogContent></Dialog>}
 </div>;
}
