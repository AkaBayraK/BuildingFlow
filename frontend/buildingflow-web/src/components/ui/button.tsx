import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"
import { Slot } from "radix-ui"

const buttonVariants = cva(
  "inline-flex shrink-0 items-center justify-center gap-2 rounded-md text-sm font-medium whitespace-nowrap transition-all outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-primary/90",
        destructive:
          "bg-destructive text-white hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:bg-destructive/60 dark:focus-visible:ring-destructive/40",
        // İŞ19 soft button standard: outline is the grey tonal neutral action (cancel/clear/link/pagination); no white buttons.
        outline:
          "border border-(--action-neutral-border) bg-(--action-neutral-bg) text-(--action-neutral-text) hover:bg-(--action-neutral-hover)",
        // Input-like choices (combobox trigger, pick-one cards/lists) keep the white field look so they read as form fields, not actions.
        field:
          "border bg-background shadow-xs hover:bg-accent hover:text-accent-foreground",
        neutral:
          "border border-(--action-neutral-border) bg-(--action-neutral-bg) text-(--action-neutral-text) hover:bg-(--action-neutral-hover)",
        view:
          "border border-(--action-view-border) bg-(--action-view-bg) text-(--action-view-text) hover:bg-(--action-view-hover)",
        edit:
          "border border-(--action-edit-border) bg-(--action-edit-bg) text-(--action-edit-text) hover:bg-(--action-edit-hover)",
        warn:
          "border border-(--action-warn-border) bg-(--action-warn-bg) text-(--action-warn-text) hover:bg-(--action-warn-hover)",
        danger:
          "border border-(--action-danger-border) bg-(--action-danger-bg) text-(--action-danger-text) hover:bg-(--action-danger-hover) focus-visible:ring-destructive/20",
        secondary:
          "bg-secondary text-secondary-foreground hover:bg-secondary/80",
        ghost:
          "hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50",
        link: "text-primary underline-offset-4 hover:underline",
      },
      size: {
        default: "h-9 px-4 py-2 has-[>svg]:px-3",
        xs: "h-6 gap-1 rounded-md px-2 text-xs has-[>svg]:px-1.5 [&_svg:not([class*='size-'])]:size-3",
        sm: "h-8 gap-1.5 rounded-md px-3 has-[>svg]:px-2.5",
        lg: "h-10 rounded-md px-6 has-[>svg]:px-4",
        icon: "size-9",
        "icon-xs": "size-6 rounded-md [&_svg:not([class*='size-'])]:size-3",
        "icon-sm": "size-8",
        "icon-lg": "size-10",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
