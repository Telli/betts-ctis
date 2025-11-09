import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Badge } from "./ui/badge";
import { Card, CardContent } from "./ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Search, Upload, FileText, Grid3x3, List, Download, Eye } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";

const mockDocuments = [
  {
    id: 1,
    name: "Financial Statements 2024.pdf",
    type: "Financial Statement",
    client: "ABC Corporation",
    year: 2024,
    taxType: "Income Tax",
    version: 2,
    uploadedBy: "John Doe",
    uploadDate: "2025-10-01",
    hash: "a3b2c1d4e5f6...",
    status: "verified",
  },
  {
    id: 2,
    name: "Bank Statements Q3.pdf",
    type: "Bank Statement",
    client: "XYZ Trading",
    year: 2025,
    taxType: "GST",
    version: 1,
    uploadedBy: "Jane Smith",
    uploadDate: "2025-09-28",
    hash: "f6e5d4c3b2a1...",
    status: "scanning",
  },
  {
    id: 3,
    name: "Sales Records Sept.xlsx",
    type: "Sales Record",
    client: "Tech Solutions",
    year: 2025,
    taxType: "GST",
    version: 3,
    uploadedBy: "Mike Brown",
    uploadDate: "2025-10-02",
    hash: "b4c5d6e7f8a9...",
    status: "verified",
  },
  {
    id: 4,
    name: "Payroll Summary Q3.pdf",
    type: "Payroll Record",
    client: "ABC Corporation",
    year: 2025,
    taxType: "PAYE",
    version: 1,
    uploadedBy: "Sarah Johnson",
    uploadDate: "2025-09-25",
    hash: "c5d6e7f8a9b0...",
    status: "verified",
  },
];

export function Documents() {
  const [searchTerm, setSearchTerm] = useState("");
  const [viewMode, setViewMode] = useState<"grid" | "table">("grid");
  const [typeFilter, setTypeFilter] = useState("all");
  const [yearFilter, setYearFilter] = useState("all");

  const filteredDocuments = mockDocuments.filter((doc) => {
    const matchesSearch = doc.name.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesType = typeFilter === "all" || doc.type === typeFilter;
    const matchesYear = yearFilter === "all" || doc.year.toString() === yearFilter;
    return matchesSearch && matchesType && matchesYear;
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "verified":
        return <Badge className="bg-success">Verified</Badge>;
      case "scanning":
        return <Badge className="bg-warning">Scanning</Badge>;
      case "blocked":
        return <Badge variant="destructive">Blocked</Badge>;
      default:
        return <Badge variant="outline">Pending</Badge>;
    }
  };

  return (
    <div>
      <PageHeader
        title="Documents"
        breadcrumbs={[{ label: "Documents" }]}
        actions={
          <Button>
            <Upload className="w-4 h-4 mr-2" />
            Upload Document
          </Button>
        }
      />

      <div className="p-6">
        {/* Filters */}
        <div className="flex flex-wrap gap-4 mb-6">
          <div className="relative flex-1 min-w-[300px]">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="Search documents..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>
          <Select value={typeFilter} onValueChange={setTypeFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Types</SelectItem>
              <SelectItem value="Financial Statement">Financial Statement</SelectItem>
              <SelectItem value="Bank Statement">Bank Statement</SelectItem>
              <SelectItem value="Sales Record">Sales Record</SelectItem>
              <SelectItem value="Payroll Record">Payroll Record</SelectItem>
            </SelectContent>
          </Select>
          <Select value={yearFilter} onValueChange={setYearFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Year" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Years</SelectItem>
              <SelectItem value="2025">2025</SelectItem>
              <SelectItem value="2024">2024</SelectItem>
              <SelectItem value="2023">2023</SelectItem>
            </SelectContent>
          </Select>
          <div className="flex border border-border rounded-lg">
            <Button
              variant={viewMode === "grid" ? "default" : "ghost"}
              size="icon"
              onClick={() => setViewMode("grid")}
            >
              <Grid3x3 className="w-4 h-4" />
            </Button>
            <Button
              variant={viewMode === "table" ? "default" : "ghost"}
              size="icon"
              onClick={() => setViewMode("table")}
            >
              <List className="w-4 h-4" />
            </Button>
          </div>
        </div>

        {/* Grid View */}
        {viewMode === "grid" && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {filteredDocuments.map((doc) => (
              <Card key={doc.id} className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4">
                  <div className="flex items-start justify-between mb-3">
                    <FileText className="w-8 h-8 text-primary" />
                    {getStatusBadge(doc.status)}
                  </div>
                  <h4 className="font-medium mb-2 truncate" title={doc.name}>
                    {doc.name}
                  </h4>
                  <div className="space-y-1 text-sm text-muted-foreground mb-3">
                    <p>{doc.type}</p>
                    <p>{doc.client}</p>
                    <p>Year: {doc.year}</p>
                    <p className="font-mono text-xs">v{doc.version}</p>
                  </div>
                  <div className="flex gap-2">
                    <Button size="sm" variant="outline" className="flex-1">
                      <Eye className="w-3 h-3 mr-1" />
                      View
                    </Button>
                    <Button size="sm" variant="outline" className="flex-1">
                      <Download className="w-3 h-3 mr-1" />
                      Download
                    </Button>
                  </div>
                  <div className="mt-3 pt-3 border-t border-border text-xs text-muted-foreground">
                    <p>Uploaded {doc.uploadDate}</p>
                    <p>by {doc.uploadedBy}</p>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}

        {/* Table View */}
        {viewMode === "table" && (
          <div className="border border-border rounded-lg bg-card overflow-x-auto">
            <table className="w-full">
              <thead className="border-b border-border">
                <tr>
                  <th className="text-left p-4 font-medium">Document Name</th>
                  <th className="text-left p-4 font-medium">Type</th>
                  <th className="text-left p-4 font-medium">Client</th>
                  <th className="text-left p-4 font-medium">Year</th>
                  <th className="text-left p-4 font-medium">Version</th>
                  <th className="text-left p-4 font-medium">Status</th>
                  <th className="text-left p-4 font-medium">Uploaded</th>
                  <th className="text-left p-4 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredDocuments.map((doc) => (
                  <tr key={doc.id} className="border-b border-border last:border-b-0 hover:bg-accent/50">
                    <td className="p-4">
                      <div className="flex items-center gap-2">
                        <FileText className="w-4 h-4 text-primary" />
                        <span className="font-medium">{doc.name}</span>
                      </div>
                    </td>
                    <td className="p-4">{doc.type}</td>
                    <td className="p-4">{doc.client}</td>
                    <td className="p-4">{doc.year}</td>
                    <td className="p-4">
                      <Badge variant="outline">v{doc.version}</Badge>
                    </td>
                    <td className="p-4">{getStatusBadge(doc.status)}</td>
                    <td className="p-4 text-sm text-muted-foreground">
                      <p>{doc.uploadDate}</p>
                      <p className="text-xs">by {doc.uploadedBy}</p>
                    </td>
                    <td className="p-4">
                      <div className="flex gap-2">
                        <Button size="sm" variant="ghost">
                          <Eye className="w-3 h-3" />
                        </Button>
                        <Button size="sm" variant="ghost">
                          <Download className="w-3 h-3" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
