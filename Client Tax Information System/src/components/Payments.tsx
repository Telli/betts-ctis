import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Badge } from "./ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "./ui/dialog";
import { Label } from "./ui/label";
import { Search, Plus, Receipt, DollarSign, CheckCircle, Clock } from "lucide-react";
import { Textarea } from "./ui/textarea";

const mockPayments = [
  {
    id: 1,
    client: "ABC Corporation",
    taxType: "GST",
    period: "Q3 2025",
    amount: 22500,
    method: "Bank Transfer",
    status: "Paid",
    date: "2025-10-01",
    receiptNo: "RCP-2025-001",
  },
  {
    id: 2,
    client: "XYZ Trading",
    taxType: "Income Tax",
    period: "2024",
    amount: 150000,
    method: "Cheque",
    status: "Pending",
    date: "2025-09-28",
    receiptNo: "RCP-2025-002",
  },
  {
    id: 3,
    client: "Tech Solutions",
    taxType: "PAYE",
    period: "Sep 2025",
    amount: 45000,
    method: "Cash",
    status: "Paid",
    date: "2025-09-25",
    receiptNo: "RCP-2025-003",
  },
  {
    id: 4,
    client: "Global Imports",
    taxType: "Excise Duty",
    period: "Q3 2025",
    amount: 78000,
    method: "Bank Transfer",
    status: "Overdue",
    date: "2025-09-15",
    receiptNo: "RCP-2025-004",
  },
];

export function Payments() {
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [taxTypeFilter, setTaxTypeFilter] = useState("all");
  const [isRecordDialogOpen, setIsRecordDialogOpen] = useState(false);

  const filteredPayments = mockPayments.filter((payment) => {
    const matchesSearch =
      payment.client.toLowerCase().includes(searchTerm.toLowerCase()) ||
      payment.receiptNo.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === "all" || payment.status === statusFilter;
    const matchesTaxType = taxTypeFilter === "all" || payment.taxType === taxTypeFilter;
    return matchesSearch && matchesStatus && matchesTaxType;
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Paid":
        return <Badge className="bg-success">Paid</Badge>;
      case "Pending":
        return <Badge className="bg-warning">Pending</Badge>;
      case "Overdue":
        return <Badge variant="destructive">Overdue</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  const totalPaid = filteredPayments
    .filter((p) => p.status === "Paid")
    .reduce((sum, p) => sum + p.amount, 0);
  const totalPending = filteredPayments
    .filter((p) => p.status === "Pending")
    .reduce((sum, p) => sum + p.amount, 0);
  const totalOverdue = filteredPayments
    .filter((p) => p.status === "Overdue")
    .reduce((sum, p) => sum + p.amount, 0);

  return (
    <div>
      <PageHeader
        title="Payments"
        breadcrumbs={[{ label: "Payments" }]}
        actions={
          <Dialog open={isRecordDialogOpen} onOpenChange={setIsRecordDialogOpen}>
            <DialogTrigger asChild>
              <Button>
                <Plus className="w-4 h-4 mr-2" />
                Record Payment
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-2xl">
              <DialogHeader>
                <DialogTitle>Record Payment</DialogTitle>
                <DialogDescription>
                  Record a new tax payment received from a client
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Client</Label>
                    <Select>
                      <SelectTrigger>
                        <SelectValue placeholder="Select client" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="abc">ABC Corporation</SelectItem>
                        <SelectItem value="xyz">XYZ Trading</SelectItem>
                        <SelectItem value="tech">Tech Solutions</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label>Tax Type</Label>
                    <Select>
                      <SelectTrigger>
                        <SelectValue placeholder="Select tax type" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="gst">GST</SelectItem>
                        <SelectItem value="income">Income Tax</SelectItem>
                        <SelectItem value="paye">PAYE</SelectItem>
                        <SelectItem value="excise">Excise Duty</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Amount (SLE)</Label>
                    <Input type="number" placeholder="0.00" />
                  </div>
                  <div className="space-y-2">
                    <Label>Payment Method</Label>
                    <Select>
                      <SelectTrigger>
                        <SelectValue placeholder="Select method" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="cash">Cash</SelectItem>
                        <SelectItem value="cheque">Cheque</SelectItem>
                        <SelectItem value="transfer">Bank Transfer</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>
                <div className="space-y-2">
                  <Label>Period</Label>
                  <Input placeholder="e.g., Q3 2025, Sep 2025" />
                </div>
                <div className="space-y-2">
                  <Label>Receipt Number</Label>
                  <Input placeholder="Auto-generated" disabled />
                </div>
                <div className="space-y-2">
                  <Label>Notes</Label>
                  <Textarea placeholder="Add any notes..." rows={3} />
                </div>
                <div className="space-y-2">
                  <Label>Upload Receipt (Optional)</Label>
                  <Button variant="outline" className="w-full">
                    <Receipt className="w-4 h-4 mr-2" />
                    Upload Receipt Image
                  </Button>
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setIsRecordDialogOpen(false)}>
                  Cancel
                </Button>
                <Button onClick={() => setIsRecordDialogOpen(false)}>
                  Record Payment
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        }
      />

      <div className="p-6">
        {/* Summary Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <Card className="border-t-4 border-t-success">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Total Paid</p>
                  <p className="text-2xl font-semibold">SLE {totalPaid.toLocaleString()}</p>
                </div>
                <CheckCircle className="w-8 h-8 text-success" />
              </div>
            </CardContent>
          </Card>
          <Card className="border-t-4 border-t-warning">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Pending</p>
                  <p className="text-2xl font-semibold">SLE {totalPending.toLocaleString()}</p>
                </div>
                <Clock className="w-8 h-8 text-warning" />
              </div>
            </CardContent>
          </Card>
          <Card className="border-t-4 border-t-destructive">
            <CardContent className="pt-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Overdue</p>
                  <p className="text-2xl font-semibold">SLE {totalOverdue.toLocaleString()}</p>
                </div>
                <DollarSign className="w-8 h-8 text-destructive" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Filters */}
        <div className="flex flex-wrap gap-4 mb-6">
          <div className="relative flex-1 min-w-[300px]">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <Input
              placeholder="Search by client or receipt number..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>
          <Select value={taxTypeFilter} onValueChange={setTaxTypeFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Tax Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Tax Types</SelectItem>
              <SelectItem value="GST">GST</SelectItem>
              <SelectItem value="Income Tax">Income Tax</SelectItem>
              <SelectItem value="PAYE">PAYE</SelectItem>
              <SelectItem value="Excise Duty">Excise Duty</SelectItem>
            </SelectContent>
          </Select>
          <Select value={statusFilter} onValueChange={setStatusFilter}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="Paid">Paid</SelectItem>
              <SelectItem value="Pending">Pending</SelectItem>
              <SelectItem value="Overdue">Overdue</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Table */}
        <div className="border border-border rounded-lg bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Receipt No.</TableHead>
                <TableHead>Client</TableHead>
                <TableHead>Tax Type</TableHead>
                <TableHead>Period</TableHead>
                <TableHead className="text-right">Amount (SLE)</TableHead>
                <TableHead>Payment Method</TableHead>
                <TableHead>Date</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-[100px]">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredPayments.map((payment) => (
                <TableRow key={payment.id}>
                  <TableCell className="font-mono text-sm">{payment.receiptNo}</TableCell>
                  <TableCell className="font-medium">{payment.client}</TableCell>
                  <TableCell>{payment.taxType}</TableCell>
                  <TableCell>{payment.period}</TableCell>
                  <TableCell className="text-right font-mono">
                    {payment.amount.toLocaleString()}
                  </TableCell>
                  <TableCell>{payment.method}</TableCell>
                  <TableCell>{payment.date}</TableCell>
                  <TableCell>{getStatusBadge(payment.status)}</TableCell>
                  <TableCell>
                    <Button variant="ghost" size="sm">
                      <Receipt className="w-4 h-4 mr-1" />
                      View
                    </Button>
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
