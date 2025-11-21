"use client";

import * as React from "react";
import ReactDatePicker, { registerLocale } from "react-datepicker";
import { enGB } from "date-fns/locale";
import "react-datepicker/dist/react-datepicker.css";

import { cn } from "@/lib/utils";
import { Calendar as CalendarIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

registerLocale("en-GB", enGB);

export type DatePickerProps = {
  value: Date | null;
  onChange: (date: Date | null) => void;
  placeholder?: string;
  className?: string;
  minDate?: Date;
  maxDate?: Date;
  disabled?: boolean;
};

export function DatePicker({
  value,
  onChange,
  placeholder = "Pick a date",
  className,
  minDate,
  maxDate,
  disabled,
}: DatePickerProps) {
  const [open, setOpen] = React.useState(false);

  return (
    <div className={cn("relative", className)}>
      <Button
        type="button"
        variant="outline"
        className={cn(
          "w-full justify-start text-left font-normal h-9",
          !value && "text-muted-foreground"
        )}
        onClick={() => setOpen((o) => !o)}
        disabled={disabled}
      >
        <CalendarIcon className="mr-2 h-4 w-4" />
        {value ? value.toLocaleDateString() : placeholder}
      </Button>

      {open ? (
        <div className="absolute z-50 mt-2 bg-background border rounded-md shadow-md p-1 min-w-[16rem]">
          <ReactDatePicker
            selected={value}
            onChange={(d: Date | null) => {
              onChange(d);
              setOpen(false);
            }}
            onClickOutside={() => setOpen(false)}
            inline
            locale="en-GB"
            minDate={minDate}
            maxDate={maxDate}
            calendarClassName="!p-2 !bg-transparent"
          />
        </div>
      ) : null}
    </div>
  );
}
