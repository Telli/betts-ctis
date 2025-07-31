"use client"

import { useState } from "react"
import Link from "next/link"
import { usePathname, useRouter } from "next/navigation"
import { useAuth } from "@/context/auth-context"
import { useNavigationCounts } from "@/hooks/use-navigation-counts"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import {
  Home,
  Users,
  FileText,
  CreditCard,
  Calendar,
  BarChart3,
  Settings,
  HelpCircle,
  ChevronLeft,
  ChevronRight,
  Building2,
  Calculator,
  Upload,
  Bell,
  Shield,
  UserCog,
} from "lucide-react"

export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const pathname = usePathname()
  const router = useRouter()
  const { isLoggedIn, logout, user } = useAuth()
  const { counts, loading } = useNavigationCounts()
  
  const handleLogout = () => {
    logout();
    router.push('/login');
  }

  const navigation = [
    {
      name: "Dashboard",
      href: "/dashboard",
      icon: BarChart3,
      current: pathname === "/dashboard",
    },
    {
      name: "Home",
      href: "/",
      icon: Home,
      current: pathname === "/",
    },
    {
      name: "Clients",
      href: "/clients",
      icon: Users,
      current: pathname === "/clients",
      badge: counts?.totalClients,
    },
    {
      name: "Tax Filings",
      href: "/tax-filings",
      icon: FileText,
      current: pathname === "/tax-filings",
      badge: counts?.totalTaxFilings,
    },
    {
      name: "Payments",
      href: "/payments",
      icon: CreditCard,
      current: pathname === "/payments",
    },
    {
      name: "Compliance",
      href: "/compliance",
      icon: Building2,
      current: pathname === "/compliance",
    },
    {
      name: "Tax Calculator",
      href: "/calculator",
      icon: Calculator,
      current: pathname === "/calculator",
    },
    {
      name: "Documents",
      href: "/documents",
      icon: Upload,
      current: pathname === "/documents",
    },
    {
      name: "Deadlines",
      href: "/deadlines",
      icon: Calendar,
      current: pathname === "/deadlines",
      badge: counts?.upcomingDeadlines,
    },
    {
      name: "Reports",
      href: "/reports",
      icon: FileText,
      current: pathname === "/reports",
    },
    {
      name: "Analytics",
      href: "/analytics",
      icon: BarChart3,
      current: pathname === "/analytics",
    },
    {
      name: "Notifications",
      href: "/notifications",
      icon: Bell,
      current: pathname === "/notifications",
      badge: counts?.unreadNotifications,
    },
  ]

  // Admin navigation (only for admin and system admin users)
  const adminNavigation = [
    {
      name: "Associate Management",
      href: "/admin/associates",
      icon: UserCog,
      current: pathname.startsWith("/admin/associates"),
    },
    {
      name: "Admin Settings",
      href: "/admin/settings",
      icon: Shield,
      current: pathname.startsWith("/admin/settings"),
    },
  ];

  // Associate navigation (only for associate users)
  const associateNavigation = [
    {
      name: "Associate Dashboard",
      href: "/associate/dashboard",
      icon: BarChart3,
      current: pathname.startsWith("/associate/dashboard"),
    },
    {
      name: "My Permissions",
      href: "/associate/permissions",
      icon: Shield,
      current: pathname.startsWith("/associate/permissions"),
    },
    {
      name: "My Clients",
      href: "/associate/clients",
      icon: Users,
      current: pathname.startsWith("/associate/clients"),
    },
  ];

  const isAdmin = user?.role === 'Admin' || user?.role === 'SystemAdmin';
  const isAssociate = user?.role === 'Associate';

  const bottomNavigation = [
    {
      name: "Settings",
      href: "/settings",
      icon: Settings,
      current: pathname === "/settings",
    },
    {
      name: "Profile",
      href: "/profile",
      icon: Users,
      current: pathname === "/profile",
    },
    {
      name: "Help & Support",
      href: "/help",
      icon: HelpCircle,
      current: pathname === "/help",
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
                  <span className="text-white font-bold text-sm">TB</span>
                </div>
                <div>
                  <h2 className="font-bold text-sierra-blue-800">CTIS</h2>
                  <p className="text-xs text-sierra-blue-500">Sierra Leone</p>
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
                  <>
                    <span className="font-medium">{item.name}</span>
                    {item.badge !== undefined && item.badge !== null && (
                      <Badge className="ml-auto bg-sierra-blue-100 text-sierra-blue-800 hover:bg-sierra-blue-200">
                        {loading ? '...' : item.badge}
                      </Badge>
                    )}
                  </>
                )}
              </div>
            </Link>
          ))}
        </nav>

        {/* Admin Navigation */}
        {isAdmin && (
          <div className="px-4 py-2">
            <div className="border-t border-gray-200 pt-4">
              <div className="flex items-center space-x-2 mb-3">
                <Shield className="h-4 w-4 text-gray-500" />
                {!collapsed && (
                  <span className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                    Admin
                  </span>
                )}
              </div>
              <div className="space-y-1">
                {adminNavigation.map((item) => (
                  <Link key={item.name} href={item.href}>
                    <div
                      className={`flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors ${
                        item.current 
                          ? "bg-red-50 text-red-700 border border-red-200" 
                          : "text-gray-700 hover:bg-red-25 hover:text-red-600"
                      }`}
                    >
                      <item.icon className="h-5 w-5 flex-shrink-0" />
                      {!collapsed && <span className="font-medium">{item.name}</span>}
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Associate Navigation */}
        {isAssociate && (
          <div className="px-4 py-2">
            <div className="border-t border-gray-200 pt-4">
              <div className="flex items-center space-x-2 mb-3">
                <UserCog className="h-4 w-4 text-gray-500" />
                {!collapsed && (
                  <span className="text-sm font-semibold text-gray-600 uppercase tracking-wide">
                    Associate
                  </span>
                )}
              </div>
              <div className="space-y-1">
                {associateNavigation.map((item) => (
                  <Link key={item.name} href={item.href}>
                    <div
                      className={`flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors ${
                        item.current 
                          ? "bg-blue-50 text-blue-700 border border-blue-200" 
                          : "text-gray-700 hover:bg-blue-25 hover:text-blue-600"
                      }`}
                    >
                      <item.icon className="h-5 w-5 flex-shrink-0" />
                      {!collapsed && <span className="font-medium">{item.name}</span>}
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Sierra Leone Info */}
        {!collapsed && (
          <div className="p-4 border-t border-gray-200">
            <div className="bg-gradient-to-r from-green-50 to-blue-50 p-3 rounded-lg border border-green-200">
              <div className="flex items-center space-x-2 mb-2">
                <span className="text-lg">🇸🇱</span>
                <span className="font-semibold text-green-800">Sierra Leone</span>
              </div>
              <p className="text-xs text-green-700">Finance Act 2025 Compliant</p>
              <p className="text-xs text-green-600 mt-1">NRA Integration Ready</p>
            </div>
          </div>
        )}

        {/* Bottom Navigation */}
        <div className="p-4 border-t border-gray-200 space-y-2">
          {bottomNavigation.map((item) => (
            <Link key={item.name} href={item.href}>
              <div
                className={`flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors ${
                  item.current ? "bg-blue-50 text-blue-700 border border-blue-200" : "text-gray-700 hover:bg-gray-50"
                }`}
              >
                <item.icon className="h-5 w-5 flex-shrink-0" />
                {!collapsed && <span className="font-medium">{item.name}</span>}
              </div>
            </Link>
          ))}
          
          {isLoggedIn && (
            <div
              onClick={handleLogout}
              className="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors text-red-600 hover:bg-red-50 cursor-pointer"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                className="h-5 w-5 flex-shrink-0"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                />
              </svg>
              {!collapsed && <span className="font-medium">Logout</span>}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
