import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Search, Filter, Download } from "lucide-react"

export function ClientsHeader() {
  return (
    <div className="mb-6">
      <div className="flex items-center justify-between mb-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Client Management</h1>
          <p className="text-gray-600">Manage taxpayer information and compliance status</p>
        </div>
      </div>

      <div className="flex items-center space-x-4 mb-4">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
          <Input placeholder="Search clients by name, TIN, or category..." className="pl-10" />
        </div>
        <Button variant="outline">
          <Filter className="h-4 w-4 mr-2" />
          Filter
        </Button>
        <Button variant="outline">
          <Download className="h-4 w-4 mr-2" />
          Export
        </Button>
      </div>

      <div className="flex items-center space-x-2">
        <Badge variant="outline">Total: 156 clients</Badge>
        <Badge className="bg-green-100 text-green-800">Compliant: 142</Badge>
        <Badge className="bg-amber-100 text-amber-800">Pending: 11</Badge>
        <Badge className="bg-red-100 text-red-800">Overdue: 3</Badge>
      </div>
    </div>
  )
}
