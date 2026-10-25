export const chargeKinds=["DUES","EXTRA"] as const;
export const chargeCategories=["FIXTURE","REPAIR","HEATING","UTILITY","INSURANCE","OTHER"] as const;
export type ChargeKind=typeof chargeKinds[number];
export type ChargeCategory=typeof chargeCategories[number];
