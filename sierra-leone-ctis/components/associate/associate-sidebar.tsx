"use client"

import { useState } from "react"
import Link from "next/link"
import Image from "next/image"
import { usePathname, useRouter } from "next/navigation"
import { useAuth } from "@/context/auth-context"
import { Button } from "@/components/ui/button"
import {
  BarChart3,
  Users,
  FileText,
  CreditCard,
  Calendar,
  Upload,
  User,
  Settings,
  HelpCircle,
  ChevronLeft,
  ChevronRight,
  Shield,
  LogOut,
  CheckCircle,
  MessageCircle
} from "lucide-react"

export function AssociateSidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const pathname = usePathname()
  const router = useRouter()
  const { user, logout } = useAuth()

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  }

  const navigation = [
    {
      name: "Dashboard",
      href: "/associate/dashboard",
      icon: BarChart3,
      current: pathname === "/associate/dashboard",
    },
    {
      name: "My Clients",
      href: "/associate/clients",
      icon: Users,
      current: pathname === "/associate/clients",
    },
    {
      name: "Messages",
      href: "/associate/messages",
      icon: MessageCircle,
      current: pathname === "/associate/messages",
    },
    {
      name: "Document Review",
      href: "/associate/documents",
      icon: CheckCircle,
      current: pathname === "/associate/documents",
    },
    {
      name: "Tax Filings",
      href: "/associate/tax-filings",
      icon: FileText,
      current: pathname === "/associate/tax-filings",
    },
    {
      name: "Payments",
      href: "/associate/payments",
      icon: CreditCard,
      current: pathname === "/associate/payments",
    },
    {
      name: "Compliance",
      href: "/associate/compliance",
      icon: Shield,
      current: pathname === "/associate/compliance",
    },
    {
      name: "Deadlines",
      href: "/associate/deadlines",
      icon: Calendar,
      current: pathname === "/associate/deadlines",
    },
  ]

  const bottomNavigation = [
    {
      name: "Permissions",
      href: "/associate/permissions",
      icon: Shield,
      current: pathname === "/associate/permissions",
    },
    {
      name: "Profile",
      href: "/associate/profile",
      icon: User,
      current: pathname === "/associate/profile",
    },
    {
      name: "Settings",
      href: "/associate/settings",
      icon: Settings,
      current: pathname === "/associate/settings",
    },
    {
      name: "Help & Support",
      href: "/associate/help",
      icon: HelpCircle,
      current: pathname === "/associate/help",
    },
  ]

  return (
    <div
      className={`bg-white border-r border-sierra-orange-100 shadow-sierra transition-all duration-300 ${
        collapsed ? "w-16" : "w-64"
      }`}
    >
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-sierra-orange-100">
        {!collapsed && (
          <div className="flex items-center space-x-2">
            <Image src="/logo.png" alt="Betts logo" width={32} height={32} className="rounded" />
            <div>
              <h1 className="text-lg font-bold text-sierra-orange-800">Betts Tax</h1>
              <p className="text-xs text-sierra-orange-600">Associate Portal</p>
            </div>
          </div>
        )}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setCollapsed(!collapsed)}
          className="text-sierra-orange-600 hover:bg-sierra-orange-50"
        >
          {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
        </Button>
      </div>

      {/* User Info */}
      {!collapsed && user && (
        <div className="p-4 border-b border-sierra-orange-100">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-sierra-orange-100 rounded-full flex items-center justify-center">
              <User className="h-4 w-4 text-sierra-orange-600" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-gray-900 truncate">
                {user.name}
              </p>
              <p className="text-xs text-gray-500 truncate">{user.email}</p>
            </div>
          </div>
        </div>
      )}

      {/* Navigation */}
      <nav className="flex-1 px-2 py-4 space-y-1">
        {navigation.map((item) => (
          <Link
            key={item.name}
            href={item.href}
            className={`group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors ${
              item.current
                ? "bg-sierra-orange-100 text-sierra-orange-800 border-r-2 border-sierra-orange-600"
                : "text-gray-700 hover:bg-sierra-orange-50 hover:text-sierra-orange-700"
            } ${collapsed ? "justify-center" : ""}`}
          >
            <item.icon
              className={`flex-shrink-0 h-5 w-5 ${
                item.current ? "text-sierra-orange-600" : "text-gray-400 group-hover:text-sierra-orange-600"
              } ${collapsed ? "" : "mr-3"}`}
            />
            {!collapsed && <span>{item.name}</span>}
          </Link>
        ))}
      </nav>

      {/* Bottom Navigation */}
      <div className="px-2 py-4 border-t border-sierra-orange-100 space-y-1">
        {bottomNavigation.map((item) => (
          <Link
            key={item.name}
            href={item.href}
            className={`group flex items-center px-3 py-2 text-sm font-medium rounded-md transition-colors ${
              item.current
                ? "bg-sierra-orange-100 text-sierra-orange-800 border-r-2 border-sierra-orange-600"
                : "text-gray-700 hover:bg-sierra-orange-50 hover:text-sierra-orange-700"
            } ${collapsed ? "justify-center" : ""}`}
          >
            <item.icon
              className={`flex-shrink-0 h-5 w-5 ${
                item.current ? "text-sierra-orange-600" : "text-gray-400 group-hover:text-sierra-orange-600"
              } ${collapsed ? "" : "mr-3"}`}
            />
            {!collapsed && <span>{item.name}</span>}
          </Link>
        ))}

        {/* Logout Button */}
        <Button
          variant="ghost"
          onClick={handleLogout}
          className={`w-full justify-start px-3 py-2 text-sm font-medium text-gray-700 hover:bg-red-50 hover:text-red-700 ${
            collapsed ? "justify-center px-2" : ""
          }`}
        >
          <LogOut className={`flex-shrink-0 h-5 w-5 ${collapsed ? "" : "mr-3"}`} />
          {!collapsed && <span>Logout</span>}
        </Button>
      </div>
    </div>
  )
}