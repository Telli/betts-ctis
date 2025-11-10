import { useEffect, useMemo, useState } from "react";
import { PageHeader } from "./PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Badge } from "./ui/badge";
import { Progress } from "./ui/progress";
import { Button } from "./ui/button";
import {
  CheckCircle,
  Clock,
  AlertTriangle,
  Calendar,
  FileText,
  Download,
} from "lucide-react";
import { Alert, AlertDescription } from "./ui/alert";
import {
  DeadlineDto,
  DeadlineStats,
  fetchDeadlineStats,
  fetchOverdueDeadlines,
  fetchUpcomingDeadlines,
} from "../lib/services/deadlines";

interface ComplianceProps {
  clientId?: number | null;
}

export function Compliance({ clientId }: ComplianceProps) {
  const [upcomingDeadlines, setUpcomingDeadlines] = useState<DeadlineDto[]>([]);
  const [overdueDeadlines, setOverdueDeadlines] = useState<DeadlineDto[]>([]);
  const [deadlineStats, setDeadlineStats] = useState<DeadlineStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadComplianceData() {
      setIsLoading(true);
      try {
        const [upcoming, overdue, stats] = await Promise.all([
            fetchUpcomingDeadlines(undefined, clientId ?? undefined),
            fetchOverdueDeadlines(clientId ?? undefined),
            fetchDeadlineStats(clientId ?? undefined),
        ]);

        if (cancelled) return;

        setUpcomingDeadlines(upcoming);
        setOverdueDeadlines(overdue);
        setDeadlineStats(stats);
        setError(null);
      } catch (err) {
        if (cancelled) return;
        const message = err instanceof Error ? err.message : "Failed to load compliance data.";
        setError(message);
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    loadComplianceData();
    return () => {
      cancelled = true;
    };
    }, [clientId]);

  const complianceStats = useMemo(
    () =>
      deadlineStats
        ? [
            { label: "Upcoming", count: deadlineStats.upcoming, color: "info" },
            { label: "Due Soon", count: deadlineStats.dueSoon, color: "warning" },
            { label: "Overdue", count: deadlineStats.overdue, color: "danger" },
            { label: "This Week", count: deadlineStats.thisWeek, color: "success" },
            { label: "This Month", count: deadlineStats.thisMonth, color: "info" },
          ]
        : [
            { label: "Upcoming", count: 0, color: "info" },
            { label: "Due Soon", count: 0, color: "warning" },
            { label: "Overdue", count: 0, color: "danger" },
            { label: "This Week", count: 0, color: "success" },
            { label: "This Month", count: 0, color: "info" },
          ],
    [deadlineStats],
  );

  const combinedDeadlines = useMemo(() => [...upcomingDeadlines, ...overdueDeadlines], [upcomingDeadlines, overdueDeadlines]);

  const filingOverview = useMemo(() => {
    const grouped = new Map<string, { upcoming: number; dueSoon: number; overdue: number }>();
    combinedDeadlines.forEach((deadline) => {
      const entry = grouped.get(deadline.taxTypeName) ?? { upcoming: 0, dueSoon: 0, overdue: 0 };
      const status = (deadline.status || '').toLowerCase();
      if (status === 'overdue') {
        entry.overdue += 1;
      } else if (status === 'duesoon') {
        entry.dueSoon += 1;
      } else {
        entry.upcoming += 1;
      }
      grouped.set(deadline.taxTypeName, entry);
    });

    return Array.from(grouped.entries()).map(([type, counts]) => ({
      type,
      ...counts,
    }));
  }, [combinedDeadlines]);

  const clientDocumentTracker = useMemo(() => {
    const grouped = new Map<string, { total: number; overdue: number }>();
    combinedDeadlines.forEach((deadline) => {
      const entry = grouped.get(deadline.clientName) ?? { total: 0, overdue: 0 };
      entry.total += 1;
      if ((deadline.status || '').toLowerCase() === 'overdue') {
        entry.overdue += 1;
      }
      grouped.set(deadline.clientName, entry);
    });

    return Array.from(grouped.entries()).map(([clientName, counts]) => {
      const submitted = Math.max(0, counts.total - counts.overdue);
      const progress = counts.total === 0 ? 100 : Math.max(5, Math.round((submitted / counts.total) * 100));
      return {
        clientName,
        required: counts.total,
        submitted,
        overdue: counts.overdue,
        progress,
      };
    });
  }, [combinedDeadlines]);

  const timelineEvents = useMemo(() => {
    return combinedDeadlines
      .slice()
      .sort((a, b) => new Date(b.dueDate).getTime() - new Date(a.dueDate).getTime())
      .slice(0, 5)
      .map((deadline) => ({
        id: deadline.id,
        event: `${deadline.taxTypeName} • ${deadline.clientName}`,
        date: formatDate(deadline.dueDate),
        status: (deadline.status || '').toLowerCase(),
      }));
  }, [combinedDeadlines]);

  const calculateDaysLeft = (dueDate: string) => {
    const due = new Date(dueDate).getTime();
    const today = new Date().setHours(0, 0, 0, 0);
    return Math.ceil((due - today) / (1000 * 60 * 60 * 24));
  };

  const determineDeadlineStatus = (deadline: DeadlineDto) => {
    const daysLeft = calculateDaysLeft(deadline.dueDate);
    if (daysLeft < 0) return "overdue";
    if (daysLeft <= 3) return "urgent";
    if (daysLeft <= 7) return "at-risk";
    return "pending";
  };

  const computeProgress = (daysLeft: number) => {
    if (daysLeft < 0) return 0;
    const percentage = 100 - (Math.min(daysLeft, 30) / 30) * 100;
    return Math.max(5, Math.min(100, Math.round(percentage)));
  };

  const formatDate = (value: string) =>
    new Date(value).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });

  const upcomingDisplay = useMemo(
    () =>
      upcomingDeadlines.map((deadline) => {
        const daysLeft = calculateDaysLeft(deadline.dueDate);
        const status = determineDeadlineStatus(deadline);
        return {
          id: deadline.id,
          title: `${deadline.taxTypeName} • ${deadline.clientName}`,
          dueDate: formatDate(deadline.dueDate),
          daysLeft,
          status,
          progress: computeProgress(daysLeft),
          assignedTo: deadline.assignedTo || "Unassigned",
        };
      }),
    [upcomingDeadlines],
  );

  const penaltyWarnings = useMemo(
    () =>
      overdueDeadlines.map((deadline) => {
        const daysLeft = calculateDaysLeft(deadline.dueDate);
        return {
          id: deadline.id,
          type: `${deadline.taxTypeName} • ${deadline.clientName}`,
          reason: `${deadline.priority} priority overdue task`,
          daysOverdue: Math.abs(daysLeft),
        };
      }),
    [overdueDeadlines],
  );

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

  const getTimelineIcon = (status: string) => {
    switch (status) {
      case "overdue":
        return <AlertTriangle className="w-4 h-4 text-destructive" />;
      case "duesoon":
        return <Clock className="w-4 h-4 text-warning" />;
      default:
        return <CheckCircle className="w-4 h-4 text-success" />;
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

      {error && (
        <div className="px-6">
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </div>
      )}

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

        {/* Filing Overview */}
        <Card>
          <CardHeader>
            <CardTitle>Filing Readiness by Tax Type</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="grid grid-cols-4 gap-4 pb-2 border-b">
                <div className="font-medium">Tax Type</div>
                <div className="font-medium text-center">Upcoming</div>
                <div className="font-medium text-center">Due Soon</div>
                <div className="font-medium text-center">Overdue</div>
              </div>
              {filingOverview.length === 0 ? (
                <p className="text-sm text-muted-foreground">No deadlines available.</p>
              ) : (
                filingOverview.map((item) => (
                  <div key={item.type} className="grid grid-cols-4 gap-4 items-center">
                    <div>{item.type}</div>
                    <div className="flex justify-center">
                      <Badge variant="outline">{item.upcoming}</Badge>
                    </div>
                    <div className="flex justify-center">
                      <Badge className="bg-warning text-warning-foreground">{item.dueSoon}</Badge>
                    </div>
                    <div className="flex justify-center">
                      <Badge variant="destructive">{item.overdue}</Badge>
                    </div>
                  </div>
                ))
              )}
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
              {isLoading ? (
                <p className="text-sm text-muted-foreground">Loading upcoming deadlines...</p>
              ) : upcomingDisplay.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  No upcoming deadlines in the selected window.
                </p>
              ) : (
                upcomingDisplay.map((deadline) => (
                  <div key={deadline.id} className="space-y-2 p-3 border border-border rounded-lg">
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
                        {deadline.status === "urgent"
                          ? "Urgent"
                          : deadline.status === "at-risk"
                          ? "At Risk"
                          : deadline.daysLeft >= 0
                          ? `${deadline.daysLeft} days`
                          : "Overdue"}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between text-sm text-muted-foreground">
                      <span>
                        {deadline.daysLeft >= 0
                          ? `${deadline.daysLeft} days remaining`
                          : `${Math.abs(deadline.daysLeft)} days overdue`}
                      </span>
                      <span>{deadline.assignedTo}</span>
                    </div>
                    <Progress value={deadline.progress} />
                  </div>
                ))
              )}
            </CardContent>
          </Card>

          {/* Penalty Warnings */}
          <Card>
            <CardHeader>
              <CardTitle>Penalty & Risk Alerts</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {isLoading ? (
                <p className="text-sm text-muted-foreground">Loading overdue deadlines...</p>
              ) : penaltyWarnings.length === 0 ? (
                <p className="text-sm text-muted-foreground">No overdue filings. Great work!</p>
              ) : (
                penaltyWarnings.map((warning) => (
                  <div
                    key={warning.id}
                    className="flex items-start gap-3 p-3 border border-destructive/20 bg-destructive/5 rounded-lg"
                  >
                    <AlertTriangle className="w-5 h-5 text-destructive mt-0.5" />
                    <div className="flex-1">
                      <p className="font-medium">{warning.type}</p>
                      <p className="text-sm text-muted-foreground">{warning.reason}</p>
                      <div className="flex items-center justify-between mt-2">
                        <Badge variant="destructive" className="text-xs">
                          {warning.daysOverdue} days overdue
                        </Badge>
                        <Button variant="link" className="h-auto p-0 text-sm">
                          Resolve now
                        </Button>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>
        </div>

        {/* Document Tracker */}
        <Card>
          <CardHeader>
            <CardTitle>Document Submission Tracker</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {clientDocumentTracker.length === 0 ? (
              <p className="text-sm text-muted-foreground">No active client deadlines.</p>
            ) : (
              clientDocumentTracker.map((doc) => (
                <div key={doc.clientName} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <FileText className="w-4 h-4 text-muted-foreground" />
                      <span className="font-medium">{doc.clientName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm text-muted-foreground">
                        {doc.submitted}/{doc.required} on track
                      </span>
                      {doc.overdue > 0 && (
                        <Badge variant="destructive" className="text-xs">
                          {doc.overdue} overdue
                        </Badge>
                      )}
                      <span className="text-sm font-medium">{doc.progress}%</span>
                    </div>
                  </div>
                  <Progress value={doc.progress} />
                </div>
              ))
            )}
          </CardContent>
        </Card>

        {/* Timeline */}
        <Card>
          <CardHeader>
            <CardTitle>Compliance Timeline</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {timelineEvents.length === 0 ? (
                <p className="text-sm text-muted-foreground">No recent filing activity.</p>
              ) : (
                timelineEvents.map((item, index) => (
                  <div key={item.id ?? index} className="flex gap-3">
                    <div className="flex flex-col items-center">
                      {getTimelineIcon(item.status)}
                      {index < timelineEvents.length - 1 && <div className="w-0.5 h-8 bg-border mt-2" />}
                    </div>
                    <div>
                      <p className="font-medium">{item.event}</p>
                      <p className="text-sm text-muted-foreground">{item.date}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
