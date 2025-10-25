import { useState } from "react";
import {
  LayoutDashboard,
  Users,
  FileText,
  FolderOpen,
  CreditCard,
  CheckSquare,
  BarChart3,
  FileBarChart,
  MessageSquare,
  Settings,
  Bell,
  Search,
  User,
  ChevronDown,
  X,
} from "lucide-react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "./ui/dropdown-menu";
import { Badge } from "./ui/badge";
import logo from "figma:asset/c09e3416d3f18d5dd7594d245d067b31f50605af.png";

interface LayoutProps {
  children: React.ReactNode;
  userRole?: "client" | "staff";
  impersonating?: string | null;
  activeView?: string;
  onNavigate?: (view: string) => void;
}

export function Layout({ children, userRole = "staff", impersonating = null, activeView = "dashboard", onNavigate }: LayoutProps) {
  const [activeNav, setActiveNav] = useState(activeView);

  const staffNavItems = [
    { id: "dashboard", icon: LayoutDashboard, label: "Dashboard" },
    { id: "clients", icon: Users, label: "Clients" },
    { id: "filings", icon: FileText, label: "Filings" },
    { id: "documents", icon: FolderOpen, label: "Documents" },
    { id: "payments", icon: CreditCard, label: "Payments" },
    { id: "compliance", icon: CheckSquare, label: "Compliance" },
    { id: "kpis", icon: BarChart3, label: "KPIs" },
    { id: "reports", icon: FileBarChart, label: "Reports" },
    { id: "chat", icon: MessageSquare, label: "Chat" },
    { id: "admin", icon: Settings, label: "Admin" },
  ];

  const clientNavItems = [
    { id: "dashboard", icon: LayoutDashboard, label: "Dashboard" },
    { id: "filings", icon: FileText, label: "My Filings" },
    { id: "documents", icon: FolderOpen, label: "Documents" },
    { id: "payments", icon: CreditCard, label: "Payments" },
    { id: "compliance", icon: CheckSquare, label: "Compliance" },
    { id: "chat", icon: MessageSquare, label: "Messages" },
  ];

  const navItems = userRole === "staff" ? staffNavItems : clientNavItems;

  return (
    <div className="flex h-screen bg-background">
      {/* Sidebar */}
      <aside className="w-64 border-r border-border bg-sidebar flex flex-col">
        {/* Logo */}
        <div className="h-16 flex items-center px-6 border-b border-sidebar-border">
          <img src={logo} alt="The Betts Firm" className="h-8" />
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto py-4">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeNav === item.id;
            return (
              <button
                key={item.id}
                onClick={() => {
                  setActiveNav(item.id);
                  onNavigate?.(item.id);
                }}
                className={`w-full flex items-center gap-3 px-6 py-3 transition-colors ${
                  isActive
                    ? "bg-sidebar-accent text-sidebar-accent-foreground border-l-4 border-sidebar-primary"
                    : "text-sidebar-foreground hover:bg-sidebar-accent/50"
                }`}
              >
                <Icon className="w-5 h-5" />
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Impersonation Banner */}
        {impersonating && (
          <div className="bg-warning text-warning-foreground px-6 py-2 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <User className="w-4 h-4" />
              <span>Acting as {impersonating}</span>
            </div>
            <Button variant="ghost" size="sm" className="text-warning-foreground hover:bg-warning/80">
              <X className="w-4 h-4 mr-1" />
              Exit
            </Button>
          </div>
        )}

        {/* Top Bar */}
        <header className="h-16 border-b border-border bg-card px-6 flex items-center justify-between">
          {/* Search */}
          <div className="flex-1 max-w-md">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <Input
                placeholder="Search clients, filings..."
                className="pl-10 bg-input-background"
              />
            </div>
          </div>

          {/* Right Side */}
          <div className="flex items-center gap-4">
            {/* Notifications */}
            <Button variant="ghost" size="icon" className="relative">
              <Bell className="w-5 h-5" />
              <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 bg-destructive">
                3
              </Badge>
            </Button>

            {/* User Menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="gap-2">
                  <div className="w-8 h-8 rounded-full bg-primary text-primary-foreground flex items-center justify-center">
                    <User className="w-4 h-4" />
                  </div>
                  <span>John Smith</span>
                  <ChevronDown className="w-4 h-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel>My Account</DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem>Profile</DropdownMenuItem>
                <DropdownMenuItem>Settings</DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem>Logout</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto bg-background">
          {children}
        </main>
      </div>
    </div>
  );
}
