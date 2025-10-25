import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Badge } from "./ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "./ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Plus, Search, Eye, UserCog, MoreHorizontal } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "./ui/dropdown-menu";

const mockClients = [
  {
    id: 1,
    name: "ABC Corporation Ltd",
    tin: "1234567890",
    segment: "Corporate",
    industry: "Manufacturing",
    status: "Active",
    complianceScore: 95,
    assignedTo: "Jane Smith",
  },
  {
    id: 2,
    name: "XYZ Trading Company",
    tin: "0987654321",
    segment: "SME",
    industry: "Retail",
    status: "Active",
    complianceScore: 87,
    assignedTo: "John Doe",
  },
  {
    id: 3,
    name: "Tech Solutions Inc",
    tin: "1122334455",
    segment: "Corporate",
    industry: "Technology",
    status: "Active",
    complianceScore: 92,
    assignedTo: "Sarah Johnson",
  },
  {
    id: 4,
    name: "Global Imports Ltd",
    tin: "5544332211",
    segment: "Large Enterprise",
    industry: "Import/Export",
    status: "Active",
    complianceScore: 78,
    assignedTo: "Mike Brown",
  },
  {
    id: 5,
    name: "Local Cafe Chain",
    tin: "9988776655",
    segment: "SME",
    industry: "Food & Beverage",
    status: "At Risk",
    complianceScore: 65,
    assignedTo: "Jane Smith",
  },
];

export function ClientList() {
  const [searchTerm, setSearchTerm] = useState("");
  const [segmentFilter, setSegmentFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");

  const filteredClients = mockClients.filter((client) => {
    const matchesSearch =
      client.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      client.tin.includes(searchTerm);
    const matchesSegment = segmentFilter === "all" || client.segment === segmentFilter;
    const matchesStatus = statusFilter === "all" || client.status === statusFilter;
    return matchesSearch && matchesSegment && matchesStatus;
  });

  const getComplianceBadge = (score: number) => {
    if (score >= 90) return <Badge className="bg-success">Excellent</Badge>;
    if (score >= 75) return <Badge className="bg-info">Good</Badge>;
    if (score >= 60) return <Badge className="bg-warning">Fair</Badge>;
    return <Badge variant="destructive">At Risk</Badge>;
  };

  return (
    <div>
      <PageHeader
        title="Clients"
        breadcrumbs={[{ label: "Clients" }]}
        actions={
          <Button>
            <Plus className="w-4 h-4 mr-2" />
            Add Client
          </Button>
        }
      />

      <div className="p-6">
        {/* Filters */}
        <div className="flex flex-wrap gap-4 mb-6">
          <div className="relative flex-1 min-w-[300px]">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="Search by name or TIN..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>
          <Select value={segmentFilter} onValueChange={setSegmentFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Segment" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Segments</SelectItem>
              <SelectItem value="Corporate">Corporate</SelectItem>
              <SelectItem value="SME">SME</SelectItem>
              <SelectItem value="Large Enterprise">Large Enterprise</SelectItem>
            </SelectContent>
          </Select>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="At Risk">At Risk</SelectItem>
              <SelectItem value="Inactive">Inactive</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Table */}
        <div className="border border-border rounded-lg bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Client Name</TableHead>
                <TableHead>TIN</TableHead>
                <TableHead>Segment</TableHead>
                <TableHead>Industry</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Compliance</TableHead>
                <TableHead>Assigned To</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredClients.map((client) => (
                <TableRow key={client.id}>
                  <TableCell className="font-medium">{client.name}</TableCell>
                  <TableCell className="font-mono text-sm">{client.tin}</TableCell>
                  <TableCell>{client.segment}</TableCell>
                  <TableCell>{client.industry}</TableCell>
                  <TableCell>
                    <Badge variant={client.status === "Active" ? "default" : "destructive"}>
                      {client.status}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <span className="text-sm">{client.complianceScore}%</span>
                      {getComplianceBadge(client.complianceScore)}
                    </div>
                  </TableCell>
                  <TableCell>{client.assignedTo}</TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="w-4 h-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem>
                          <Eye className="w-4 h-4 mr-2" />
                          View Profile
                        </DropdownMenuItem>
                        <DropdownMenuItem>
                          <UserCog className="w-4 h-4 mr-2" />
                          Impersonate
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </div>
    </div>
  );
}
