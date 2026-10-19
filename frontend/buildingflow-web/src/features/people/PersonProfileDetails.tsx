"use client";
import {useTranslations,useLocale} from "next-intl";
import {DetailField} from "@/components/management/WorkspacePresentation";
import {formatDateOnly} from "@/lib/format";
import {PersonCodeValue} from "./PersonCode";
import type {Person} from "./types";
export function PersonProfileDetails({person}:{person:Person}){
 const t=useTranslations("ui");const locale=useLocale();
 const identity=person.citizenship==="FOREIGN"?(person.personKind==="LEGAL"?"personForeignCompanyId":"personPassport"):person.personKind==="LEGAL"?"personTaxId":"personNationalId";
 const genders={FEMALE:"personFemale",MALE:"personMale",OTHER:"personOther",UNSPECIFIED:"notSpecified"};
 return <><DetailField label={t("personKind")}>{person.personKind?t(person.personKind==="LEGAL"?"personLegal":"personNatural"):t("notSpecified")}</DetailField><DetailField label={t("personCitizenship")}>{person.citizenship?`${t(person.citizenship==="FOREIGN"?"personForeign":"personDomestic")} · ${person.countryCode}`:t("notSpecified")}</DetailField><DetailField label={t(identity)}>{person.identityNumber??"—"}</DetailField>{person.personKind!=="LEGAL"&&<><DetailField label={t("personBirthDate")}>{person.birthDate?formatDateOnly(person.birthDate,locale):"—"}</DetailField><DetailField label={t("personGender")}>{person.gender?t(genders[person.gender]):"—"}</DetailField></>}<DetailField label={t("address")}>{person.address??"—"}</DetailField><DetailField label={t("personLinkedAccount")}>{person.linkedUserId??"—"}</DetailField><DetailField label={t("personCode")}><PersonCodeValue person={person}/></DetailField></>;
}
