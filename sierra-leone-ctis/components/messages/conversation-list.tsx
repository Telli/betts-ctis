'use client'

import { Conversation } from '@/lib/services/message-service'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { MessageSquare } from 'lucide-react'
import { format } from 'date-fns'

interface ConversationListProps {
  conversations: Conversation[]
  selectedConversation: Conversation | null
  onSelect: (conversation: Conversation) => void
  loading: boolean
}

export default function ConversationList({
  conversations,
  selectedConversation,
  onSelect,
  loading,
}: ConversationListProps) {
  const getStatusBadge = (status: string) => {
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

  if (loading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
      </div>
    )
  }

  if (conversations.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        <MessageSquare className="h-12 w-12 mx-auto mb-4 text-muted" />
        <p>No conversations found</p>
      </div>
    )
  }

  return (
    <ScrollArea className="h-[600px]">
      <div className="space-y-2">
        {conversations.map((conversation) => (
          <div
            key={conversation.id}
            className={`p-3 rounded-lg border cursor-pointer transition-colors ${
              selectedConversation?.id === conversation.id
                ? 'bg-primary/5 border-primary'
                : 'hover:bg-muted'
            }`}
            onClick={() => onSelect(conversation)}
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
    </ScrollArea>
  )
}

