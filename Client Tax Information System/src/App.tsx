import { useState } from "react";
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

type ViewType = "dashboard" | "clients" | "filings" | "documents" | "payments" | "compliance" | "kpis" | "reports" | "chat" | "admin";

export default function App() {
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
      {renderView()}
    </Layout>
  );
}