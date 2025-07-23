import { DashboardHeader } from "@/components/dashboard-header"
import { ClientForm } from "@/components/client-form"

export default function NewClientPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50">
      <DashboardHeader />
      <main className="container mx-auto px-4 py-6">
        <h1 className="text-2xl font-bold mb-6">Add New Client</h1>
        <ClientForm />
      </main>
    </div>
  )
}
