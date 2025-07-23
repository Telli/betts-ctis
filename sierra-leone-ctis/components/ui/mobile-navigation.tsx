"use client"

import * as React from "react"
import { Menu, X, Home, FileText, User, Settings, LogOut } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "./button"
import { Sheet, SheetContent, SheetTrigger, SheetHeader, SheetTitle } from "./sheet"
import { useIsMobile } from "./use-mobile"

interface MobileNavigationProps {
  children: React.ReactNode
  title?: string
  className?: string
}

export function MobileNavigation({ 
  children, 
  title = "Navigation",
  className 
}: MobileNavigationProps) {
  const [open, setOpen] = React.useState(false)
  const isMobile = useIsMobile()

  if (!isMobile) {
    return null
  }

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          className={cn("md:hidden", className)}
          data-testid="mobile-menu-button"
        >
          <Menu className="h-6 w-6" />
          <span className="sr-only">Toggle navigation menu</span>
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-80">
        <SheetHeader>
          <SheetTitle>{title}</SheetTitle>
        </SheetHeader>
        <div className="mt-6" onClick={() => setOpen(false)}>
          {children}
        </div>
      </SheetContent>
    </Sheet>
  )
}

interface MobileNavItemProps {
  href: string
  icon: React.ReactNode
  children: React.ReactNode
  active?: boolean
  onClick?: () => void
}

export function MobileNavItem({ 
  href, 
  icon, 
  children, 
  active = false,
  onClick 
}: MobileNavItemProps) {
  return (
    <a
      href={href}
      onClick={onClick}
      className={cn(
        "flex items-center gap-3 px-3 py-2 text-sm font-medium rounded-md transition-colors",
        "hover:bg-accent hover:text-accent-foreground",
        "focus:bg-accent focus:text-accent-foreground focus:outline-none",
        active && "bg-accent text-accent-foreground"
      )}
    >
      {icon}
      {children}
    </a>
  )
}

// Client Portal Mobile Navigation
interface ClientPortalMobileNavProps {
  currentPath?: string
  onLogout?: () => void
}

export function ClientPortalMobileNav({ 
  currentPath, 
  onLogout 
}: ClientPortalMobileNavProps) {
  const isMobile = useIsMobile()

  if (!isMobile) {
    return null
  }

  const navItems = [
    {
      href: "/client-portal/dashboard",
      icon: <Home className="h-4 w-4" />,
      label: "Dashboard",
      path: "/client-portal/dashboard"
    },
    {
      href: "/client-portal/documents",
      icon: <FileText className="h-4 w-4" />,
      label: "My Documents",
      path: "/client-portal/documents"
    },
    {
      href: "/client-portal/profile",
      icon: <User className="h-4 w-4" />,
      label: "Profile",
      path: "/client-portal/profile"
    },
    {
      href: "/client-portal/tax-filings",
      icon: <FileText className="h-4 w-4" />,
      label: "Tax Filings",
      path: "/client-portal/tax-filings"
    },
    {
      href: "/client-portal/payments",
      icon: <Settings className="h-4 w-4" />,
      label: "Payment History",
      path: "/client-portal/payments"
    },
    {
      href: "/client-portal/compliance",
      icon: <Settings className="h-4 w-4" />,
      label: "Compliance Status",
      path: "/client-portal/compliance"
    }
  ]

  return (
    <MobileNavigation title="Client Portal" className="lg:hidden">
      <nav className="space-y-1" data-testid="client-sidebar">
        {navItems.map((item) => (
          <MobileNavItem
            key={item.path}
            href={item.href}
            icon={item.icon}
            active={currentPath === item.path}
          >
            {item.label}
          </MobileNavItem>
        ))}
        <div className="border-t pt-4 mt-4">
          <MobileNavItem
            href="#"
            icon={<LogOut className="h-4 w-4" />}
            onClick={onLogout}
          >
            Logout
          </MobileNavItem>
        </div>
      </nav>
    </MobileNavigation>
  )
}

// Admin Mobile Navigation
interface AdminMobileNavProps {
  currentPath?: string
  onLogout?: () => void
}

export function AdminMobileNav({ 
  currentPath, 
  onLogout 
}: AdminMobileNavProps) {
  const isMobile = useIsMobile()

  if (!isMobile) {
    return null
  }

  const navItems = [
    {
      href: "/dashboard",
      icon: <Home className="h-4 w-4" />,
      label: "Dashboard",
      path: "/dashboard"
    },
    {
      href: "/dashboard/clients",
      icon: <User className="h-4 w-4" />,
      label: "Clients",
      path: "/dashboard/clients"
    },
    {
      href: "/dashboard/documents",
      icon: <FileText className="h-4 w-4" />,
      label: "Documents",
      path: "/dashboard/documents"
    },
    {
      href: "/dashboard/reports",
      icon: <Settings className="h-4 w-4" />,
      label: "Reports",
      path: "/dashboard/reports"
    },
    {
      href: "/dashboard/settings",
      icon: <Settings className="h-4 w-4" />,
      label: "Settings",
      path: "/dashboard/settings"
    }
  ]

  return (
    <MobileNavigation title="Admin Dashboard" className="lg:hidden">
      <nav className="space-y-1" data-testid="admin-sidebar">
        {navItems.map((item) => (
          <MobileNavItem
            key={item.path}
            href={item.href}
            icon={item.icon}
            active={currentPath === item.path}
          >
            {item.label}
          </MobileNavItem>
        ))}
        <div className="border-t pt-4 mt-4">
          <MobileNavItem
            href="#"
            icon={<LogOut className="h-4 w-4" />}
            onClick={onLogout}
          >
            Logout
          </MobileNavItem>
        </div>
      </nav>
    </MobileNavigation>
  )
}