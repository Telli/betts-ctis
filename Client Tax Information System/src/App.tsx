import { useState, lazy, Suspense, useEffect } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Login } from "./components/Login";
import { Layout } from "./components/Layout";
import { Dashboard } from "./components/Dashboard";
import { Loader2 } from "lucide-react";
import { getUser, getCurrentUser, type UserInfo } from "./lib/auth";

// Lazy load components for code splitting and better performance
const ClientList = lazy(() => import("./components/ClientList").then(m => ({ default: m.ClientList })));
const FilingWorkspace = lazy(() => import("./components/FilingWorkspace").then(m => ({ default: m.FilingWorkspace })));
const Documents = lazy(() => import("./components/Documents").then(m => ({ default: m.Documents })));
const Payments = lazy(() => import("./components/Payments").then(m => ({ default: m.Payments })));
const Compliance = lazy(() => import("./components/Compliance").then(m => ({ default: m.Compliance })));
const KPIs = lazy(() => import("./components/KPIs").then(m => ({ default: m.KPIs })));
const Reports = lazy(() => import("./components/Reports").then(m => ({ default: m.Reports })));
const Chat = lazy(() => import("./components/Chat").then(m => ({ default: m.Chat })));
const Admin = lazy(() => import("./components/Admin").then(m => ({ default: m.Admin })));

// Configure React Query
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // Data stays fresh for 5 minutes
      cacheTime: 10 * 60 * 1000, // Cache for 10 minutes
      retry: 1, // Retry failed requests once
      refetchOnWindowFocus: false, // Don't refetch on window focus
    },
  },
});

type ViewType = "dashboard" | "clients" | "filings" | "documents" | "payments" | "compliance" | "kpis" | "reports" | "chat" | "admin";

// Loading fallback component
function LoadingFallback() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="text-center">
        <Loader2 className="w-8 h-8 animate-spin mx-auto mb-4 text-primary" />
        <p className="text-muted-foreground">Loading...</p>
      </div>
    </div>
  );
}

function AppContent() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [currentView, setCurrentView] = useState<ViewType>("dashboard");
  const [userRole, setUserRole] = useState<"client" | "staff">("staff");
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [impersonating] = useState<string | null>(null);

  // Check authentication status on mount
  useEffect(() => {
    async function checkAuth() {
      try {
        const user = getUser();
        if (user) {
          setUserInfo(user);
          const role = user.role.toLowerCase() === "client" ? "client" : "staff";
          setUserRole(role);
          setIsLoggedIn(true);
        } else {
          // Try to get current user from API
          const currentUser = await getCurrentUser();
          if (currentUser) {
            setUserInfo(currentUser);
            const role = currentUser.role.toLowerCase() === "client" ? "client" : "staff";
            setUserRole(role);
            setIsLoggedIn(true);
          }
        }
      } catch (error) {
        console.error("Auth check failed:", error);
      } finally {
        setIsLoading(false);
      }
    }
    checkAuth();
  }, []);

  const handleLogin = (role: "client" | "staff") => {
    setUserRole(role);
    setIsLoggedIn(true);
    // Refresh user info after login
    const user = getUser();
    if (user) {
      setUserInfo(user);
    }
  };

  // Redirect client users away from admin-only views
  const handleViewChange = (view: ViewType) => {
    // Admin-only views that clients shouldn't access
    const adminOnlyViews: ViewType[] = ["clients", "admin", "kpis", "reports"];
    
    if (userRole === "client" && adminOnlyViews.includes(view)) {
      // Redirect client to dashboard if trying to access admin view
      setCurrentView("dashboard");
      return;
    }
    
    setCurrentView(view);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <Loader2 className="w-8 h-8 animate-spin mx-auto mb-4 text-primary" />
          <p className="text-muted-foreground">Loading...</p>
        </div>
      </div>
    );
  }

  if (!isLoggedIn) {
    return <Login onLogin={handleLogin} />;
  }

  // Ensure client users start on dashboard, not admin views
  useEffect(() => {
    const adminOnlyViews: ViewType[] = ["clients", "admin", "kpis", "reports"];
    if (userRole === "client" && adminOnlyViews.includes(currentView)) {
      setCurrentView("dashboard");
    }
  }, [userRole, currentView]);

  const renderView = () => {
    switch (currentView) {
      case "dashboard":
        return <Dashboard userRole={userRole} />;
      case "clients":
        return <ClientList />;
      case "filings":
        return <FilingWorkspace />;
      case "documents":
        return <Documents />;
      case "payments":
        return <Payments />;
      case "compliance":
        return <Compliance />;
      case "kpis":
        return <KPIs />;
      case "reports":
        return <Reports />;
      case "chat":
        return <Chat />;
      case "admin":
        return <Admin />;
      default:
        return <Dashboard userRole={userRole} />;
    }
  };

  return (
    <Layout
      userRole={userRole}
      impersonating={impersonating}
      activeView={currentView}
      onNavigate={(view) => handleViewChange(view as ViewType)}
    >
      <Suspense fallback={<LoadingFallback />}>
        {renderView()}
      </Suspense>
    </Layout>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppContent />
    </QueryClientProvider>
  );
}