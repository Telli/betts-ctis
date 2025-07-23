"use client"

import { useState } from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { useAuth } from "@/context/auth-context"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  BarChart3,
  FileText,
  CreditCard,
  Calendar,
  Upload,
  User,
  Settings,
  HelpCircle,
  ChevronLeft,
  ChevronRight,
  Building2,
  Shield,
  LogOut
} from "lucide-react"

export function ClientSidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const pathname = usePathname()
  const router = useRouter()
  const { user, logout } = useAuth()
  
  const handleLogout = () => {
    logout();
    router.push('/login');
  }

  const navigation = [
    {
      name: "Dashboard",
      href: "/client-portal/dashboard",
      icon: BarChart3,
      current: pathname === "/client-portal/dashboard",
    },
    {
      name: "My Documents",
      href: "/client-portal/documents",
      icon: Upload,
      current: pathname === "/client-portal/documents",
    },
    {
      name: "Tax Filings",
      href: "/client-portal/tax-filings",
      icon: FileText,
      current: pathname === "/client-portal/tax-filings",
    },
    {
      name: "Payment History",
      href: "/client-portal/payments",
      icon: CreditCard,
      current: pathname === "/client-portal/payments",
    },
    {
      name: "Compliance Status",
      href: "/client-portal/compliance",
      icon: Shield,
      current: pathname === "/client-portal/compliance",
    },
    {
      name: "Deadlines",
      href: "/client-portal/deadlines",
      icon: Calendar,
      current: pathname === "/client-portal/deadlines",
    },
  ]

  const bottomNavigation = [
    {
      name: "Profile",
      href: "/client-portal/profile",
      icon: User,
      current: pathname === "/client-portal/profile",
    },
    {
      name: "Settings",
      href: "/client-portal/settings",
      icon: Settings,
      current: pathname === "/client-portal/settings",
    },
    {
      name: "Help & Support",
      href: "/client-portal/help",
      icon: HelpCircle,
      current: pathname === "/client-portal/help",
    },
  ]

  return (
    <div
      className={`bg-white border-r border-sierra-blue-100 shadow-sierra transition-all duration-300 ${
        collapsed ? "w-16" : "w-64"
      }`}
    >
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="p-4 border-b border-sierra-blue-100">
          <div className="flex items-center justify-between">
            {!collapsed && (
              <div className="flex items-center space-x-3">
                <div className="w-8 h-8 bg-gradient-to-br from-sierra-blue-600 to-sierra-blue-700 rounded-lg flex items-center justify-center">
                  <Building2 className="h-4 w-4 text-white" />
                </div>
                <div>
                  <h2 className="font-bold text-sierra-blue-800">Client Portal</h2>
                  <p className="text-xs text-sierra-blue-500 truncate">
                    {user?.name || 'Client User'}
                  </p>
                </div>
              </div>
            )}
            <Button 
              variant="ghost" 
              size="sm" 
              onClick={() => setCollapsed(!collapsed)} 
              className="p-1 hover:bg-sierra-blue-50"
            >
              {collapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
            </Button>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 p-4 space-y-2">
          {navigation.map((item) => (
            <Link key={item.name} href={item.href}>
              <div
                className={`flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors ${
                  item.current 
                    ? "bg-sierra-blue-50 text-sierra-blue-700 border border-sierra-blue-200" 
                    : "text-gray-700 hover:bg-sierra-blue-25 hover:text-sierra-blue-600"
                }`}
              >
                <item.icon className="h-5 w-5 flex-shrink-0" />
                {!collapsed && (
                  <span className="font-medium">{item.name}</span>
                )}
              </div>
            </Link>
          ))}
        </nav>

        {/* Client Info */}
        {!collapsed && (
          <div className="p-4 border-t border-gray-200">
            <div className="bg-gradient-to-r from-sierra-blue-50 to-sierra-green-50 p-3 rounded-lg border border-sierra-blue-200">
              <div className="flex items-center space-x-2 mb-2">
                <span className="text-lg">🇸🇱</span>
                <span className="font-semibold text-sierra-blue-800">Sierra Leone CTIS</span>
              </div>
              <p className="text-xs text-sierra-blue-700">Secure Client Portal</p>
              <p className="text-xs text-sierra-green-600 mt-1">Finance Act 2025 Compliant</p>
            </div>
          </div>
        )}

        {/* Bottom Navigation */}
        <div className="p-4 border-t border-gray-200 space-y-2">
          {bottomNavigation.map((item) => (
            <Link key={item.name} href={item.href}>
              <div
                className={`flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors ${
                  item.current 
                    ? "bg-sierra-blue-50 text-sierra-blue-700 border border-sierra-blue-200" 
                    : "text-gray-700 hover:bg-sierra-blue-25 hover:text-sierra-blue-600"
                }`}
              >
                <item.icon className="h-5 w-5 flex-shrink-0" />
                {!collapsed && <span className="font-medium">{item.name}</span>}
              </div>
            </Link>
          ))}
          
          <div
            onClick={handleLogout}
            className="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors text-red-600 hover:bg-red-50 cursor-pointer"
          >
            <LogOut className="h-5 w-5 flex-shrink-0" />
            {!collapsed && <span className="font-medium">Logout</span>}
          </div>
        </div>
      </div>
    </div>
  )
}