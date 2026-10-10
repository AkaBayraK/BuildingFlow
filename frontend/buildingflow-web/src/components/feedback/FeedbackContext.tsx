"use client";
import {createContext,useContext} from "react";
import type {MessageKind} from "./message-types";
import type {NotificationMessage} from "./notification-text";
export type {NotificationMessage} from "./notification-text";
export type Notifications=Record<MessageKind,(message:NotificationMessage)=>void>&{field:(text:string)=>void};
export const FeedbackContext=createContext<Notifications|null>(null);
export const NotificationSurfaceContext=createContext<HTMLElement|null>(null);
export function useNotification(){const value=useContext(FeedbackContext);if(!value)throw new Error("Notification provider required");return value;}
