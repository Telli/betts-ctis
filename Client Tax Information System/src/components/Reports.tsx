import { useState, useEffect } from "react";
import { PageHeader } from "./PageHeader";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Label } from "./ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Calendar } from "./ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "./ui/popover";
import { FileText, Download, Eye, CalendarIcon, FileSpreadsheet, Loader2 } from "lucide-react";
import { fetchClients, type Client } from "../lib/services/clients";

const reportTypes = [
  {
    id: "tax-filing",
    name: "Tax Filing Summary",
    description: "Comprehensive report of all tax filings by period and type",
    icon: FileText,
  },
  {
    id: "payment-history",
    name: "Payment History",
    description: "Detailed history of all tax payments and receipts",
    icon: FileText,
  },
  {
    id: "compliance",
    name: "Compliance Report",
    description: "Client compliance status and deadline adherence",
    icon: FileText,
  },
  {
    id: "document-submission",
    name: "Document Submission",
    description: "Track document submission rates and completeness",
    icon: FileText,
  },
  {
    id: "tax-calendar",
    name: "Tax Calendar",
    description: "Upcoming deadlines and filing requirements",
    icon: FileText,
  },
  {
    id: "revenue-processed",
    name: "Revenue Processed",
    description: "Total revenue processed by tax type and period",
    icon: FileText,
  },
  {
    id: "activity-logs",
    name: "Activity Logs",
    description: "Detailed audit trail of all system activities",
    icon: FileText,
  },
  {
    id: "case-management",
    name: "Case Management",
    description: "Overview of ongoing cases and issues",
    icon: FileText,
  },
];

export function Reports() {
  const [selectedReport, setSelectedReport] = useState<string>("");
  const [dateFrom, setDateFrom] = useState<Date>();
  const [dateTo, setDateTo] = useState<Date>();
  const [clientFilter, setClientFilter] = useState("all");
  const [taxTypeFilter, setTaxTypeFilter] = useState("all");
  const [clients, setClients] = useState<Client[]>([]);
  const [loadingClients, setLoadingClients] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function loadClients() {
      setLoadingClients(true);
      try {
        const data = await fetchClients();
        if (!cancelled) {
          setClients(data);
        }
      } catch (err) {
        console.error("Failed to load clients:", err);
      } finally {
        if (!cancelled) {
          setLoadingClients(false);
        }
      }
    }

    loadClients();
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div>
      <PageHeader
        title="Reports"
        breadcrumbs={[{ label: "Reports" }]}
      />

      <div className="p-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Report Selection */}
          <div className="lg:col-span-1">
            <Card>
              <CardHeader>
                <CardTitle>Select Report</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {reportTypes.map((report) => {
                  const Icon = report.icon;
                  return (
                    <button
                      key={report.id}
                      onClick={() => setSelectedReport(report.id)}
                      className={`w-full text-left p-3 rounded-lg border transition-colors ${
                        selectedReport === report.id
                          ? "border-primary bg-primary/5"
                          : "border-border hover:bg-accent"
                      }`}
                    >
                      <div className="flex items-start gap-3">
                        <Icon className="w-5 h-5 text-primary mt-0.5" />
                        <div>
                          <p className="font-medium">{report.name}</p>
                          <p className="text-xs text-muted-foreground mt-1">
                            {report.description}
                          </p>
                        </div>
                      </div>
                    </button>
                  );
                })}
              </CardContent>
            </Card>
          </div>

          {/* Parameters & Preview */}
          <div className="lg:col-span-2 space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Report Parameters</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Date From</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button variant="outline" className="w-full justify-start">
                          <CalendarIcon className="mr-2 w-4 h-4" />
                          {dateFrom ? dateFrom.toLocaleDateString() : <span>Pick a date</span>}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={dateFrom}
                          onSelect={setDateFrom}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                  <div className="space-y-2">
                    <Label>Date To</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button variant="outline" className="w-full justify-start">
                          <CalendarIcon className="mr-2 w-4 h-4" />
                          {dateTo ? dateTo.toLocaleDateString() : <span>Pick a date</span>}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={dateTo}
                          onSelect={setDateTo}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Client</Label>
                    <Select value={clientFilter} onValueChange={setClientFilter} disabled={loadingClients}>
                      <SelectTrigger>
                        <SelectValue>
                          {loadingClients ? "Loading..." : clientFilter === "all" ? "All Clients" : clients.find(c => c.id.toString() === clientFilter)?.name}
                        </SelectValue>
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Clients</SelectItem>
                        {clients.map((client) => (
                          <SelectItem key={client.id} value={client.id.toString()}>
                            {client.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>Tax Type</Label>
                    <Select value={taxTypeFilter} onValueChange={setTaxTypeFilter}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Types</SelectItem>
                        <SelectItem value="gst">GST</SelectItem>
                        <SelectItem value="income">Income Tax</SelectItem>
                        <SelectItem value="paye">PAYE</SelectItem>
                        <SelectItem value="excise">Excise Duty</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="flex gap-3 pt-4">
                  <Button className="flex-1" disabled={!selectedReport}>
                    <Eye className="w-4 h-4 mr-2" />
                    Preview Report
                  </Button>
                  <Button variant="outline" disabled={!selectedReport}>
                    <Download className="w-4 h-4 mr-2" />
                    Export PDF
                  </Button>
                  <Button variant="outline" disabled={!selectedReport}>
                    <FileSpreadsheet className="w-4 h-4 mr-2" />
                    Export Excel
                  </Button>
                </div>
              </CardContent>
            </Card>

            {selectedReport && (
              <Card>
                <CardHeader>
                  <CardTitle>Report Preview</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="border border-border rounded-lg p-8 bg-muted/20 min-h-[400px] flex items-center justify-center">
                    <div className="text-center text-muted-foreground">
                      <FileText className="w-16 h-16 mx-auto mb-4 text-primary/50" />
                      <p>Report preview will appear here</p>
                      <p className="text-sm mt-2">
                        Click "Preview Report" to generate the report with selected parameters
                      </p>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
