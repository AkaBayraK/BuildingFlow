"use client";
import {useLocale} from "next-intl";
import {useCallback} from "react";
import {useIdentity} from "../auth/AuthGate";
import {useNotification} from "../components/feedback/FeedbackContext";
import {connectionError} from "./problem";
import {ApiError,authorizedRequest} from "./authorized-request";
export {ApiError} from "./authorized-request";
export function useAuthorizedApi() {
 const identity=useIdentity();const locale=useLocale();const notify=useNotification();
 return useCallback(<T,>(path:string,options:RequestInit={},responseType:"json"|"blob"="json"):Promise<T>=>authorizedRequest<T>(identity,locale,notify,process.env.NEXT_PUBLIC_API_URL??"http://localhost:5005",path,options,fetch,responseType),[identity,locale,notify]);
}
export type Property = {id:string;name:string;city:string;district:string;address:string;version:number;createdAt:string;isEnabled:boolean;type:PropertyType;floorCount:number|null;buildingCount:number;unitCount:number;canChangeType:boolean};
export type PropertyPage = {items:Property[];page:number;pageSize:number;totalCount:number};
export type Building = {id:string;propertyId:string;name:string;version:number;createdAt:string;isEnabled:boolean;unitCount:number;floorCount:number|null};
export type Unit = {id:string;propertyId:string;buildingId:string|null;type:UnitType;number:string;floor:number;grossSquareMeters:number;version:number;createdAt:string;isEnabled:boolean};
export type PagedRows<T> = {items:T[];page:number;pageSize:number;totalCount:number};
export function errorMessage(error:unknown,locale:string="tr") {
  return error instanceof ApiError ? error.message : connectionError(locale);
}

export type PropertyType="APARTMENT"|"SITE"|"RESIDENCE"|"MIXED";
export type UnitType="APARTMENT"|"OFFICE"|"SHOP"|"VILLA"|"WAREHOUSE"|"WORKSHOP";
export const propertyTypeNames:Record<PropertyType,string>={APARTMENT:"apartmentBuilding",SITE:"community",RESIDENCE:"residence",MIXED:"mixedUse"};
export const unitTypeNames:Record<UnitType,string>={APARTMENT:"apartment",OFFICE:"office",SHOP:"shop",VILLA:"villa",WAREHOUSE:"warehouse",WORKSHOP:"workshop"};
