"use client"

import { useState, useCallback } from "react"
import { Search, X } from "lucide-react"
import { Input } from "./input"
import { Button } from "./button"
import { cn } from "@/lib/utils"

interface SearchInputProps {
  onSearch: (query: string) => void
  placeholder?: string
  debounceMs?: number
  className?: string
  value?: string
  showClearButton?: boolean
}

export function SearchInput({
  onSearch,
  placeholder = "Search...",
  debounceMs = 300,
  className,
  value: controlledValue,
  showClearButton = true
}: SearchInputProps) {
  const [internalValue, setInternalValue] = useState(controlledValue || "")
  const value = controlledValue !== undefined ? controlledValue : internalValue

  // Debounced search function
  const debouncedSearch = useCallback(
    debounce((query: string) => onSearch(query), debounceMs),
    [onSearch, debounceMs]
  )

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value
    if (controlledValue === undefined) {
      setInternalValue(newValue)
    }
    debouncedSearch(newValue)
  }

  const handleClear = () => {
    if (controlledValue === undefined) {
      setInternalValue("")
    }
    onSearch("")
  }

  return (
    <div className={cn("relative", className)}>
      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
      <Input
        type="text"
        placeholder={placeholder}
        value={value}
        onChange={handleChange}
        className="pl-10 pr-10"
      />
      {showClearButton && value && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="absolute right-1 top-1/2 transform -translate-y-1/2 h-6 w-6 p-0"
          onClick={handleClear}
        >
          <X className="h-3 w-3" />
        </Button>
      )}
    </div>
  )
}

// Utility function for debouncing
function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout
  return (...args: Parameters<T>) => {
    clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }
}