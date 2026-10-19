// Shared phone and e-mail rules for every form (İŞ19). No package: Turkish numbers get +90 and the 3-3-2-2 grouping,
// other countries keep their own + country code. The API accepts the same characters (+ digits space - ( )).
const allowed=/[^0-9+ ()-]/g;
// While typing only characters a phone can contain are kept; a single + is allowed at the start.
export function phoneChars(value:string){const kept=value.replace(allowed,"");return (kept.startsWith("+")?"+":"")+kept.replace(/\+/g,"");}
export function formatPhone(value:string){
 const raw=value.trim();if(!raw)return "";
 const digits=raw.replace(/\D/g,"");if(!digits)return raw.startsWith("+")?"+":"";
 let national:string|null=null;
 if(raw.startsWith("+"))national=digits.startsWith("90")?digits.slice(2):null;
 else if(digits.startsWith("00"))return formatPhone("+"+digits.slice(2));
 else if(digits.startsWith("90")&&digits.length===12)national=digits.slice(2);
 else if(digits.startsWith("0"))national=digits.slice(1);
 else national=digits;
 if(national===null)return "+"+digits;
 const parts=[national.slice(0,3),national.slice(3,6),national.slice(6,8),national.slice(8)].filter(Boolean);
 return ["+90",...parts].join(" ");
}
// Turkey: +90 and ten digits starting with 2–5 (landline or mobile). Elsewhere: + country code and 8–15 digits in total.
export function validPhone(value:string){
 const formatted=formatPhone(value),digits=formatted.replace(/\D/g,"");
 if(formatted.startsWith("+90 "))return /^90[2-5][0-9]{9}$/.test(digits);
 return formatted.startsWith("+")&&digits.length>=8&&digits.length<=15&&!digits.startsWith("0");
}
export const normalizeEmail=(value:string)=>value.trim().toLowerCase();
// Same practical shape as the API: one @, a dotted domain, no spaces, at most 254 characters.
export const validEmail=(value:string)=>{const v=normalizeEmail(value);return v.length<=254&&/^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$/.test(v);};
