'use client'

import { Badge } from '@/components/ui/badge'
import { Lock } from 'lucide-react'

export default function InternalNoteBadge() {
  return (
    <Badge variant="outline" className="gap-1 bg-amber-50 text-amber-800 border-amber-200">
      <Lock className="w-3 h-3" />
      Internal
    </Badge>
  )
}

