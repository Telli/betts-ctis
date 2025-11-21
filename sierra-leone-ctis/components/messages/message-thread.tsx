'use client'

import { Message } from '@/lib/services/message-service'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Lock } from 'lucide-react'
import { format } from 'date-fns'

interface MessageThreadProps {
  messages: Message[]
}

export default function MessageThread({ messages }: MessageThreadProps) {
  return (
    <ScrollArea className="h-[450px]">
      <div className="space-y-4 pr-4">
        {messages.map((message) => (
          <div
            key={message.id}
            className={`p-4 rounded-lg ${
              message.isInternal ? 'bg-amber-50 border border-amber-200' : 'bg-muted'
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
  )
}

