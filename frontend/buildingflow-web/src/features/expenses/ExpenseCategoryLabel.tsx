"use client";
import {useDefinitionOptions} from "@/features/definitions/useDefinitionOptions";
import {categoryStyle} from "./category-style";
import {expenseCategories,type ExpenseCategory} from "./model";
export function ExpenseCategoryLabel({propertyId,category,label}:{propertyId:string;category:ExpenseCategory;label:string}){
 const definitions=useDefinitionOptions(propertyId,"EXPENSE_CATEGORY",expenseCategories);
 const c=definitions.style(category,categoryStyle[category]);
 return <span className="request-type"><span className={`request-type-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span>{definitions.label(category,label)}</span>;
}
