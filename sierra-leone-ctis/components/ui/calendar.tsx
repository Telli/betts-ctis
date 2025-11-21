"use client"

/**
 * @deprecated This Calendar wrapper (react-day-picker) is deprecated.
 * Migrate to the new DatePicker component at `components/ui/date-picker`.
 * The new DatePicker uses react-datepicker with en-GB (Monday-first) locale
 * and provides a simpler single-date selection UX consistent across the app.
 */

import * as React from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { DayPicker } from "react-day-picker"
import { enGB } from "date-fns/locale"

import { cn } from "@/lib/utils"
import { buttonVariants } from "@/components/ui/button"

export type CalendarProps = React.ComponentProps<typeof DayPicker>

function Calendar({
  className,
  classNames,
  showOutsideDays = true,
  ...props
}: CalendarProps) {
  // Warn in development to encourage migration
  React.useEffect(() => {
    if (process.env.NODE_ENV !== "production") {
      // eslint-disable-next-line no-console
      console.warn("[Deprecated] components/ui/calendar: migrate to components/ui/date-picker");
    }
  }, []);
  // Determine if dropdown caption is used to avoid duplicate text labels
  const isDropdownCaption = props.captionLayout?.startsWith("dropdown") ?? false

  // Build a consistent locale with Monday as first day.
  // If a locale is provided via props, respect it but enforce weekStartsOn when missing.
  const baseLocale = (props as any).locale ?? enGB
  const locale = React.useMemo(() => ({
    ...baseLocale,
    options: { ...(baseLocale as any).options, weekStartsOn: 1 },
  }), [baseLocale])

  // Keep classNames minimal to preserve DayPicker's native table layout.
  const mergedClassNames = React.useMemo(() => ({
    months: "flex flex-col sm:flex-row gap-4",
    month: "space-y-4",
    caption: "flex justify-center pt-1 relative items-center",
    caption_label: isDropdownCaption ? "sr-only" : "text-sm font-medium",
    caption_dropdowns: "flex items-center gap-2",
    caption_dropdown: "flex items-center justify-center",
    caption_dropdown_month: "px-2 py-1 text-sm rounded-md border bg-background",
    caption_dropdown_year: "px-2 py-1 text-sm rounded-md border bg-background",
    nav: "space-x-1 flex items-center",
    nav_button: cn(
      buttonVariants({ variant: "outline" }),
      "h-7 w-7 bg-transparent p-0 opacity-50 hover:opacity-100"
    ),
    nav_button_previous: "absolute left-1",
    nav_button_next: "absolute right-1",
    table: "w-full border-collapse",
    // Do NOT override head_row/row/cell display to keep header/body alignment intact
    head_cell: "text-muted-foreground text-[0.8rem] font-normal",
    day: cn(
      buttonVariants({ variant: "ghost" }),
      "h-9 w-9 p-0 font-normal aria-selected:opacity-100"
    ),
    day_range_end: "day-range-end",
    day_selected:
      "bg-primary text-primary-foreground hover:bg-primary hover:text-primary-foreground focus:bg-primary focus:text-primary-foreground",
    day_today: "bg-accent text-accent-foreground",
    day_outside:
      "day-outside text-muted-foreground opacity-50",
    day_disabled: "text-muted-foreground opacity-50",
    day_range_middle:
      "aria-selected:bg-accent aria-selected:text-accent-foreground",
    day_hidden: "invisible",
    ...classNames,
  }), [classNames, isDropdownCaption])

  return (
    <DayPicker
      {...props}
      locale={locale}
      showOutsideDays={showOutsideDays}
      className={cn("p-3", className)}
      classNames={mergedClassNames as any}
      components={{
        Chevron: ({ orientation, ...rest }) =>
          orientation === "left" ? (
            <ChevronLeft className="h-4 w-4" {...rest} />
          ) : (
            <ChevronRight className="h-4 w-4" {...rest} />
          ),
      }}
    />
  )
}
Calendar.displayName = "Calendar"

export { Calendar }
