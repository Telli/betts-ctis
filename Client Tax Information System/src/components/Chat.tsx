import { useState } from "react";
import { PageHeader } from "./PageHeader";
import { Card, CardContent, CardHeader, CardTitle } from "./ui/card";
import { Input } from "./ui/input";
import { Button } from "./ui/button";
import { Badge } from "./ui/badge";
import { Avatar, AvatarFallback } from "./ui/avatar";
import { Textarea } from "./ui/textarea";
import { Switch } from "./ui/switch";
import { Label } from "./ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Search, Send, Paperclip, User, Filter } from "lucide-react";

const conversations = [
  {
    id: 1,
    client: "ABC Corporation",
    subject: "Q3 GST Filing Question",
    lastMessage: "Thanks for the clarification",
    timestamp: "2 hours ago",
    status: "open",
    unread: 0,
    assignedTo: "John Doe",
  },
  {
    id: 2,
    client: "XYZ Trading",
    subject: "Payment Receipt Request",
    lastMessage: "I need a copy of the receipt",
    timestamp: "5 hours ago",
    status: "pending",
    unread: 2,
    assignedTo: "Jane Smith",
  },
  {
    id: 3,
    client: "Tech Solutions",
    subject: "Document Upload Issue",
    lastMessage: "The system won't let me upload",
    timestamp: "1 day ago",
    status: "urgent",
    unread: 1,
    assignedTo: "Unassigned",
  },
  {
    id: 4,
    client: "Global Imports",
    subject: "Excise Duty Clarification",
    lastMessage: "What rate should we apply?",
    timestamp: "2 days ago",
    status: "open",
    unread: 0,
    assignedTo: "Mike Brown",
  },
];

const messages = [
  {
    id: 1,
    sender: "Client",
    name: "Sarah Johnson",
    content: "Hi, I have a question about the Q3 GST filing. Can you help?",
    timestamp: "10:30 AM",
    isInternal: false,
  },
  {
    id: 2,
    sender: "Staff",
    name: "John Doe",
    content: "Of course! What would you like to know?",
    timestamp: "10:32 AM",
    isInternal: false,
  },
  {
    id: 3,
    sender: "Client",
    name: "Sarah Johnson",
    content: "I'm not sure how to handle the input tax credit for imported equipment.",
    timestamp: "10:35 AM",
    isInternal: false,
  },
  {
    id: 4,
    sender: "Staff",
    name: "John Doe",
    content: "[Internal Note] Need to check the current excise regulations for equipment imports",
    timestamp: "10:36 AM",
    isInternal: true,
  },
  {
    id: 5,
    sender: "Staff",
    name: "John Doe",
    content:
      "For imported equipment, you can claim input tax credit on the GST paid at the time of import. You'll need to attach the customs declaration and payment receipt to your filing.",
    timestamp: "10:40 AM",
    isInternal: false,
  },
  {
    id: 6,
    sender: "Client",
    name: "Sarah Johnson",
    content: "Thanks for the clarification! That helps a lot.",
    timestamp: "10:45 AM",
    isInternal: false,
  },
];

export function Chat() {
  const [selectedConversation, setSelectedConversation] = useState(1);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [isInternalNote, setIsInternalNote] = useState(false);

  const filteredConversations = conversations.filter((conv) => {
    const matchesSearch =
      conv.client.toLowerCase().includes(searchTerm.toLowerCase()) ||
      conv.subject.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === "all" || conv.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "open":
        return <Badge className="bg-info">Open</Badge>;
      case "pending":
        return <Badge className="bg-warning">Pending</Badge>;
      case "urgent":
        return <Badge variant="destructive">Urgent</Badge>;
      case "closed":
        return <Badge variant="outline">Closed</Badge>;
      default:
        return <Badge>{status}</Badge>;
    }
  };

  return (
    <div>
      <PageHeader title="Messages" breadcrumbs={[{ label: "Chat" }]} />

      <div className="p-6">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-[calc(100vh-200px)]">
          {/* Conversation List */}
          <Card className="lg:col-span-1 flex flex-col">
            <CardHeader className="pb-3">
              <div className="space-y-3">
                <CardTitle>Conversations</CardTitle>
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                  <Input
                    placeholder="Search conversations..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Status</SelectItem>
                    <SelectItem value="open">Open</SelectItem>
                    <SelectItem value="pending">Pending</SelectItem>
                    <SelectItem value="urgent">Urgent</SelectItem>
                    <SelectItem value="closed">Closed</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardHeader>
            <CardContent className="flex-1 overflow-y-auto space-y-2 pt-0">
              {filteredConversations.map((conv) => (
                <button
                  key={conv.id}
                  onClick={() => setSelectedConversation(conv.id)}
                  className={`w-full text-left p-3 rounded-lg border transition-colors ${
                    selectedConversation === conv.id
                      ? "border-primary bg-primary/5"
                      : "border-border hover:bg-accent"
                  }`}
                >
                  <div className="flex items-start justify-between mb-1">
                    <p className="font-medium truncate">{conv.client}</p>
                    {conv.unread > 0 && (
                      <Badge className="ml-2 h-5 min-w-5 px-1.5 flex items-center justify-center bg-destructive">
                        {conv.unread}
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm font-medium text-muted-foreground mb-1 truncate">
                    {conv.subject}
                  </p>
                  <p className="text-sm text-muted-foreground truncate mb-2">{conv.lastMessage}</p>
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-muted-foreground">{conv.timestamp}</span>
                    {getStatusBadge(conv.status)}
                  </div>
                </button>
              ))}
            </CardContent>
          </Card>

          {/* Message Thread */}
          <Card className="lg:col-span-2 flex flex-col">
            <CardHeader className="border-b border-border pb-4">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>ABC Corporation</CardTitle>
                  <p className="text-sm text-muted-foreground mt-1">Q3 GST Filing Question</p>
                </div>
                <div className="flex items-center gap-2">
                  <Select defaultValue="john">
                    <SelectTrigger className="w-[150px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="john">John Doe</SelectItem>
                      <SelectItem value="jane">Jane Smith</SelectItem>
                      <SelectItem value="mike">Mike Brown</SelectItem>
                    </SelectContent>
                  </Select>
                  {getStatusBadge("open")}
                </div>
              </div>
            </CardHeader>

            <CardContent className="flex-1 overflow-y-auto p-6 space-y-4">
              {messages.map((message) => (
                <div
                  key={message.id}
                  className={`flex gap-3 ${message.isInternal ? "opacity-75" : ""}`}
                >
                  <Avatar>
                    <AvatarFallback className={message.sender === "Staff" ? "bg-primary text-primary-foreground" : "bg-muted"}>
                      {message.name.split(" ").map((n) => n[0]).join("")}
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium">{message.name}</span>
                      <span className="text-xs text-muted-foreground">{message.timestamp}</span>
                      {message.isInternal && (
                        <Badge variant="outline" className="text-xs">
                          Internal
                        </Badge>
                      )}
                    </div>
                    <div
                      className={`p-3 rounded-lg ${
                        message.isInternal
                          ? "bg-warning/10 border border-warning/20"
                          : message.sender === "Staff"
                          ? "bg-primary/5"
                          : "bg-muted"
                      }`}
                    >
                      <p className="text-sm">{message.content}</p>
                    </div>
                  </div>
                </div>
              ))}
            </CardContent>

            <div className="border-t border-border p-4">
              <div className="flex items-center gap-2 mb-3">
                <Switch
                  id="internal-note"
                  checked={isInternalNote}
                  onCheckedChange={setIsInternalNote}
                />
                <Label htmlFor="internal-note" className="text-sm cursor-pointer">
                  Internal note (not visible to client)
                </Label>
              </div>
              <div className="flex gap-2">
                <Textarea
                  placeholder={
                    isInternalNote ? "Add an internal note..." : "Type your message..."
                  }
                  rows={2}
                  className={isInternalNote ? "border-warning" : ""}
                />
                <div className="flex flex-col gap-2">
                  <Button size="icon" variant="outline">
                    <Paperclip className="w-4 h-4" />
                  </Button>
                  <Button size="icon">
                    <Send className="w-4 h-4" />
                  </Button>
                </div>
              </div>
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
