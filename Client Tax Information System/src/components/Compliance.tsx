import { PageHeader } from "./PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Badge } from "./ui/badge";
import { Progress } from "./ui/progress";
import { Button } from "./ui/button";
import {
  CheckCircle,
  Clock,
  XCircle,
  AlertTriangle,
  Calendar,
  FileText,
  Download,
} from "lucide-react";

export function Compliance() {
  const complianceStats = [
    { label: "Filed", count: 42, color: "success" },
    { label: "Pending", count: 8, color: "warning" },
    { label: "Paid", count: 38, color: "info" },
    { label: "Overdue", count: 2, color: "danger" },
    { label: "N/A", count: 5, color: "muted" },
  ];

  const filingChecklist = [
    { type: "GST Returns", q1: "filed", q2: "filed", q3: "pending", q4: "upcoming" },
    { type: "PAYE Returns", q1: "filed", q2: "filed", q3: "filed", q4: "upcoming" },
    { type: "Income Tax", q1: "filed", q2: "n/a", q3: "n/a", q4: "n/a" },
    { type: "Excise Duty", q1: "filed", q2: "filed", q3: "overdue", q4: "upcoming" },
  ];

  const upcomingDeadlines = [
    {
      title: "GST Return Q3 2025",
      dueDate: "2025-10-15",
      daysLeft: 8,
      status: "pending",
      progress: 75,
    },
    {
      title: "Payroll Tax September",
      dueDate: "2025-10-12",
      daysLeft: 5,
      status: "at-risk",
      progress: 40,
    },
    {
      title: "Excise Duty Q3",
      dueDate: "2025-10-10",
      daysLeft: 3,
      status: "urgent",
      progress: 20,
    },
  ];

  const penaltyWarnings = [
    {
      type: "Excise Duty Q3",
      reason: "Late filing",
      estimatedAmount: 5000,
      daysOverdue: 2,
    },
    {
      type: "GST Return Q2",
      reason: "Payment delay",
      estimatedAmount: 2500,
      daysOverdue: 15,
    },
  ];

  const documentTracker = [
    { name: "Financial Statements", required: 12, submitted: 12, progress: 100 },
    { name: "Bank Statements", required: 12, submitted: 11, progress: 92 },
    { name: "Payroll Records", required: 12, submitted: 10, progress: 83 },
    { name: "Sales Invoices", required: 4, submitted: 3, progress: 75 },
  ];

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "filed":
        return <CheckCircle className="w-4 h-4 text-success" />;
      case "pending":
        return <Clock className="w-4 h-4 text-warning" />;
      case "overdue":
        return <AlertTriangle className="w-4 h-4 text-destructive" />;
      default:
        return <div className="w-4 h-4 rounded-full bg-muted" />;
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "filed":
        return <Badge className="bg-success">Filed</Badge>;
      case "pending":
        return <Badge className="bg-warning">Pending</Badge>;
      case "overdue":
        return <Badge variant="destructive">Overdue</Badge>;
      case "upcoming":
        return <Badge variant="outline">Upcoming</Badge>;
      default:
        return <Badge variant="secondary">N/A</Badge>;
    }
  };

  return (
    <div>
      <PageHeader
        title="Compliance Overview"
        breadcrumbs={[{ label: "Compliance" }]}
        actions={
          <Button variant="outline">
            <Download className="w-4 h-4 mr-2" />
            Export Report
          </Button>
        }
      />

      <div className="p-6 space-y-6">
        {/* Status Blocks */}
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
          {complianceStats.map((stat) => (
            <Card
              key={stat.label}
              className={`border-t-4 ${
                stat.color === "success"
                  ? "border-t-success"
                  : stat.color === "warning"
                  ? "border-t-warning"
                  : stat.color === "info"
                  ? "border-t-info"
                  : stat.color === "danger"
                  ? "border-t-destructive"
                  : "border-t-muted"
              }`}
            >
              <CardContent className="pt-6">
                <div className="text-center">
                  <div className="text-3xl font-semibold">{stat.count}</div>
                  <p className="text-sm text-muted-foreground mt-1">{stat.label}</p>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Filing Checklist */}
        <Card>
          <CardHeader>
            <CardTitle>Filing Checklist - 2025</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="grid grid-cols-5 gap-4 pb-2 border-b">
                <div className="font-medium">Tax Type</div>
                <div className="font-medium text-center">Q1</div>
                <div className="font-medium text-center">Q2</div>
                <div className="font-medium text-center">Q3</div>
                <div className="font-medium text-center">Q4</div>
              </div>
              {filingChecklist.map((item, index) => (
                <div key={index} className="grid grid-cols-5 gap-4 items-center">
                  <div>{item.type}</div>
                  <div className="flex justify-center">{getStatusIcon(item.q1)}</div>
                  <div className="flex justify-center">{getStatusIcon(item.q2)}</div>
                  <div className="flex justify-center">{getStatusIcon(item.q3)}</div>
                  <div className="flex justify-center">{getStatusIcon(item.q4)}</div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Upcoming Deadlines */}
          <Card>
            <CardHeader>
              <CardTitle>Upcoming Deadlines</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {upcomingDeadlines.map((deadline, index) => (
                <div key={index} className="space-y-2 p-3 border border-border rounded-lg">
                  <div className="flex items-start justify-between">
                    <div>
                      <p className="font-medium">{deadline.title}</p>
                      <div className="flex items-center gap-2 mt-1">
                        <Calendar className="w-3 h-3 text-muted-foreground" />
                        <span className="text-sm text-muted-foreground">Due {deadline.dueDate}</span>
                      </div>
                    </div>
                    <Badge
                      variant={
                        deadline.status === "urgent"
                          ? "destructive"
                          : deadline.status === "at-risk"
                          ? "default"
                          : "outline"
                      }
                      className={
                        deadline.status === "at-risk" ? "bg-warning text-warning-foreground" : ""
                      }
                    >
                      {deadline.daysLeft} days
                    </Badge>
                  </div>
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">Progress</span>
                      <span>{deadline.progress}%</span>
                    </div>
                    <Progress value={deadline.progress} />
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* Penalty Warnings */}
          <Card>
            <CardHeader>
              <CardTitle>Penalty Warnings</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {penaltyWarnings.map((warning, index) => (
                <div
                  key={index}
                  className="flex items-start gap-3 p-3 border border-destructive/20 bg-destructive/5 rounded-lg"
                >
                  <AlertTriangle className="w-5 h-5 text-destructive mt-0.5" />
                  <div className="flex-1">
                    <p className="font-medium">{warning.type}</p>
                    <p className="text-sm text-muted-foreground">{warning.reason}</p>
                    <div className="flex items-center justify-between mt-2">
                      <span className="text-sm text-destructive font-medium">
                        Estimated: SLE {warning.estimatedAmount.toLocaleString()}
                      </span>
                      <Badge variant="destructive" className="text-xs">
                        {warning.daysOverdue} days overdue
                      </Badge>
                    </div>
                  </div>
                </div>
              ))}

              <div className="p-3 border border-border rounded-lg">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">Total Potential Penalties</p>
                    <p className="text-sm text-muted-foreground">If not addressed</p>
                  </div>
                  <span className="text-xl font-semibold text-destructive">SLE 7,500</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Document Tracker */}
        <Card>
          <CardHeader>
            <CardTitle>Document Submission Tracker</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {documentTracker.map((doc, index) => (
              <div key={index} className="space-y-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <FileText className="w-4 h-4 text-muted-foreground" />
                    <span className="font-medium">{doc.name}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-muted-foreground">
                      {doc.submitted}/{doc.required} submitted
                    </span>
                    <span className="text-sm font-medium">{doc.progress}%</span>
                  </div>
                </div>
                <Progress value={doc.progress} />
              </div>
            ))}
          </CardContent>
        </Card>

        {/* Timeline */}
        <Card>
          <CardHeader>
            <CardTitle>Compliance Timeline</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                {
                  date: "Oct 5, 2025",
                  event: "GST Return Q3 filed on time",
                  status: "success",
                },
                {
                  date: "Sep 28, 2025",
                  event: "Payroll tax payment processed",
                  status: "success",
                },
                {
                  date: "Sep 15, 2025",
                  event: "Income tax filed 5 days early",
                  status: "success",
                },
                {
                  date: "Aug 30, 2025",
                  event: "GST Return Q2 filed on time",
                  status: "success",
                },
              ].map((item, index) => (
                <div key={index} className="flex gap-3">
                  <div className="flex flex-col items-center">
                    <CheckCircle className="w-4 h-4 text-success" />
                    {index < 3 && <div className="w-0.5 h-8 bg-border mt-2" />}
                  </div>
                  <div>
                    <p className="font-medium">{item.event}</p>
                    <p className="text-sm text-muted-foreground">{item.date}</p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
