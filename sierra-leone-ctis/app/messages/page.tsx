'use client'

import { useState, useEffect } from 'react'
import { useAuth } from '@/context/auth-context'
import { MessageService, Conversation, Message as MessageType, ConversationStatus } from '@/lib/services/message-service'
import { useToast } from '@/hooks/use-toast'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Textarea } from '@/components/ui/textarea'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { MessageSquare, Send, Lock, AlertCircle, Clock, CheckCircle, XCircle, Search, UserPlus } from 'lucide-react'
import { format } from 'date-fns'
import ConversationList from '@/components/messages/conversation-list'
import MessageThread from '@/components/messages/message-thread'
import AssignmentSelector from '@/components/messages/assignment-selector'

export default function MessagesPage() {
  const { user } = useAuth()
  const { toast } = useToast()
  const [conversations, setConversations] = useState<Conversation[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedConversation, setSelectedConversation] = useState<Conversation | null>(null)
  const [messages, setMessages] = useState<MessageType[]>([])
  const [showReplyDialog, setShowReplyDialog] = useState(false)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState<ConversationStatus | 'all'>('all')
  const [replyForm, setReplyForm] = useState({
    body: '',
    isInternal: false,
  })

  useEffect(() => {
    loadConversations()
  }, [statusFilter])

  useEffect(() => {
    if (selectedConversation) {
      loadMessages()
    }
  }, [selectedConversation])

  const loadConversations = async () => {
    try {
      setLoading(true)
      const filters = statusFilter !== 'all' ? { status: statusFilter } : undefined
      const data = await MessageService.getConversations(filters)
      setConversations(data)
    } catch (error) {
      console.error('Error loading conversations:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load conversations',
      })
    } finally {
      setLoading(false)
    }
  }

  const loadMessages = async () => {
    if (!selectedConversation) return

    try {
      const data = await MessageService.getMessages(selectedConversation.id)
      setMessages(data)
    } catch (error) {
      console.error('Error loading messages:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load messages',
      })
    }
  }

  const handleSendReply = async () => {
    if (!selectedConversation || !replyForm.body.trim()) return

    try {
      await MessageService.sendMessage(selectedConversation.id, {
        body: replyForm.body,
        isInternal: replyForm.isInternal,
      })

      toast({
        title: 'Success',
        description: replyForm.isInternal ? 'Internal note added' : 'Message sent',
      })

      setShowReplyDialog(false)
      setReplyForm({ body: '', isInternal: false })
      loadMessages()
      loadConversations()
    } catch (error) {
      console.error('Error sending reply:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to send message',
      })
    }
  }

  const handleAssignConversation = async (userId: string) => {
    if (!selectedConversation) return

    try {
      await MessageService.assignConversation(selectedConversation.id, userId)
      toast({
        title: 'Success',
        description: 'Conversation assigned successfully',
      })
      loadConversations()
    } catch (error) {
      console.error('Error assigning conversation:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to assign conversation',
      })
    }
  }

  const handleStatusChange = async (status: ConversationStatus) => {
    if (!selectedConversation) return

    try {
      await MessageService.updateConversationStatus(selectedConversation.id, status)
      toast({
        title: 'Success',
        description: 'Status updated successfully',
      })
      loadConversations()
      setSelectedConversation({ ...selectedConversation, status })
    } catch (error) {
      console.error('Error updating status:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to update status',
      })
    }
  }

  const getStatusBadge = (status: ConversationStatus) => {
    switch (status) {
      case 'Open':
        return <Badge className="bg-green-100 text-green-800">Open</Badge>
      case 'Pending':
        return <Badge className="bg-yellow-100 text-yellow-800">Pending</Badge>
      case 'Urgent':
        return <Badge className="bg-red-100 text-red-800">Urgent</Badge>
      case 'Closed':
        return <Badge className="bg-gray-100 text-gray-800">Closed</Badge>
      default:
        return null
    }
  }

  const filteredConversations = conversations.filter(
    (conv) =>
      conv.clientName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      conv.subject.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="flex-1 flex flex-col p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Messages & Conversations</h1>
          <p className="text-muted-foreground">Manage client conversations and internal notes</p>
        </div>
      </div>

      <div className="flex gap-4 items-center">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search conversations..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-10"
          />
        </div>
        <div>
          <Select value={statusFilter} onValueChange={(value: any) => setStatusFilter(value)}>
            <SelectTrigger className="w-[180px]">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Conversations</SelectItem>
              <SelectItem value="Open">Open</SelectItem>
              <SelectItem value="Pending">Pending</SelectItem>
              <SelectItem value="Urgent">Urgent</SelectItem>
              <SelectItem value="Closed">Closed</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 flex-1">
        {/* Conversation List */}
        <div className="lg:col-span-1">
          <Card className="h-full">
            <CardHeader>
              <CardTitle>Conversations</CardTitle>
            </CardHeader>
            <CardContent>
              <ScrollArea className="h-[600px]">
                {loading ? (
                  <div className="flex items-center justify-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
                  </div>
                ) : filteredConversations.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    <MessageSquare className="h-12 w-12 mx-auto mb-4 text-muted" />
                    <p>No conversations found</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {filteredConversations.map((conversation) => (
                      <div
                        key={conversation.id}
                        className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                          selectedConversation?.id === conversation.id
                            ? 'bg-primary/5 border-primary'
                            : 'hover:bg-muted'
                        }`}
                        onClick={() => setSelectedConversation(conversation)}
                      >
                        <div className="flex items-start justify-between mb-2">
                          <div className="flex-1 min-w-0">
                            <p className="font-medium text-sm truncate">{conversation.clientName}</p>
                            <p className="text-sm text-muted-foreground truncate">{conversation.subject}</p>
                          </div>
                          {getStatusBadge(conversation.status)}
                        </div>
                        <div className="flex items-center justify-between text-xs text-muted-foreground">
                          <span>{conversation.unreadCount > 0 && `${conversation.unreadCount} unread`}</span>
                          <span>{format(new Date(conversation.lastMessageAt), 'MMM d')}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </ScrollArea>
            </CardContent>
          </Card>
        </div>

        {/* Message Thread & Details */}
        <div className="lg:col-span-2">
          <Card className="h-full">
            <CardHeader>
              <div className="flex items-center justify-between">
                <div className="flex-1">
                  <CardTitle>{selectedConversation ? selectedConversation.subject : 'Select a conversation'}</CardTitle>
                  {selectedConversation && (
                    <p className="text-sm text-muted-foreground mt-1">{selectedConversation.clientName}</p>
                  )}
                </div>
                {selectedConversation && (
                  <div className="flex items-center gap-2">
                    <Select value={selectedConversation.status} onValueChange={handleStatusChange}>
                      <SelectTrigger className="w-[140px]">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Open">Open</SelectItem>
                        <SelectItem value="Pending">Pending</SelectItem>
                        <SelectItem value="Urgent">Urgent</SelectItem>
                        <SelectItem value="Closed">Closed</SelectItem>
                      </SelectContent>
                    </Select>
                    <AssignmentSelector
                      assignedTo={selectedConversation.assignedTo}
                      onAssign={handleAssignConversation}
                    />
                  </div>
                )}
              </div>
            </CardHeader>
            <CardContent>
              {selectedConversation ? (
                <div className="space-y-4">
                  <ScrollArea className="h-[450px]">
                    <div className="space-y-4 pr-4">
                      {messages.map((message) => (
                        <div
                          key={message.id}
                          className={`p-4 rounded-lg ${
                            message.isInternal
                              ? 'bg-amber-50 border border-amber-200'
                              : 'bg-muted'
                          }`}
                        >
                          <div className="flex items-start gap-3">
                            <Avatar className="w-8 h-8">
                              <AvatarFallback>
                                {message.senderName
                                  .split(' ')
                                  .map((n) => n[0])
                                  .join('')}
                              </AvatarFallback>
                            </Avatar>
                            <div className="flex-1">
                              <div className="flex items-center gap-2 mb-1">
                                <span className="font-medium text-sm">{message.senderName}</span>
                                {message.isInternal && (
                                  <Badge variant="outline" className="gap-1">
                                    <Lock className="w-3 h-3" />
                                    Internal
                                  </Badge>
                                )}
                                <span className="text-xs text-muted-foreground">
                                  {format(new Date(message.sentAt), 'MMM d, h:mm a')}
                                </span>
                              </div>
                              <p className="text-sm whitespace-pre-wrap">{message.body}</p>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </ScrollArea>

                  <Separator />

                  <div className="space-y-3">
                    <div className="flex items-center gap-2">
                      <Switch
                        id="internal-note"
                        checked={replyForm.isInternal}
                        onCheckedChange={(checked) =>
                          setReplyForm((prev) => ({ ...prev, isInternal: checked }))
                        }
                      />
                      <Label htmlFor="internal-note" className="flex items-center gap-1">
                        <Lock className="w-4 h-4" />
                        Internal Note (not visible to client)
                      </Label>
                    </div>
                    <Textarea
                      placeholder="Type your message..."
                      value={replyForm.body}
                      onChange={(e) => setReplyForm((prev) => ({ ...prev, body: e.target.value }))}
                      rows={3}
                    />
                    <Button onClick={handleSendReply} disabled={!replyForm.body.trim()}>
                      <Send className="w-4 h-4 mr-2" />
                      {replyForm.isInternal ? 'Add Internal Note' : 'Send Message'}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-center h-[550px]">
                  <div className="text-center">
                    <MessageSquare className="h-16 w-16 mx-auto mb-4 text-muted" />
                    <h3 className="text-lg font-medium mb-2">No conversation selected</h3>
                    <p className="text-muted-foreground">Choose a conversation to view messages</p>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

