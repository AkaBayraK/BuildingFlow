import type {CodedMessage} from "../../services/problem.ts";
import {translateCode} from "../../services/problem.ts";
import {translateUiText} from "../../i18n/translate-text.ts";
export type NotificationMessage=string|({text:string}&Partial<CodedMessage>);
export function notificationText(message:NotificationMessage,locale:string):string {
 if(typeof message!=="string"&&message.code)return translateCode({code:message.code,parameters:message.parameters??{}},locale);
 const text=typeof message==="string"?message:message.text;
 return text.split("\n").map(line=>translateUiText(line,locale)).join("\n");
}
