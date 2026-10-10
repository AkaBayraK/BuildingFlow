"use client";

import type {ComponentProps} from "react";
import {DialogContent} from "@/components/ui/dialog";
import {cn} from "@/lib/utils";

/** Shared presentation for management forms and record details. */
export function ManagementPanel({className,...props}:Omit<ComponentProps<typeof DialogContent>,"placement">){
  return <DialogContent
    {...props}
    placement="right"
    className={cn(
      "bg-card p-6 sm:max-w-lg",
      "[&_[data-slot=dialog-header]]:border-b [&_[data-slot=dialog-header]]:pb-5 [&_[data-slot=dialog-header]]:pr-6 [&_[data-slot=dialog-header]]:text-left",
      "[&_[data-slot=dialog-title]]:text-2xl [&_[data-slot=dialog-title]]:font-bold [&_[data-slot=dialog-title]]:wrap-anywhere",
      "[&_[data-slot=dialog-footer]]:border-t [&_[data-slot=dialog-footer]]:pt-5",
      className
    )}
  />;
}
