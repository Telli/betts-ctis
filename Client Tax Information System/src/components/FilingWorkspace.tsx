import { useState } from "react";
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

export function FilingWorkspace() {
  const [activeTab, setActiveTab] = useState("form");
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  const scheduleData = [
    { id: 1, description: "Sales Revenue", amount: 250000, taxable: 250000 },
    { id: 2, description: "Cost of Goods Sold", amount: 150000, taxable: 0 },
    { id: 3, description: "Operating Expenses", amount: 50000, taxable: 0 },
  ];

  const documents = [
    { id: 1, name: "Financial Statements 2024", version: 2, uploadedBy: "John Doe", date: "2025-10-01" },
    { id: 2, name: "Bank Statements", version: 1, uploadedBy: "Jane Smith", date: "2025-09-28" },
    { id: 3, name: "Sales Records", version: 3, uploadedBy: "John Doe", date: "2025-10-02" },
  ];

  const history = [
    { date: "2025-10-05 14:30", user: "John Doe", action: "Updated form data", changes: "Revenue figures" },
    { date: "2025-10-04 10:15", user: "Jane Smith", action: "Uploaded document", changes: "Financial Statements v2" },
    { date: "2025-10-03 16:45", user: "John Doe", action: "Created filing", changes: "GST Return Q3 2025" },
  ];

  return (
    <div>
      <PageHeader
        title="GST Return - Q3 2025"
        breadcrumbs={[
          { label: "Filings", href: "#" },
          { label: "GST", href: "#" },
          { label: "Q3 2025" },
        ]}
        actions={
          <div className="flex gap-2">
            <Button variant="outline">
              <Save className="w-4 h-4 mr-2" />
              Save Draft
            </Button>
            <Button>
              <Send className="w-4 h-4 mr-2" />
              Submit Filing
            </Button>
          </div>
        }
      />

      <div className="p-6">
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
                    <Select defaultValue="q3-2025">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="q3-2025">Q3 2025 (Jul-Sep)</SelectItem>
                        <SelectItem value="q2-2025">Q2 2025 (Apr-Jun)</SelectItem>
                        <SelectItem value="q1-2025">Q1 2025 (Jan-Mar)</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>Filing Status</Label>
                    <Select defaultValue="draft">
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="draft">Draft</SelectItem>
                        <SelectItem value="pending">Pending Review</SelectItem>
                        <SelectItem value="submitted">Submitted</SelectItem>
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
                    <Input type="number" placeholder="0.00" defaultValue="250000" />
                  </div>
                  <div className="space-y-2">
                    <Label>Taxable Sales (SLE)</Label>
                    <Input type="number" placeholder="0.00" defaultValue="250000" />
                  </div>
                  <div className="space-y-2">
                    <Label>GST Rate (%)</Label>
                    <Input type="number" placeholder="15" defaultValue="15" disabled />
                  </div>
                  <div className="space-y-2">
                    <Label>Output Tax (SLE)</Label>
                    <Input type="number" placeholder="0.00" defaultValue="37500" disabled />
                  </div>
                  <div className="space-y-2">
                    <Label>Input Tax Credit (SLE)</Label>
                    <Input type="number" placeholder="0.00" defaultValue="15000" />
                  </div>
                  <div className="space-y-2">
                    <Label>Net GST Payable (SLE)</Label>
                    <Input
                      type="number"
                      placeholder="0.00"
                      defaultValue="22500"
                      disabled
                      className="font-semibold"
                    />
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
                    defaultValue="All sales figures verified against bank statements and invoices."
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
                      {scheduleData.map((row) => (
                        <TableRow key={row.id}>
                          <TableCell>{row.description}</TableCell>
                          <TableCell className="text-right font-mono">
                            {row.amount.toLocaleString()}
                          </TableCell>
                          <TableCell className="text-right font-mono">
                            {row.taxable.toLocaleString()}
                          </TableCell>
                          <TableCell>
                            <Button variant="ghost" size="sm">
                              Edit
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
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
                    <span className="font-semibold">SLE 250,000</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Taxable Sales</span>
                    <span className="font-semibold">SLE 250,000</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">GST Rate</span>
                    <span className="font-semibold">15%</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Output Tax</span>
                    <span className="font-semibold">SLE 37,500</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Input Tax Credit</span>
                    <span className="font-semibold text-success">- SLE 15,000</span>
                  </div>
                  <div className="flex justify-between items-center py-3 border-b">
                    <span className="text-muted-foreground">Penalties</span>
                    <span className="font-semibold">SLE 0</span>
                  </div>
                  <div className="flex justify-between items-center py-4 bg-primary/5 px-4 rounded-lg">
                    <span className="font-semibold">Total GST Payable</span>
                    <span className="text-xl font-semibold text-primary">SLE 22,500</span>
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
                      {documents.map((doc) => (
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
                          <TableCell>{doc.uploadedBy}</TableCell>
                          <TableCell>{doc.date}</TableCell>
                          <TableCell>
                            <Button variant="ghost" size="sm">
                              View
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
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
                  {history.map((entry, index) => (
                    <div key={index} className="flex gap-4 pb-4 border-b last:border-b-0">
                      <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                      <div className="flex-1">
                        <div className="flex items-start justify-between">
                          <div>
                            <p className="font-medium">{entry.action}</p>
                            <p className="text-sm text-muted-foreground">{entry.changes}</p>
                          </div>
                          <time className="text-sm text-muted-foreground">{entry.date}</time>
                        </div>
                        <p className="text-sm text-muted-foreground mt-1">by {entry.user}</p>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
