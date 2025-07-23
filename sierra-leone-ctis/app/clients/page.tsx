import { DashboardHeader } from "@/components/dashboard-header"
import { ClientsTable } from "@/components/clients-table"
import { ClientsHeader } from "@/components/clients-header"

export default function ClientsPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50">
      <DashboardHeader />

      <main className="container mx-auto px-4 py-6">
        <ClientsHeader />
        <ClientsTable />
      </main>
    </div>
  )
}
