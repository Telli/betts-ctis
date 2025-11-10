"use client"

import React, { useEffect, useMemo, useState } from "react"
import { Button } from "@/components/ui/button"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { ChevronsUpDown, Check } from "lucide-react"
import { cn } from "@/lib/utils"
import { ClientService, type ClientDto } from "@/lib/services"

interface ClientSearchSelectProps {
  value?: string
  onChange: (value: string) => void
  placeholder?: string
  disabled?: boolean
  className?: string
  allowEmpty?: boolean
  emptyLabel?: string
}

export default function ClientSearchSelect({ value, onChange, placeholder = "Select client", disabled, className, allowEmpty, emptyLabel = "All Clients" }: ClientSearchSelectProps) {
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [clients, setClients] = useState<ClientDto[]>([])

  useEffect(() => {
    let mounted = true
    const loadClients = async () => {
      try {
        setLoading(true)
        const list = await ClientService.getAll()
        if (mounted) setClients(Array.isArray(list) ? list : [])
      } catch {
        if (mounted) setClients([])
      } finally {
        if (mounted) setLoading(false)
      }
    }
    loadClients()
    return () => { mounted = false }
  }, [])

  const options = useMemo(() => {
    return (clients || []).filter(c => !!c.clientId).map(c => ({
      label: (c.businessName || [c.firstName, c.lastName].filter(Boolean).join(" ") || "Unnamed Client") + (c.clientNumber ? ` (${c.clientNumber})` : ""),
      value: String(c.clientId),
    }))
  }, [clients])

  const selected = useMemo(() => options.find(o => o.value === (value ?? ""))?.label, [options, value])

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className={cn("w-full justify-between", className)}
          disabled={disabled}
        >
          <span className={cn("truncate text-left", !selected && "text-muted-foreground")}>{selected || placeholder}</span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0" align="start">
        <Command>
          <CommandInput placeholder="Search clients..." />
          <CommandList>
            <CommandEmpty>{loading ? "Loading clients..." : "No clients found."}</CommandEmpty>
            <CommandGroup heading="Clients">
              {allowEmpty && (
                <CommandItem
                  key="__all__"
                  value={emptyLabel}
                  onSelect={() => {
                    onChange("")
                    setOpen(false)
                  }}
                >
                  <Check className={cn("mr-2 h-4 w-4", value === "" ? "opacity-100" : "opacity-0")} />
                  <span className="truncate">{emptyLabel}</span>
                </CommandItem>
              )}
              {options.map(opt => (
                <CommandItem
                  key={opt.value}
                  value={opt.label}
                  onSelect={() => {
                    onChange(opt.value)
                    setOpen(false)
                  }}
                >
                  <Check className={cn("mr-2 h-4 w-4", opt.value === value ? "opacity-100" : "opacity-0")} />
                  <span className="truncate">{opt.label}</span>
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
