'use client';

import React from 'react';
import DatePicker from 'react-datepicker';
import { enGB } from 'date-fns/locale';
import 'react-datepicker/dist/react-datepicker.css';

interface CalendarDisplayProps {
  selected?: Date | null;
  onSelect?: (date: Date | null) => void;
  className?: string;
  highlightDates?: Date[];
  highlightClassName?: string;
}

/**
 * CalendarDisplay - Full month calendar view with date highlighting
 * Used for displaying calendars with marked dates (e.g., deadlines, events)
 * For date input fields, use DatePicker instead
 */
export function CalendarDisplay({
  selected,
  onSelect,
  className = '',
  highlightDates = [],
  highlightClassName = 'highlighted-date',
}: CalendarDisplayProps) {
  const highlightDatesSet = new Set(
    highlightDates.map(d => d.toDateString())
  );

  return (
    <div className={`calendar-display-wrapper ${className}`}>
      <DatePicker
        selected={selected}
        onChange={onSelect}
        inline
        locale={enGB}
        calendarStartDay={1}
        highlightDates={highlightDates}
        dayClassName={(date: Date) => {
          const isHighlighted = highlightDatesSet.has(date.toDateString());
          return isHighlighted ? highlightClassName : undefined;
        }}
        renderDayContents={(day: number, date?: Date) => {
          const isHighlighted = date && highlightDatesSet.has(date.toDateString());
          return (
            <span className={isHighlighted ? 'font-bold' : ''}>
              {day}
            </span>
          );
        }}
      />
      <style jsx global>{`
        .calendar-display-wrapper .react-datepicker {
          border: 1px solid hsl(var(--border));
          border-radius: var(--radius);
          font-family: inherit;
          background-color: hsl(var(--background));
          box-shadow: none;
        }

        .calendar-display-wrapper .react-datepicker__header {
          background-color: hsl(var(--muted));
          border-bottom: 1px solid hsl(var(--border));
          border-top-left-radius: var(--radius);
          border-top-right-radius: var(--radius);
          padding-top: 0.5rem;
        }

        .calendar-display-wrapper .react-datepicker__current-month {
          color: hsl(var(--foreground));
          font-weight: 600;
          font-size: 0.875rem;
          margin-bottom: 0.5rem;
        }

        .calendar-display-wrapper .react-datepicker__day-names {
          display: flex;
          justify-content: space-around;
          margin-bottom: 0.25rem;
        }

        .calendar-display-wrapper .react-datepicker__day-name {
          color: hsl(var(--muted-foreground));
          font-size: 0.75rem;
          font-weight: 500;
          width: 2.25rem;
          line-height: 2.25rem;
          text-align: center;
          margin: 0.125rem;
        }

        .calendar-display-wrapper .react-datepicker__month {
          margin: 0.5rem;
        }

        .calendar-display-wrapper .react-datepicker__week {
          display: flex;
          justify-content: space-around;
        }

        .calendar-display-wrapper .react-datepicker__day {
          color: hsl(var(--foreground));
          width: 2.25rem;
          line-height: 2.25rem;
          text-align: center;
          margin: 0.125rem;
          border-radius: var(--radius);
          font-size: 0.875rem;
          cursor: pointer;
        }

        .calendar-display-wrapper .react-datepicker__day:hover {
          background-color: hsl(var(--accent));
        }

        .calendar-display-wrapper .react-datepicker__day--selected {
          background-color: hsl(var(--primary)) !important;
          color: hsl(var(--primary-foreground)) !important;
          font-weight: 600;
        }

        .calendar-display-wrapper .react-datepicker__day--today {
          background-color: hsl(var(--accent));
          font-weight: 600;
        }

        .calendar-display-wrapper .react-datepicker__day--outside-month {
          color: hsl(var(--muted-foreground));
          opacity: 0.5;
        }

        .calendar-display-wrapper .react-datepicker__day--disabled {
          color: hsl(var(--muted-foreground));
          cursor: not-allowed;
          opacity: 0.5;
        }

        .calendar-display-wrapper .react-datepicker__day--disabled:hover {
          background-color: transparent;
        }

        .calendar-display-wrapper .react-datepicker__navigation {
          top: 0.5rem;
        }

        .calendar-display-wrapper .react-datepicker__navigation-icon::before {
          border-color: hsl(var(--foreground));
        }

        .calendar-display-wrapper .react-datepicker__navigation:hover *::before {
          border-color: hsl(var(--primary));
        }

        /* Highlighted dates (e.g., deadlines) */
        .calendar-display-wrapper .highlighted-date {
          background-color: #fef3c7 !important;
          color: #d97706 !important;
          position: relative;
        }

        .calendar-display-wrapper .highlighted-date::after {
          content: '';
          position: absolute;
          bottom: 2px;
          left: 50%;
          transform: translateX(-50%);
          width: 4px;
          height: 4px;
          border-radius: 50%;
          background-color: #d97706;
        }

        .calendar-display-wrapper .react-datepicker__day--highlighted {
          background-color: #fef3c7 !important;
          color: #d97706 !important;
        }

        .calendar-display-wrapper .react-datepicker__day--highlighted:hover {
          background-color: #fde68a !important;
        }
      `}</style>
    </div>
  );
}
