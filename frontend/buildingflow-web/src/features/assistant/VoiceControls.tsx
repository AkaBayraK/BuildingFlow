"use client";
import {useEffect,useRef,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {Mic,MicOff,Square,Volume2,VolumeX} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {cn} from "@/lib/utils";
import {assistantPath,pickRecorderType,readTranscript,readVoicePreference,readVoiceStatus,transcribeForm,voicePreferenceKey,voiceRoles,type AssistantQuota,type AssistantVoiceStatus,type VoicePreference} from "./model";

type Phase="idle"|"listening"|"transcribing";

// One place turns voice on or off. Recording uses MediaRecorder; the server's local STT returns text into the chat box.
export function VoiceControls({propertyId,disabled,onText,onQuota,onReadAloud}:{propertyId:string;disabled:boolean;onText:(text:string)=>void;onQuota:(quota:AssistantQuota)=>void;onReadAloud:(enabled:boolean)=>void}){
 const t=useTranslations("assistant"),locale=useLocale() as "tr"|"en",api=useAuthorizedApi(),identity=useIdentity();
 const allowed=voiceRoles.some(role=>identity.hasRealmRole(role));
 const [status,setStatus]=useState<AssistantVoiceStatus|null>(null),[preference,setPreference]=useState<VoicePreference>({voice:false,readAloud:false});
 const [phase,setPhase]=useState<Phase>("idle"),[seconds,setSeconds]=useState(0),[error,setError]=useState("");
 const recorder=useRef<MediaRecorder|null>(null),chunks=useRef<Blob[]>([]),started=useRef(0),timer=useRef<number|null>(null);
 const supported=typeof window!=="undefined"&&typeof MediaRecorder!=="undefined"&&!!navigator.mediaDevices?.getUserMedia;
 const speech=typeof window!=="undefined"&&"speechSynthesis" in window;
 useEffect(()=>{let stored:VoicePreference={voice:false,readAloud:false};try{stored=readVoicePreference(localStorage.getItem(voicePreferenceKey));}catch{}setPreference(stored);onReadAloud(stored.readAloud&&speech);
 // eslint-disable-next-line react-hooks/exhaustive-deps
 },[]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void api<unknown>(`${assistantPath}/voice/status`,{signal:c.signal},"json",[403]).then(v=>{if(!c.signal.aborted)setStatus(readVoiceStatus(v));}).catch(()=>{if(!c.signal.aborted)setStatus(null);});return()=>c.abort();},[api,allowed]);
 useEffect(()=>()=>{if(timer.current)window.clearInterval(timer.current);recorder.current?.stream.getTracks().forEach(track=>track.stop());},[]);
 if(!allowed)return null;
 const save=(next:VoicePreference)=>{setPreference(next);onReadAloud(next.readAloud&&speech);if(!next.readAloud&&speech)window.speechSynthesis.cancel();try{localStorage.setItem(voicePreferenceKey,JSON.stringify(next));}catch{}};
 const ready=!!status?.enabled&&supported;
 const reason=!status?t("voiceStatusUnknown"):!status.enabled?t("voiceProviderMissing"):!supported?t("voiceUnsupported"):"";
 async function upload(blob:Blob,durationMs:number){
  setPhase("transcribing");
  try{const result=readTranscript(await api<unknown>(`${assistantPath}/transcribe`,{method:"POST",body:transcribeForm(propertyId,blob,durationMs,locale)}));onQuota(result.quota);if(result.text)onText(result.text);else setError(t("voiceEmpty"));}
  catch(e){setError(errorMessage(e,locale));}finally{setPhase("idle");}
 }
 function stop(){if(timer.current){window.clearInterval(timer.current);timer.current=null;}recorder.current?.stop();}
 async function start(){
  setError("");const type=pickRecorderType(v=>MediaRecorder.isTypeSupported(v));
  try{
   const stream=await navigator.mediaDevices.getUserMedia({audio:true});const media=new MediaRecorder(stream,type?{mimeType:type}:undefined);
   recorder.current=media;chunks.current=[];started.current=Date.now();setSeconds(0);setPhase("listening");
   media.ondataavailable=e=>{if(e.data.size)chunks.current.push(e.data);};
   media.onstop=()=>{stream.getTracks().forEach(track=>track.stop());const blob=new Blob(chunks.current,{type:media.mimeType||type||"audio/webm"});chunks.current=[];recorder.current=null;
    if(blob.size>(status?.maxBytes??0)){setPhase("idle");setError(t("voiceTooLong"));return;}void upload(blob,Date.now()-started.current);};
   media.start();
   timer.current=window.setInterval(()=>{const elapsed=Math.floor((Date.now()-started.current)/1000);setSeconds(elapsed);if(elapsed>=(status?.maxSeconds??60))stop();},250);
  }catch{setPhase("idle");setError(t("voicePermission"));}
 }
 const Toggle=({checked,label,onChange,icon:Icon,offIcon:Off,disabled:off}:{checked:boolean;label:string;onChange:()=>void;icon:typeof Mic;offIcon:typeof Mic;disabled?:boolean})=>
  <button type="button" role="switch" aria-checked={checked} disabled={off} onClick={onChange} className={cn("inline-flex min-h-11 items-center gap-2 rounded-full border px-3 text-xs font-semibold focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-60",checked?"border-primary bg-primary text-primary-foreground":"bg-background text-muted-foreground hover:bg-accent")}>
   {checked?<Icon aria-hidden="true" className="size-4"/>:<Off aria-hidden="true" className="size-4"/>}{label}: {t(checked?"voiceOn":"voiceOff")}
  </button>;
 const minutes=`${Math.floor(seconds/60)}:${String(seconds%60).padStart(2,"0")}`;
 return <div className="mb-2 space-y-2">
  <div className="flex flex-wrap items-center gap-2">
   <Toggle checked={preference.voice&&ready} label={t("voiceTitle")} icon={Mic} offIcon={MicOff} disabled={!ready||phase!=="idle"} onChange={()=>save({...preference,voice:!preference.voice})}/>
   {speech&&<Toggle checked={preference.readAloud} label={t("readAloudTitle")} icon={Volume2} offIcon={VolumeX} onChange={()=>save({...preference,readAloud:!preference.readAloud})}/>}
   {preference.voice&&ready&&<Button type="button" variant={phase==="listening"?"danger":"view"} disabled={disabled||phase==="transcribing"} onClick={()=>phase==="listening"?stop():void start()} aria-pressed={phase==="listening"} aria-label={t(phase==="listening"?"voiceStop":"voiceStart")} title={t(phase==="listening"?"voiceStop":"voiceStart")}>
    {phase==="listening"?<Square aria-hidden="true"/>:<Mic aria-hidden="true"/>}{phase==="listening"?t("voiceListening",{time:minutes}):phase==="transcribing"?t("voiceTranscribing"):t("voiceStart")}
   </Button>}
  </div>
  <p className="text-xs text-muted-foreground" aria-live="polite">{reason||(phase==="listening"?t("voiceListeningHelp",{max:status?.maxSeconds??60}):preference.voice?t("voiceHelp"):"")}</p>
  {error&&<p className="text-xs text-destructive" role="alert">{error}</p>}
 </div>;
}
