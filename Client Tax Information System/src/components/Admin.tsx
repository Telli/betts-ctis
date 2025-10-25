import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "./ui/tabs";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "./ui/dialog";
import { Users, Shield, DollarSign, Activity, Plus, Search, Upload } from "lucide-react";

const users = [
  { id: 1, name: "John Doe", email: "john@bettsfirm.com", role: "Admin", status: "Active" },
  { id: 2, name: "Jane Smith", email: "jane@bettsfirm.com", role: "Staff", status: "Active" },
  { id: 3, name: "Mike Brown", email: "mike@bettsfirm.com", role: "Staff", status: "Active" },
  { id: 4, name: "Sarah Johnson", email: "sarah@abc.com", role: "Client", status: "Active" },
];

const auditLogs = [
  {
    id: 1,
    timestamp: "2025-10-07 14:30:00",
    actor: "John Doe",
    role: "Admin",
    actingFor: null,
    action: "Updated GST filing for ABC Corporation",
    ip: "192.168.1.100",
  },
  {
    id: 2,
    timestamp: "2025-10-07 12:15:00",
    actor: "Jane Smith",
    role: "Staff",
    actingFor: "XYZ Trading",
    action: "Uploaded financial statements",
    ip: "192.168.1.101",
  },
  {
    id: 3,
    timestamp: "2025-10-07 10:00:00",
    actor: "Mike Brown",
    role: "Staff",
    actingFor: null,
    action: "Generated compliance report",
    ip: "192.168.1.102",
  },
];

const taxRates = [
  { type: "Corporate Income Tax (CIT)", rate: 30, applicableTo: "Companies" },
  { type: "Goods & Services Tax (GST)", rate: 15, applicableTo: "All taxable supplies" },
  { type: "Minimum Alternative Tax (MAT)", rate: 2, applicableTo: "Gross revenue" },
  { type: "PAYE", rate: "Progressive", applicableTo: "Employees" },
];

export function Admin() {
  const [searchTerm, setSearchTerm] = useState("");
  const [isInviteDialogOpen, setIsInviteDialogOpen] = useState(false);

  return (
    <div>
      <PageHeader title="Administration" breadcrumbs={[{ label: "Admin" }]} />

      <div className="p-6">
        <Tabs defaultValue="users">
          <TabsList className="mb-6">
            <TabsTrigger value="users">
              <Users className="w-4 h-4 mr-2" />
              Users & Roles
            </TabsTrigger>
            <TabsTrigger value="rates">
              <DollarSign className="w-4 h-4 mr-2" />
              Rates & Penalties
            </TabsTrigger>
            <TabsTrigger value="audit">
              <Activity className="w-4 h-4 mr-2" />
              Audit Log
            </TabsTrigger>
            <TabsTrigger value="jobs">
              <Shield className="w-4 h-4 mr-2" />
              Jobs Monitor
            </TabsTrigger>
          </TabsList>

          {/* Users & Roles */}
          <TabsContent value="users" className="space-y-6">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>User Management</CardTitle>
                  <Dialog open={isInviteDialogOpen} onOpenChange={setIsInviteDialogOpen}>
                    <DialogTrigger asChild>
                      <Button>
                        <Plus className="w-4 h-4 mr-2" />
                        Invite User
                      </Button>
                    </DialogTrigger>
                    <DialogContent>
                      <DialogHeader>
                        <DialogTitle>Invite New User</DialogTitle>
                        <DialogDescription>
                          Send an invitation to a new user to join the system
                        </DialogDescription>
                      </DialogHeader>
                      <div className="space-y-4 py-4">
                        <div className="space-y-2">
                          <Label>Full Name</Label>
                          <Input placeholder="John Smith" />
                        </div>
                        <div className="space-y-2">
                          <Label>Email Address</Label>
                          <Input type="email" placeholder="john@example.com" />
                        </div>
                        <div className="space-y-2">
                          <Label>Role</Label>
                          <Select>
                            <SelectTrigger>
                              <SelectValue placeholder="Select role" />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="admin">Admin</SelectItem>
                              <SelectItem value="staff">Staff</SelectItem>
                              <SelectItem value="client">Client</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                      </div>
                      <DialogFooter>
                        <Button variant="outline" onClick={() => setIsInviteDialogOpen(false)}>
                          Cancel
                        </Button>
                        <Button onClick={() => setIsInviteDialogOpen(false)}>
                          Send Invitation
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>
              </CardHeader>
              <CardContent>
                <div className="border border-border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Name</TableHead>
                        <TableHead>Email</TableHead>
                        <TableHead>Role</TableHead>
                        <TableHead>Status</TableHead>
                        <TableHead className="w-[100px]">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {users.map((user) => (
                        <TableRow key={user.id}>
                          <TableCell className="font-medium">{user.name}</TableCell>
                          <TableCell>{user.email}</TableCell>
                          <TableCell>
                            <Badge
                              variant={user.role === "Admin" ? "default" : "outline"}
                              className={user.role === "Admin" ? "bg-primary" : ""}
                            >
                              {user.role}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            <Badge className="bg-success">{user.status}</Badge>
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

          {/* Rates & Penalties */}
          <TabsContent value="rates" className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Tax Rate Configuration</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="border border-border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tax Type</TableHead>
                        <TableHead>Rate</TableHead>
                        <TableHead>Applicable To</TableHead>
                        <TableHead className="w-[100px]">Actions</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {taxRates.map((rate, index) => (
                        <TableRow key={index}>
                          <TableCell className="font-medium">{rate.type}</TableCell>
                          <TableCell>
                            {typeof rate.rate === "number" ? `${rate.rate}%` : rate.rate}
                          </TableCell>
                          <TableCell>{rate.applicableTo}</TableCell>
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

            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Penalty Matrix</CardTitle>
                  <Button variant="outline">
                    <Upload className="w-4 h-4 mr-2" />
                    Import Excise Table
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="grid grid-cols-4 gap-4">
                    <div className="space-y-2">
                      <Label>Tax Type</Label>
                      <Select>
                        <SelectTrigger>
                          <SelectValue placeholder="Select" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="gst">GST</SelectItem>
                          <SelectItem value="income">Income Tax</SelectItem>
                          <SelectItem value="paye">PAYE</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label>Condition</Label>
                      <Select>
                        <SelectTrigger>
                          <SelectValue placeholder="Select" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="late">Late Filing</SelectItem>
                          <SelectItem value="non">Non-Filing</SelectItem>
                          <SelectItem value="payment">Payment Delay</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label>Amount (SLE)</Label>
                      <Input type="number" placeholder="0.00" />
                    </div>
                    <div className="space-y-2">
                      <Label>&nbsp;</Label>
                      <Button className="w-full">Add Rule</Button>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Audit Log */}
          <TabsContent value="audit" className="space-y-6">
            <Card>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>System Audit Trail</CardTitle>
                  <div className="flex gap-2">
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                      <Input
                        placeholder="Search logs..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 w-[300px]"
                      />
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="border border-border rounded-lg">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Timestamp</TableHead>
                        <TableHead>Actor</TableHead>
                        <TableHead>Role</TableHead>
                        <TableHead>Acting For</TableHead>
                        <TableHead>Action</TableHead>
                        <TableHead>IP Address</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {auditLogs.map((log) => (
                        <TableRow key={log.id}>
                          <TableCell className="font-mono text-sm">{log.timestamp}</TableCell>
                          <TableCell className="font-medium">{log.actor}</TableCell>
                          <TableCell>
                            <Badge variant="outline">{log.role}</Badge>
                          </TableCell>
                          <TableCell>
                            {log.actingFor ? (
                              <Badge className="bg-warning">{log.actingFor}</Badge>
                            ) : (
                              <span className="text-muted-foreground">-</span>
                            )}
                          </TableCell>
                          <TableCell>{log.action}</TableCell>
                          <TableCell className="font-mono text-sm">{log.ip}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Jobs Monitor */}
          <TabsContent value="jobs" className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Card className="border-t-4 border-t-primary">
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-muted-foreground mb-2">Reminder Scheduler</p>
                    <p className="text-2xl font-semibold">Running</p>
                    <Badge className="mt-2 bg-success">Active</Badge>
                  </div>
                </CardContent>
              </Card>
              <Card className="border-t-4 border-t-info">
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-muted-foreground mb-2">KPI Recalculation</p>
                    <p className="text-2xl font-semibold">Idle</p>
                    <Badge variant="outline" className="mt-2">
                      Scheduled
                    </Badge>
                  </div>
                </CardContent>
              </Card>
              <Card className="border-t-4 border-t-warning">
                <CardContent className="pt-6">
                  <div className="text-center">
                    <p className="text-sm text-muted-foreground mb-2">File Scanner</p>
                    <p className="text-2xl font-semibold">Processing</p>
                    <Badge className="mt-2 bg-warning">12 in queue</Badge>
                  </div>
                </CardContent>
              </Card>
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
