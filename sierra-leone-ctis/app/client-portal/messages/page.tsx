"use client"

import { useState, useEffect } from "react"
import { useAuth } from "@/context/auth-context"
import { ClientPortalService, Message, SendMessageDto, MessageReplyDto } from "@/lib/services/client-portal-service"
import { useChat } from "@/hooks/useSignalR"
import { useToast } from "@/hooks/use-toast"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Textarea } from "@/components/ui/textarea"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ScrollArea } from "@/components/ui/scroll-area"
import { Separator } from "@/components/ui/separator"
import {
  MessageSquare,
  Send,
  Reply,
  Star,
  Archive,
  Trash2,
  Search,
  Filter,
  Plus,
  Mail,
  Inbox,
  Send as SendIcon,
  Archive as ArchiveIcon,
  Star as StarIcon,
  Clock,
  CheckCircle,
  AlertCircle,
  User,
  Building2,
  FileText
} from "lucide-react"
import { format } from "date-fns"

export default function ClientMessagesPage() {
  const { user } = useAuth()
  const { toast } = useToast()
  const [messages, setMessages] = useState<Message[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedMessage, setSelectedMessage] = useState<Message | null>(null)
  const [showComposeDialog, setShowComposeDialog] = useState(false)
  const [showReplyDialog, setShowReplyDialog] = useState(false)
  const [activeTab, setActiveTab] = useState("inbox")
  const [searchTerm, setSearchTerm] = useState("")
  const [unreadCount, setUnreadCount] = useState(0)

  // Real-time chat with SignalR (use conversation ID from selected message)
  const { isConnected: chatConnected, messages: realtimeMessages } = useChat(
    selectedMessage?.messageId
  )

  // Compose form state
  const [composeForm, setComposeForm] = useState({
    subject: "",
    body: "",
    priority: "Normal" as "Low" | "Normal" | "High" | "Urgent",
    category: "General" as "General" | "Document" | "TaxFiling" | "Payment" | "Compliance" | "Support"
  })

  // Reply form state
  const [replyForm, setReplyForm] = useState({
    body: ""
  })

  useEffect(() => {
    loadMessages()
    loadUnreadCount()
  }, [activeTab])
  
  // Handle real-time messages from SignalR
  useEffect(() => {
    if (realtimeMessages.length > 0) {
      const latestMessage = realtimeMessages[realtimeMessages.length - 1]
      toast({
        title: 'New Message',
        description: `From ${latestMessage.senderName}: ${latestMessage.message.substring(0, 50)}...`,
      })
      // Reload messages to show the new one
      loadMessages()
      loadUnreadCount()
    }
  }, [realtimeMessages])
  
  // Show connection status
  useEffect(() => {
    if (chatConnected) {
      console.log('✅ Real-time chat connected')
    }
  }, [chatConnected])

  const loadMessages = async () => {
    try {
      setLoading(true)
      let response

      switch (activeTab) {
        case "inbox":
          response = await ClientPortalService.getInbox()
          break
        case "sent":
          response = await ClientPortalService.getSentMessages()
          break
        case "archived":
          response = await ClientPortalService.getArchivedMessages()
          break
        case "starred":
          response = await ClientPortalService.getStarredMessages()
          break
        default:
          response = await ClientPortalService.getInbox()
      }

      setMessages(response.items)
    } catch (error) {
      console.error("Error loading messages:", error)
    } finally {
      setLoading(false)
    }
  }

  const loadUnreadCount = async () => {
    try {
      const response = await ClientPortalService.getUnreadCount()
      setUnreadCount(response.count)
    } catch (error) {
      console.error("Error loading unread count:", error)
    }
  }

  const handleSendMessage = async () => {
    try {
      // For now, send to a default associate. In a real implementation,
      // this would allow selecting from available associates
      const messageData: SendMessageDto = {
        recipientId: "associate-default", // This would be selected from a dropdown
        subject: composeForm.subject,
        body: composeForm.body,
        priority: composeForm.priority,
        category: composeForm.category
      }

      await ClientPortalService.sendMessage(messageData)

      setShowComposeDialog(false)
      setComposeForm({
        subject: "",
        body: "",
        priority: "Normal",
        category: "General"
      })

      loadMessages()
      loadUnreadCount()
    } catch (error) {
      console.error("Error sending message:", error)
    }
  }

  const handleReply = async () => {
    if (!selectedMessage) return

    try {
      const replyData: MessageReplyDto = {
        body: replyForm.body
      }

      await ClientPortalService.replyToMessage(selectedMessage.messageId, replyData)

      setShowReplyDialog(false)
      setReplyForm({ body: "" })

      // Reload the message thread
      const thread = await ClientPortalService.getMessageThread(selectedMessage.messageId)
      setSelectedMessage(thread.rootMessage)
      loadMessages()
    } catch (error) {
      console.error("Error replying to message:", error)
    }
  }

  const handleMarkAsRead = async (messageId: number) => {
    try {
      await ClientPortalService.markMessageAsRead(messageId)
      loadMessages()
      loadUnreadCount()
    } catch (error) {
      console.error("Error marking message as read:", error)
    }
  }

  const handleToggleStar = async (messageId: number) => {
    try {
      await ClientPortalService.toggleMessageStar(messageId)
      loadMessages()
    } catch (error) {
      console.error("Error toggling star:", error)
    }
  }

  const handleArchive = async (messageId: number) => {
    try {
      await ClientPortalService.archiveMessage(messageId)
      loadMessages()
      loadUnreadCount()
    } catch (error) {
      console.error("Error archiving message:", error)
    }
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case "Urgent": return "bg-red-100 text-red-800"
      case "High": return "bg-orange-100 text-orange-800"
      case "Normal": return "bg-blue-100 text-blue-800"
      case "Low": return "bg-gray-100 text-gray-800"
      default: return "bg-gray-100 text-gray-800"
    }
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "Read": return <CheckCircle className="h-4 w-4 text-green-600" />
      case "Delivered": return <Clock className="h-4 w-4 text-blue-600" />
      default: return <AlertCircle className="h-4 w-4 text-gray-600" />
    }
  }

  const filteredMessages = messages.filter(message =>
    message.subject.toLowerCase().includes(searchTerm.toLowerCase()) ||
    message.senderName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    message.body.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-sierra-orange-800">Messages</h1>
          <p className="text-sierra-orange-600">Communicate with your tax associates</p>
        </div>
        <Dialog open={showComposeDialog} onOpenChange={setShowComposeDialog}>
          <DialogTrigger asChild>
            <Button className="bg-sierra-orange-600 hover:bg-sierra-orange-700">
              <Plus className="h-4 w-4 mr-2" />
              New Message
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Compose New Message</DialogTitle>
              <DialogDescription>
                Send a message to your tax associate
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div>
                <Label htmlFor="subject">Subject</Label>
                <Input
                  id="subject"
                  value={composeForm.subject}
                  onChange={(e) => setComposeForm(prev => ({ ...prev, subject: e.target.value }))}
                  placeholder="Enter message subject"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="priority">Priority</Label>
                  <select
                    id="priority"
                    value={composeForm.priority}
                    onChange={(e) => setComposeForm(prev => ({ ...prev, priority: e.target.value as any }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-sierra-orange-500"
                  >
                    <option value="Low">Low</option>
                    <option value="Normal">Normal</option>
                    <option value="High">High</option>
                    <option value="Urgent">Urgent</option>
                  </select>
                </div>
                <div>
                  <Label htmlFor="category">Category</Label>
                  <select
                    id="category"
                    value={composeForm.category}
                    onChange={(e) => setComposeForm(prev => ({ ...prev, category: e.target.value as any }))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-sierra-orange-500"
                  >
                    <option value="General">General</option>
                    <option value="Document">Document</option>
                    <option value="TaxFiling">Tax Filing</option>
                    <option value="Payment">Payment</option>
                    <option value="Compliance">Compliance</option>
                    <option value="Support">Support</option>
                  </select>
                </div>
              </div>
              <div>
                <Label htmlFor="body">Message</Label>
                <Textarea
                  id="body"
                  value={composeForm.body}
                  onChange={(e) => setComposeForm(prev => ({ ...prev, body: e.target.value }))}
                  placeholder="Enter your message"
                  rows={6}
                />
              </div>
              <div className="flex justify-end space-x-2">
                <Button variant="outline" onClick={() => setShowComposeDialog(false)}>
                  Cancel
                </Button>
                <Button onClick={handleSendMessage} className="bg-sierra-orange-600 hover:bg-sierra-orange-700">
                  <Send className="h-4 w-4 mr-2" />
                  Send Message
                </Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Message List */}
        <div className="lg:col-span-1">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center">
                  <Mail className="h-5 w-5 mr-2" />
                  Messages
                </CardTitle>
                {unreadCount > 0 && (
                  <Badge variant="destructive" className="bg-sierra-orange-600">
                    {unreadCount} unread
                  </Badge>
                )}
              </div>
              <div className="flex items-center space-x-2">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                  <Input
                    placeholder="Search messages..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <Tabs value={activeTab} onValueChange={setActiveTab}>
                <TabsList className="grid w-full grid-cols-4">
                  <TabsTrigger value="inbox" className="flex items-center">
                    <Inbox className="h-4 w-4 mr-1" />
                    Inbox
                  </TabsTrigger>
                  <TabsTrigger value="sent" className="flex items-center">
                    <SendIcon className="h-4 w-4 mr-1" />
                    Sent
                  </TabsTrigger>
                  <TabsTrigger value="starred" className="flex items-center">
                    <StarIcon className="h-4 w-4 mr-1" />
                    Starred
                  </TabsTrigger>
                  <TabsTrigger value="archived" className="flex items-center">
                    <ArchiveIcon className="h-4 w-4 mr-1" />
                    Archive
                  </TabsTrigger>
                </TabsList>

                <ScrollArea className="h-96 mt-4">
                  {loading ? (
                    <div className="flex items-center justify-center py-8">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-orange-600"></div>
                    </div>
                  ) : filteredMessages.length === 0 ? (
                    <div className="text-center py-8 text-gray-500">
                      <Mail className="h-12 w-12 mx-auto mb-4 text-gray-300" />
                      <p>No messages found</p>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {filteredMessages.map((message) => (
                        <div
                          key={message.messageId}
                          className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                            selectedMessage?.messageId === message.messageId
                              ? "bg-sierra-orange-50 border-sierra-orange-300"
                              : "hover:bg-gray-50"
                          }`}
                          onClick={() => {
                            setSelectedMessage(message)
                            if (message.status !== "Read") {
                              handleMarkAsRead(message.messageId)
                            }
                          }}
                        >
                          <div className="flex items-start justify-between">
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center space-x-2 mb-1">
                                <p className="font-medium text-sm truncate">
                                  {message.senderName}
                                </p>
                                {getStatusIcon(message.status)}
                              </div>
                              <p className="text-sm font-medium text-gray-900 truncate mb-1">
                                {message.subject}
                              </p>
                              <p className="text-xs text-gray-600 truncate">
                                {message.body.substring(0, 50)}...
                              </p>
                            </div>
                            <div className="flex flex-col items-end space-y-1">
                              <Badge className={`text-xs ${getPriorityColor(message.priority)}`}>
                                {message.priority}
                              </Badge>
                              <p className="text-xs text-gray-500">
                                {format(new Date(message.sentDate), "MMM d")}
                              </p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </ScrollArea>
              </Tabs>
            </CardContent>
          </Card>
        </div>

        {/* Message Detail */}
        <div className="lg:col-span-2">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>
                  {selectedMessage ? selectedMessage.subject : "Select a message"}
                </CardTitle>
                {selectedMessage && (
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleToggleStar(selectedMessage.messageId)}
                    >
                      <Star className={`h-4 w-4 ${selectedMessage.isStarred ? "fill-yellow-400 text-yellow-400" : ""}`} />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleArchive(selectedMessage.messageId)}
                    >
                      <Archive className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setShowReplyDialog(true)}
                    >
                      <Reply className="h-4 w-4" />
                    </Button>
                  </div>
                )}
              </div>
            </CardHeader>
            <CardContent>
              {selectedMessage ? (
                <div className="space-y-4">
                  <div className="flex items-start space-x-4">
                    <Avatar>
                      <AvatarFallback>
                        {selectedMessage.senderName.split(' ').map(n => n[0]).join('')}
                      </AvatarFallback>
                    </Avatar>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-2">
                        <h3 className="font-medium">{selectedMessage.senderName}</h3>
                        <Badge className={getPriorityColor(selectedMessage.priority)}>
                          {selectedMessage.priority}
                        </Badge>
                        <span className="text-sm text-gray-500">
                          {format(new Date(selectedMessage.sentDate), "PPP 'at' p")}
                        </span>
                      </div>
                      <div className="prose prose-sm max-w-none">
                        <p className="whitespace-pre-wrap">{selectedMessage.body}</p>
                      </div>
                      {selectedMessage.attachments && selectedMessage.attachments.length > 0 && (
                        <div className="mt-4">
                          <h4 className="font-medium mb-2">Attachments</h4>
                          <div className="space-y-2">
                            {selectedMessage.attachments.map((attachment) => (
                              <div key={attachment.documentId} className="flex items-center space-x-2 p-2 bg-gray-50 rounded">
                                <FileText className="h-4 w-4" />
                                <span className="text-sm">{attachment.originalFileName}</span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  </div>

                  {selectedMessage.replies && selectedMessage.replies.length > 0 && (
                    <div className="space-y-4">
                      <Separator />
                      <h4 className="font-medium">Replies</h4>
                      {selectedMessage.replies.map((reply) => (
                        <div key={reply.messageId} className="flex items-start space-x-4 pl-8 border-l-2 border-gray-200">
                          <Avatar className="w-8 h-8">
                            <AvatarFallback className="text-xs">
                              {reply.senderName.split(' ').map(n => n[0]).join('')}
                            </AvatarFallback>
                          </Avatar>
                          <div className="flex-1">
                            <div className="flex items-center space-x-2 mb-1">
                              <span className="font-medium text-sm">{reply.senderName}</span>
                              <span className="text-xs text-gray-500">
                                {format(new Date(reply.sentDate), "MMM d 'at' p")}
                              </span>
                            </div>
                            <p className="text-sm whitespace-pre-wrap">{reply.body}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ) : (
                <div className="text-center py-12">
                  <MessageSquare className="h-16 w-16 mx-auto mb-4 text-gray-300" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No message selected</h3>
                  <p className="text-gray-500">Choose a message from the list to view its contents</p>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Reply Dialog */}
      <Dialog open={showReplyDialog} onOpenChange={setShowReplyDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reply to Message</DialogTitle>
            <DialogDescription>
              Send a reply to {selectedMessage?.senderName}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Label htmlFor="reply-body">Your Reply</Label>
              <Textarea
                id="reply-body"
                value={replyForm.body}
                onChange={(e) => setReplyForm({ body: e.target.value })}
                placeholder="Enter your reply"
                rows={4}
              />
            </div>
            <div className="flex justify-end space-x-2">
              <Button variant="outline" onClick={() => setShowReplyDialog(false)}>
                Cancel
              </Button>
              <Button onClick={handleReply} className="bg-sierra-orange-600 hover:bg-sierra-orange-700">
                <Send className="h-4 w-4 mr-2" />
                Send Reply
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}