import {categoryStyle} from "./category-style";
import type {ExpenseCategory} from "./model";
export function ExpenseCategoryLabel({category,label}:{category:ExpenseCategory;label:string}){const c=categoryStyle[category];return <span className="request-type"><span className={`request-type-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span>{label}</span>;}
