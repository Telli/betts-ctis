import { useEffect, useState } from "react";
import { PageHeader } from "./PageHeader";
import { Button } from "./ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Textarea } from "./ui/textarea";
import { Badge } from "./ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "./ui/table";
import { FileText, Upload, Save, Send, AlertCircle, CheckCircle, FileUp } from "lucide-react";
import { Alert, AlertDescription } from "./ui/alert";
import { fetchActiveFiling, FilingWorkspace as FilingWorkspaceData } from "../lib/services/filings";

interface FilingWorkspaceProps {
  clientId?: number | null;
}

export function FilingWorkspace({ clientId }: FilingWorkspaceProps) {
  const [activeTab, setActiveTab] = useState("form");
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [filing, setFiling] = useState<FilingWorkspaceData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadFiling() {
      setIsLoading(true);
      try {
        const data = await fetchActiveFiling();
        if (!cancelled) {
          if (clientId && data.clientId && data.clientId !== clientId) {
            setFiling(null);
            setError("No active filing found for the selected client.");
          } else {
            setFiling(data);
            setError(null);
          }
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load filing workspace.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadFiling();
    return () => {
      cancelled = true;
    };
  }, [clientId]);

  const scheduleData = filing?.schedule ?? [];
  const documents = filing?.supportingDocuments ?? [];
  const history = filing?.history ?? [];

  return (
      <div>
        <PageHeader
          title={filing?.title ?? "Filing Workspace"}
          breadcrumbs={[
            { label: "Filings", href: "#" },
            { label: filing?.taxType ?? "Not available", href: "#" },
            { label: filing?.selectedTaxPeriod ?? "Current" },
          ]}
          actions={
            <div className="flex gap-2">
              <Button variant="outline" disabled={isLoading || !filing}>
                <Save className="w-4 h-4 mr-2" />
                Save Draft
              </Button>
              <Button disabled={isLoading || !filing}>
                <Send className="w-4 h-4 mr-2" />
                Submit Filing
              </Button>
            </div>
          }
        />

        <div className="p-6 space-y-6">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

          {isLoading ? (
            <p className="text-sm text-muted-foreground">Loading filing workspace...</p>
          ) : !filing ? (
            <p className="text-sm text-muted-foreground">No filing data available.</p>
          ) : (
        <Tabs value={activeTab} onValueChange={setActiveTab}>
          <TabsList className="mb-6">
            <TabsTrigger value="form">Form</TabsTrigger>
            <TabsTrigger value="schedules">Schedules</TabsTrigger>
            <TabsTrigger value="assessment">Assessment</TabsTrigger>
            <TabsTrigger value="documents">Documents</TabsTrigger>
            <TabsTrigger value="history">History</TabsTrigger>
          </TabsList>

          {/* Form Tab */}
          <TabsContent value="form" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Basic Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Tax Period</Label>
                      <Select value={filing.selectedTaxPeriod ?? ""} disabled>
                      <SelectTrigger>
                          <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                          {filing.taxPeriodOptions.map((option) => (
                            <SelectItem key={option.value} value={option.value}>
                              {option.label}
                            </SelectItem>
                          ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>Filing Status</Label>
                      <Select value={filing.selectedFilingStatus ?? ""} disabled>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                          {filing.filingStatusOptions.map((option) => (
                            <SelectItem key={option.value} value={option.value}>
                              {option.label}
                            </SelectItem>
                          ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>GST Details</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Total Sales (SLE)</Label>
                      <Input type="number" value={filing.totalSales ?? 0} readOnly />
                  </div>
                  <div className="space-y-2">
                    <Label>Taxable Sales (SLE)</Label>
                      <Input type="number" value={filing.taxableSales ?? 0} readOnly />
                  </div>
                  <div className="space-y-2">
                    <Label>GST Rate (%)</Label>
                      <Input type="number" value={filing.gstRate ?? 0} readOnly />
                  </div>
                  <div className="space-y-2">
                    <Label>Output Tax (SLE)</Label>
                      <Input type="number" value={filing.outputTax ?? 0} readOnly />
                  </div>
                  <div className="space-y-2">
                    <Label>Input Tax Credit (SLE)</Label>
                      <Input type="number" value={filing.inputTaxCredit ?? 0} readOnly />
                  </div>
                  <div className="space-y-2">
                    <Label>Net GST Payable (SLE)</Label>
                      <Input type="number" value={filing.netGstPayable ?? 0} readOnly className="font-semibold" />
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Additional Information</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <Label>Notes / Comments</Label>
                    <Textarea
                      placeholder="Add any additional notes or explanations..."
                      rows={4}
                      value={filing.notes ?? ""}
                      readOnly
                    />
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Schedules Tab */}
          <TabsContent value="schedules" className="space-y-6">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Import Schedule Data</CardTitle>
                  <Button>
                    <Upload className="w-4 h-4 mr-2" />
                    Import CSV/Excel
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <Alert>
                  <AlertCircle className="w-4 h-4" />
                  <AlertDescription>
                    Upload a CSV or Excel file with columns: Description, Amount, Taxable Amount
                  </AlertDescription>
                </Alert>

                <div className="mt-6 border border-border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Description</TableHead>
                        <TableHead className="text-right">Amount (SLE)</TableHead>
                        <TableHead className="text-right">Taxable (SLE)</TableHead>
                        <TableHead className="w-[100px]">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                      <TableBody>
                        {scheduleData.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={4} className="text-sm text-muted-foreground text-center">
                              No schedule data available.
                            </TableCell>
                          </TableRow>
                        ) : (
                          scheduleData.map((row) => (
                            <TableRow key={row.id}>
                              <TableCell>{row.description}</TableCell>
                              <TableCell className="text-right font-mono">
                                {Math.round(row.amount).toLocaleString()}
                              </TableCell>
                              <TableCell className="text-right font-mono">
                                {Math.round(row.taxableAmount).toLocaleString()}
                              </TableCell>
                              <TableCell>
                                <Button variant="ghost" size="sm">
                                  Edit
                                </Button>
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Assessment Tab */}
          <TabsContent value="assessment" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Tax Assessment Summary</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Total Sales</span>
                      <span className="font-semibold">SLE {Math.round(filing.totalSales ?? 0).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Taxable Sales</span>
                      <span className="font-semibold">SLE {Math.round(filing.taxableSales ?? 0).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">GST Rate</span>
                      <span className="font-semibold">{filing.gstRate ?? 0}%</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Output Tax</span>
                      <span className="font-semibold">SLE {Math.round(filing.outputTax ?? 0).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Input Tax Credit</span>
                      <span className="font-semibold text-success">- SLE {Math.round(filing.inputTaxCredit ?? 0).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Penalties</span>
                      <span className="font-semibold">SLE 0</span>
                  </div>
                  <div className="flex justify-between items-center py-4 bg-primary/5 px-4 rounded-lg">
                    <span className="font-semibold">Total GST Payable</span>
                      <span className="text-xl font-semibold text-primary">
                        SLE {Math.round(filing.netGstPayable ?? 0).toLocaleString()}
                      </span>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Alert>
              <CheckCircle className="w-4 h-4 text-success" />
              <AlertDescription>
                No validation errors found. Ready to submit.
              </AlertDescription>
            </Alert>
          </TabsContent>

          {/* Documents Tab */}
          <TabsContent value="documents" className="space-y-6">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Supporting Documents</CardTitle>
                  <Button>
                    <FileUp className="w-4 h-4 mr-2" />
                    Upload Document
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="border border-border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Document Name</TableHead>
                        <TableHead>Version</TableHead>
                        <TableHead>Uploaded By</TableHead>
                        <TableHead>Date</TableHead>
                        <TableHead className="w-[100px]">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                      <TableBody>
                        {documents.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={5} className="text-sm text-muted-foreground text-center">
                              No supporting documents available.
                            </TableCell>
                          </TableRow>
                        ) : (
                          documents.map((doc) => (
                            <TableRow key={doc.id}>
                              <TableCell>
                                <div className="flex items-center gap-2">
                                  <FileText className="w-4 h-4 text-primary" />
                                  {doc.name}
                                </div>
                              </TableCell>
                              <TableCell>
                                <Badge variant="outline">v{doc.version}</Badge>
                              </TableCell>
                              <TableCell>{doc.uploadedBy ?? "Not available"}</TableCell>
                              <TableCell>
                                {doc.uploadedAt ? new Date(doc.uploadedAt).toLocaleDateString() : "Not available"}
                              </TableCell>
                              <TableCell>
                                <Button variant="ghost" size="sm">
                                  View
                                </Button>
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* History Tab */}
          <TabsContent value="history" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Audit Trail</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                    {history.length === 0 ? (
                      <p className="text-sm text-muted-foreground">No history entries available.</p>
                    ) : (
                      history.map((entry, index) => (
                        <div key={`${entry.timestamp}-${index}`} className="flex gap-4 pb-4 border-b last:border-b-0">
                          <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                          <div className="flex-1">
                            <div className="flex items-start justify-between">
                              <div>
                                <p className="font-medium">{entry.action}</p>
                                <p className="text-sm text-muted-foreground">{entry.changes}</p>
                              </div>
                              <time className="text-sm text-muted-foreground">
                                {entry.timestamp ? new Date(entry.timestamp).toLocaleString() : "Not available"}
                              </time>
                            </div>
                            <p className="text-sm text-muted-foreground mt-1">by {entry.user}</p>
                          </div>
                        </div>
                      ))
                    )}
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
          )}
      </div>
    </div>
  );
}
