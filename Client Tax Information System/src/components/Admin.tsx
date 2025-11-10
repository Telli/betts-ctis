import { useEffect, useMemo, useState } from "react";
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
import { Alert, AlertDescription } from "./ui/alert";
import { fetchAdminUsers, fetchAuditLogs, fetchJobStatuses, fetchTaxRates, AdminUser, AuditLogEntry, JobStatus, TaxRate } from "../lib/services/admin";

export function Admin() {
  const [searchTerm, setSearchTerm] = useState("");
  const [isInviteDialogOpen, setIsInviteDialogOpen] = useState(false);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [auditLogs, setAuditLogs] = useState<AuditLogEntry[]>([]);
  const [taxRates, setTaxRates] = useState<TaxRate[]>([]);
  const [jobStatuses, setJobStatuses] = useState<JobStatus[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadAdminData() {
      setIsLoading(true);
      try {
        const [usersData, logsData, ratesData, jobsData] = await Promise.all([
          fetchAdminUsers(),
          fetchAuditLogs(),
          fetchTaxRates(),
          fetchJobStatuses(),
        ]);

        if (!cancelled) {
          setUsers(usersData);
          setAuditLogs(logsData);
          setTaxRates(ratesData);
          setJobStatuses(jobsData);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load administration data.");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadAdminData();
    return () => {
      cancelled = true;
    };
  }, []);

  const filteredAuditLogs = useMemo(() => {
    const term = searchTerm.toLowerCase();
    return auditLogs.filter((log) => {
      return (
        log.actor.toLowerCase().includes(term) ||
        log.action.toLowerCase().includes(term) ||
        log.ipAddress.toLowerCase().includes(term) ||
        (log.actingFor ?? "").toLowerCase().includes(term)
      );
    });
  }, [auditLogs, searchTerm]);

  return (
    <div>
      <PageHeader title="Administration" breadcrumbs={[{ label: "Admin" }]} />

      <div className="p-6 space-y-6">
        {error && (
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}

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
                        {isLoading ? (
                          <TableRow>
                            <TableCell colSpan={5} className="text-sm text-muted-foreground text-center">
                              Loading users...
                            </TableCell>
                          </TableRow>
                        ) : users.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={5} className="text-sm text-muted-foreground text-center">
                              No users found.
                            </TableCell>
                          </TableRow>
                        ) : (
                          users.map((user) => (
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
                                <Badge className={user.status === "Active" ? "bg-success" : "bg-warning"}>
                                  {user.status}
                                </Badge>
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
                        {isLoading ? (
                          <TableRow>
                            <TableCell colSpan={4} className="text-sm text-muted-foreground text-center">
                              Loading tax rates...
                            </TableCell>
                          </TableRow>
                        ) : taxRates.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={4} className="text-sm text-muted-foreground text-center">
                              No tax rates configured.
                            </TableCell>
                          </TableRow>
                        ) : (
                          taxRates.map((rate, index) => (
                            <TableRow key={`${rate.type}-${index}`}>
                              <TableCell className="font-medium">{rate.type}</TableCell>
                              <TableCell>
                                {rate.rate}
                              </TableCell>
                              <TableCell>{rate.applicableTo}</TableCell>
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
                      {isLoading ? (
                        <TableRow>
                          <TableCell colSpan={6} className="text-sm text-muted-foreground text-center">
                            Loading audit logs...
                          </TableCell>
                        </TableRow>
                      ) : filteredAuditLogs.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={6} className="text-sm text-muted-foreground text-center">
                            No audit entries found.
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredAuditLogs.map((log) => (
                          <TableRow key={log.id}>
                            <TableCell className="font-mono text-sm">
                              {log.timestamp ? new Date(log.timestamp).toLocaleString() : "Not available"}
                            </TableCell>
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
                            <TableCell className="font-mono text-sm">{log.ipAddress}</TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Jobs Monitor */}
          <TabsContent value="jobs" className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {isLoading ? (
                  <Card className="border-t-4 border-t-muted">
                    <CardContent className="pt-6">
                      <div className="text-center text-muted-foreground">Loading job statuses...</div>
                    </CardContent>
                  </Card>
                ) : jobStatuses.length === 0 ? (
                  <Card className="border-t-4 border-t-muted">
                    <CardContent className="pt-6">
                      <div className="text-center text-muted-foreground">No job status information available.</div>
                    </CardContent>
                  </Card>
                ) : (
                  jobStatuses.map((job) => (
                    <Card key={job.name} className="border-t-4 border-t-primary">
                      <CardContent className="pt-6">
                        <div className="text-center">
                          <p className="text-sm text-muted-foreground mb-2">{job.name}</p>
                          <p className="text-2xl font-semibold">{job.state}</p>
                          <Badge
                            className={`mt-2 ${
                              job.badgeVariant === "warning"
                                ? "bg-warning"
                                : job.badgeVariant === "success"
                                ? "bg-success"
                                : job.badgeVariant === "outline"
                                ? ""
                                : "bg-muted"
                            }`}
                            variant={job.badgeVariant === "outline" ? "outline" : "default"}
                          >
                            {job.badgeText}
                          </Badge>
                        </div>
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
          </TabsContent>
        </Tabs>
      </div>
    </div>
  );
}
