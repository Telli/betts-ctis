import { useEffect, useMemo, useState } from "react";
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
import { Alert, AlertDescription } from "./ui/alert";
import { fetchConversations, fetchMessages, ChatConversation, ChatMessage } from "../lib/services/chat";

interface ChatProps {
  clientId?: number | null;
}

export function Chat({ clientId }: ChatProps) {
  const [conversations, setConversations] = useState<ChatConversation[]>([]);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [selectedConversation, setSelectedConversation] = useState<number | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [isInternalNote, setIsInternalNote] = useState(false);
  const [isLoadingConversations, setIsLoadingConversations] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadConversations() {
      setIsLoadingConversations(true);
      try {
        const data = await fetchConversations();
        const filtered = clientId ? data.filter((conv) => conv.clientId === clientId) : data;
        if (!cancelled) {
          setConversations(filtered);
          setSelectedConversation(filtered.length > 0 ? filtered[0].id : null);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load conversations.");
        }
      } finally {
        if (!cancelled) {
          setIsLoadingConversations(false);
        }
      }
    }

    loadConversations();
    return () => {
      cancelled = true;
    };
  }, [clientId]);

  useEffect(() => {
    if (selectedConversation === null) {
      setMessages([]);
      return;
    }

    let cancelled = false;

    async function loadMessages() {
      setIsLoadingMessages(true);
      try {
        const data = await fetchMessages(selectedConversation);
        if (!cancelled) {
          setMessages(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load messages.");
        }
      } finally {
        if (!cancelled) {
          setIsLoadingMessages(false);
        }
      }
    }

    loadMessages();
    return () => {
      cancelled = true;
    };
  }, [selectedConversation]);

  const filteredConversations = conversations.filter((conv) => {
    if (clientId && conv.clientId !== clientId) {
      return false;
    }
    const matchesSearch =
      conv.client.toLowerCase().includes(searchTerm.toLowerCase()) ||
      conv.subject.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === "all" || conv.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  const activeConversation = useMemo(
    () => conversations.find((conv) => conv.id === selectedConversation) ?? null,
    [conversations, selectedConversation],
  );

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

        <div className="p-6 space-y-4">
          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}

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
                {isLoadingConversations ? (
                  <p className="text-sm text-muted-foreground">Loading conversations...</p>
                ) : filteredConversations.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No conversations found.</p>
                ) : (
                  filteredConversations.map((conv) => (
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
                        {conv.unreadCount > 0 && (
                          <Badge className="ml-2 h-5 min-w-5 px-1.5 flex items-center justify-center bg-destructive">
                            {conv.unreadCount}
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm font-medium text-muted-foreground mb-1 truncate">
                        {conv.subject}
                      </p>
                      <p className="text-sm text-muted-foreground truncate mb-2">{conv.lastMessagePreview}</p>
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-muted-foreground">{conv.timestampDisplay}</span>
                        {getStatusBadge(conv.status)}
                      </div>
                    </button>
                  ))
                )}
              </CardContent>
          </Card>

          {/* Message Thread */}
          <Card className="lg:col-span-2 flex flex-col">
            <CardHeader className="border-b border-border pb-4">
                {activeConversation ? (
                  <div className="flex items-center justify-between">
                    <div>
                      <CardTitle>{activeConversation.client}</CardTitle>
                      <p className="text-sm text-muted-foreground mt-1">{activeConversation.subject}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      {activeConversation.assignedTo ? (
                        <Badge variant="outline" className="text-xs">
                          Assigned to {activeConversation.assignedTo}
                        </Badge>
                      ) : null}
                      {getStatusBadge(activeConversation.status)}
                    </div>
                  </div>
                ) : (
                  <div>
                    <CardTitle>Select a conversation</CardTitle>
                    <p className="text-sm text-muted-foreground mt-1">Choose a conversation to view messages.</p>
                  </div>
                )}
            </CardHeader>

            <CardContent className="flex-1 overflow-y-auto p-6 space-y-4">
                {selectedConversation === null ? (
                  <p className="text-sm text-muted-foreground">Select a conversation to view messages.</p>
                ) : isLoadingMessages ? (
                  <p className="text-sm text-muted-foreground">Loading messages...</p>
                ) : messages.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No messages in this conversation yet.</p>
                ) : (
                  messages.map((message) => (
                    <div
                      key={message.id}
                      className={`flex gap-3 ${message.isInternal ? "opacity-75" : ""}`}
                    >
                      <Avatar>
                        <AvatarFallback className={
                          message.senderType === "Staff"
                            ? "bg-primary text-primary-foreground"
                            : "bg-muted"
                        }>
                          {message.senderName
                            .split(" ")
                            .map((n) => n[0])
                            .join("")
                            .toUpperCase()}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="font-medium">{message.senderName}</span>
                          <span className="text-xs text-muted-foreground">
                            {message.sentAt ? new Date(message.sentAt).toLocaleString() : "Not available"}
                          </span>
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
                              : message.senderType === "Staff"
                              ? "bg-primary/5"
                              : "bg-muted"
                          }`}
                        >
                          <p className="text-sm whitespace-pre-line">{message.content}</p>
                        </div>
                      </div>
                    </div>
                  ))
                )}
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
                    placeholder={isInternalNote ? "Add an internal note..." : "Type your message..."}
                    rows={2}
                    className={isInternalNote ? "border-warning" : ""}
                    disabled={selectedConversation === null}
                  />
                  <div className="flex flex-col gap-2">
                    <Button size="icon" variant="outline" disabled={selectedConversation === null}>
                      <Paperclip className="w-4 h-4" />
                    </Button>
                    <Button size="icon" disabled={selectedConversation === null}>
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
