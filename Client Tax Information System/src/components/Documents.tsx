import { useEffect, useMemo, useState } from "react";
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
import { Alert, AlertDescription } from "./ui/alert";
import { DocumentRecord, fetchDocuments } from "../lib/services/documents";

interface DocumentsProps {
  clientId?: number | null;
}

export function Documents({ clientId }: DocumentsProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [viewMode, setViewMode] = useState<"grid" | "table">("grid");
  const [typeFilter, setTypeFilter] = useState("all");
  const [yearFilter, setYearFilter] = useState("all");
  const [documents, setDocuments] = useState<DocumentRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadDocuments() {
      setIsLoading(true);
      try {
        const data = await fetchDocuments(clientId ?? undefined);
        if (!cancelled) {
          setDocuments(data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load documents.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadDocuments();
    return () => {
      cancelled = true;
    };
  }, [clientId]);

  const uniqueTypes = useMemo(() => {
    const types = new Set<string>();
    documents.forEach((doc) => {
      if (doc.type) {
        types.add(doc.type);
      }
    });
    return Array.from(types).sort();
  }, [documents]);

  const uniqueYears = useMemo(() => {
    const years = new Set<number>();
    documents.forEach((doc) => {
      if (typeof doc.year === "number") {
        years.add(doc.year);
      }
    });
    return Array.from(years).sort((a, b) => b - a);
  }, [documents]);

  const filteredDocuments = useMemo(() => {
    return documents.filter((doc) => {
      const matchesSearch = doc.name.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesType = typeFilter === "all" || doc.type === typeFilter;
      const matchesYear = yearFilter === "all" || doc.year?.toString() === yearFilter;
      return matchesSearch && matchesType && matchesYear;
    });
  }, [documents, searchTerm, typeFilter, yearFilter]);

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
        {error && (
          <Alert variant="destructive" className="mb-6">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

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
                {uniqueTypes.map((type) => (
                  <SelectItem key={type} value={type}>
                    {type}
                  </SelectItem>
                ))}
            </SelectContent>
          </Select>
          <Select value={yearFilter} onValueChange={setYearFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Year" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Years</SelectItem>
                {uniqueYears.map((year) => (
                  <SelectItem key={year} value={String(year)}>
                    {year}
                  </SelectItem>
                ))}
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
          <>
            {isLoading ? (
              <p className="text-sm text-muted-foreground">Loading documents...</p>
            ) : filteredDocuments.length === 0 ? (
              <p className="text-sm text-muted-foreground">No documents match the selected filters.</p>
            ) : (
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
                        <p>{doc.type || "Not available"}</p>
                        <p>{doc.client || "Not available"}</p>
                        <p>Year: {doc.year ?? 0}</p>
                        <p className="font-mono text-xs">v{doc.version ?? 1}</p>
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
                        <p>Uploaded {doc.uploadDate ? new Date(doc.uploadDate).toLocaleDateString() : "Not available"}</p>
                        <p>by {doc.uploadedBy || "Not available"}</p>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}
          </>
        )}

        {/* Table View */}
        {viewMode === "table" && (
          <>
            {isLoading ? (
              <p className="text-sm text-muted-foreground">Loading documents...</p>
            ) : filteredDocuments.length === 0 ? (
              <p className="text-sm text-muted-foreground">No documents match the selected filters.</p>
            ) : (
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
                        <td className="p-4">{doc.type || "Not available"}</td>
                        <td className="p-4">{doc.client || "Not available"}</td>
                        <td className="p-4">{doc.year ?? "Not available"}</td>
                        <td className="p-4">
                          <Badge variant="outline">v{doc.version ?? 1}</Badge>
                        </td>
                        <td className="p-4">{getStatusBadge(doc.status)}</td>
                        <td className="p-4 text-sm text-muted-foreground">
                          <p>{doc.uploadDate ? new Date(doc.uploadDate).toLocaleDateString() : "Not available"}</p>
                          <p className="text-xs">by {doc.uploadedBy || "Not available"}</p>
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
          </>
        )}
      </div>
    </div>
  );
}
