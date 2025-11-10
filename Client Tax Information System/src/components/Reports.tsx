import { useEffect, useMemo, useState } from "react";
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
import { FileText, Download, Eye, CalendarIcon, FileSpreadsheet } from "lucide-react";
import { Alert, AlertDescription } from "./ui/alert";
import { fetchReportFilters, fetchReportTypes, FilterOption, ReportType } from "../lib/services/reports";

interface ReportsProps {
  clientId?: number | null;
}

const iconMap: Record<string, typeof FileText> = {
  FileText,
};

function resolveIcon(iconKey?: string) {
  if (!iconKey) return FileText;
  return iconMap[iconKey] ?? FileText;
}

export function Reports({ clientId }: ReportsProps) {
  const [selectedReport, setSelectedReport] = useState<string>("");
  const [dateFrom, setDateFrom] = useState<Date>();
  const [dateTo, setDateTo] = useState<Date>();
  const [clientFilter, setClientFilter] = useState("all");
  const [taxTypeFilter, setTaxTypeFilter] = useState("all");
  const [reportTypes, setReportTypes] = useState<ReportType[]>([]);
  const [clientOptions, setClientOptions] = useState<FilterOption[]>([{ value: "all", label: "All Clients" }]);
  const [taxTypeOptions, setTaxTypeOptions] = useState<FilterOption[]>([{ value: "all", label: "All Types" }]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadResources() {
      setIsLoading(true);
      try {
        const [types, filters] = await Promise.all([fetchReportTypes(), fetchReportFilters()]);
        if (!cancelled) {
          setReportTypes(types);
          const clientOpts =
            filters.clients.length > 0
              ? filters.clients
              : [{ value: "all", label: "All Clients" }];
          const taxOpts =
            filters.taxTypes.length > 0
              ? filters.taxTypes
              : [{ value: "all", label: "All Types" }];
          setClientOptions(
            clientOpts.some((option) => option.value === "all")
              ? clientOpts
              : [{ value: "all", label: "All Clients" }, ...clientOpts],
          );
          setTaxTypeOptions(
            taxOpts.some((option) => option.value === "all")
              ? taxOpts
              : [{ value: "all", label: "All Types" }, ...taxOpts],
          );
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load report options.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadResources();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (clientId) {
      const match = clientOptions.find((option) => option.value === String(clientId));
      if (match) {
        setClientFilter(match.value);
      }
    }
  }, [clientId, clientOptions]);

  const filteredReportTypes = useMemo(() => reportTypes, [reportTypes]);

  return (
    <div>
      <PageHeader
        title="Reports"
        breadcrumbs={[{ label: "Reports" }]}
      />

        <div className="p-6">
          {error && (
            <Alert variant="destructive" className="mb-6">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading report options...</p>
          ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Report Selection */}
          <div className="lg:col-span-1">
            <Card>
              <CardHeader>
                <CardTitle>Select Report</CardTitle>
              </CardHeader>
                <CardContent className="space-y-2">
                  {filteredReportTypes.length === 0 ? (
                    <p className="text-sm text-muted-foreground">No reports available.</p>
                  ) : (
                    filteredReportTypes.map((report) => {
                      const Icon = resolveIcon(report.iconKey);
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
                    })
                  )}
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
                      <Select value={clientFilter} onValueChange={setClientFilter}>
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {clientOptions.map((option) => (
                            <SelectItem key={option.value} value={option.value}>
                              {option.label}
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
                          {taxTypeOptions.map((option) => (
                            <SelectItem key={option.value} value={option.value}>
                              {option.label}
                            </SelectItem>
                          ))}
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
          )}
        </div>
      </div>
    </div>
  );
}
