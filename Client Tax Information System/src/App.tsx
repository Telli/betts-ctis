import { useEffect, useState } from "react";
import { Login } from "./components/Login";
import { Layout } from "./components/Layout";
import { Dashboard } from "./components/Dashboard";
import { ClientList } from "./components/ClientList";
import { FilingWorkspace } from "./components/FilingWorkspace";
import { Documents } from "./components/Documents";
import { Payments } from "./components/Payments";
import { Compliance } from "./components/Compliance";
import { KPIs } from "./components/KPIs";
import { Reports } from "./components/Reports";
import { Chat } from "./components/Chat";
import { Admin } from "./components/Admin";
import { UserInfo, getCurrentUser, logout as performLogout } from "./lib/auth";

type ViewType = "dashboard" | "clients" | "filings" | "documents" | "payments" | "compliance" | "kpis" | "reports" | "chat" | "admin";

export default function App() {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [isCheckingSession, setIsCheckingSession] = useState(true);
  const [currentView, setCurrentView] = useState<ViewType>("dashboard");
  const [impersonating] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function initialize() {
      try {
        const result = await getCurrentUser();
        if (!cancelled && result) {
          setUser(result);
        }
      } catch {
        if (!cancelled) {
          setUser(null);
        }
      } finally {
        if (!cancelled) {
          setIsCheckingSession(false);
        }
      }
    }

    initialize();
    return () => {
      cancelled = true;
    };
  }, []);

  const handleLogin = (info: UserInfo) => {
    setUser(info);
    setCurrentView("dashboard");
  };

  const handleLogout = async () => {
    try {
      await performLogout();
    } finally {
      setUser(null);
      setCurrentView("dashboard");
    }
  };

  if (isCheckingSession) {
    return (
      <div className="w-full h-screen flex items-center justify-center text-muted-foreground">
        Checking session...
      </div>
    );
  }

  if (!user) {
    return <Login onLogin={handleLogin} />;
  }

  const renderView = () => {
    switch (currentView) {
      case "dashboard":
        return <Dashboard userRole={user.role.toLowerCase() === "client" ? "client" : "staff"} clientId={user.clientId} />;
      case "clients":
        return <ClientList />;
      case "filings":
        return <FilingWorkspace clientId={user.clientId} />;
      case "documents":
        return <Documents clientId={user.clientId} />;
      case "payments":
        return <Payments clientId={user.clientId} />;
      case "compliance":
        return <Compliance clientId={user.clientId} />;
      case "kpis":
        return <KPIs clientId={user.clientId} userRole={user.role} />;
      case "reports":
        return <Reports clientId={user.clientId} />;
      case "chat":
        return <Chat clientId={user.clientId} />;
      case "admin":
        return <Admin />;
      default:
        return <Dashboard userRole={user.role.toLowerCase() === "client" ? "client" : "staff"} clientId={user.clientId} />;
    }
  };

  return (
    <Layout 
      userRole={user.role.toLowerCase() === "client" ? "client" : "staff"}
      user={user}
      impersonating={impersonating}
      activeView={currentView}
      onLogout={handleLogout}
      onNavigate={(view) => setCurrentView(view as ViewType)}
    >
      {renderView()}
    </Layout>
  );
}