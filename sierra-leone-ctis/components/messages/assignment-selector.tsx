'use client'

import { useState, useEffect } from 'react'
import { MessageService } from '@/lib/services/message-service'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { UserPlus } from 'lucide-react'

interface AssignmentSelectorProps {
  assignedTo?: string
  onAssign: (userId: string) => void
}

export default function AssignmentSelector({ assignedTo, onAssign }: AssignmentSelectorProps) {
  const [staffUsers, setStaffUsers] = useState<Array<{ id: string; name: string }>>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadStaffUsers()
  }, [])

  const loadStaffUsers = async () => {
    try {
      setLoading(true)
      const users = await MessageService.getStaffUsers()
      setStaffUsers(users)
    } catch (error) {
      console.error('Error loading staff users:', error)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Select value={assignedTo || 'unassigned'} onValueChange={onAssign} disabled={loading}>
      <SelectTrigger className="w-[180px]">
        <div className="flex items-center gap-2">
          <UserPlus className="w-4 h-4" />
          <SelectValue placeholder="Assign to..." />
        </div>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="unassigned">Unassigned</SelectItem>
        {staffUsers.map((user) => (
          <SelectItem key={user.id} value={user.id}>
            {user.name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}

