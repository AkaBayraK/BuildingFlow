"use client";
import type {ComponentProps} from "react";
import {Popover as Primitive} from "radix-ui";
import {cn} from "@/lib/utils";
export const Popover=Primitive.Root;
export const PopoverTrigger=Primitive.Trigger;
export function PopoverContent({className,sideOffset=6,...props}:ComponentProps<typeof Primitive.Content>){
 return <Primitive.Portal><Primitive.Content sideOffset={sideOffset} className={cn('z-[60] rounded-xl border bg-popover p-3 text-popover-foreground shadow-lg outline-none',className)} {...props}/></Primitive.Portal>;
}
