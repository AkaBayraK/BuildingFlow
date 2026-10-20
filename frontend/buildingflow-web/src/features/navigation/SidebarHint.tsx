"use client";
import type {ReactElement} from "react";
import {Tooltip} from "radix-ui";

export function SidebarHint({enabled,label,children}:{enabled:boolean;label:string;children:ReactElement}) {
  if (!enabled) return children;
  return <Tooltip.Root><Tooltip.Trigger asChild>{children}</Tooltip.Trigger><Tooltip.Portal><Tooltip.Content side="right" sideOffset={12} className="sidebar-tooltip">{label}<Tooltip.Arrow className="sidebar-tooltip-arrow"/></Tooltip.Content></Tooltip.Portal></Tooltip.Root>;
}
