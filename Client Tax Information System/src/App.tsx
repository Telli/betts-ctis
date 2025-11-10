import { useState, lazy, Suspense } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Login } from "./components/Login";
import { Layout } from "./components/Layout";
import { Dashboard } from "./components/Dashboard";
import { Loader2 } from "lucide-react";

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
  const [impersonating] = useState<string | null>(null);

  const handleLogin = (role: "client" | "staff") => {
    setUserRole(role);
    setIsLoggedIn(true);
  };

  if (!isLoggedIn) {
    return <Login onLogin={handleLogin} />;
  }

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
      onNavigate={(view) => setCurrentView(view as ViewType)}
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