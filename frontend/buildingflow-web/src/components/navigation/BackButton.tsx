import Link from "next/link";
import {ArrowLeft} from "lucide-react";
import {Button} from "@/components/ui/button";
import {cn} from "@/lib/utils";

export function BackButton({href,label,className}:{href:string;label:string;className?:string}){
 return <Button asChild variant="outline" size="sm" className={cn("print-hidden max-w-full",className)}><Link href={href}><ArrowLeft aria-hidden="true"/><span className="truncate">{label}</span></Link></Button>;
}
