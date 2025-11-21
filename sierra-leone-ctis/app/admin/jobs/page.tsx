'use client'

import { useState, useEffect } from 'react'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/hooks/use-toast'
import { AdminService, JobStatus } from '@/lib/services/admin-service'
import { Play, Square, RotateCw, Activity, Clock, CheckCircle, XCircle } from 'lucide-react'
import Loading from '@/app/loading'

export default function JobsPage() {
  const { toast } = useToast()
  const [jobs, setJobs] = useState<JobStatus[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadJobs()
    const interval = setInterval(loadJobs, 30000) // Refresh every 30 seconds
    return () => clearInterval(interval)
  }, [])

  const loadJobs = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getJobs()
      setJobs(data)
    } catch (error) {
      console.error('Error loading jobs:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load jobs',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleStartJob = async (name: string) => {
    try {
      await AdminService.startJob(name)
      toast({
        title: 'Success',
        description: `Job ${name} started successfully`,
      })
      loadJobs()
    } catch (error) {
      console.error('Error starting job:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to start job',
      })
    }
  }

  const handleStopJob = async (name: string) => {
    try {
      await AdminService.stopJob(name)
      toast({
        title: 'Success',
        description: `Job ${name} stopped successfully`,
      })
      loadJobs()
    } catch (error) {
      console.error('Error stopping job:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to stop job',
      })
    }
  }

  const handleRestartJob = async (name: string) => {
    try {
      await AdminService.restartJob(name)
      toast({
        title: 'Success',
        description: `Job ${name} restarted successfully`,
      })
      loadJobs()
    } catch (error) {
      console.error('Error restarting job:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to restart job',
      })
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'Running':
        return (
          <Badge className="bg-green-100 text-green-800 flex items-center gap-1">
            <CheckCircle className="w-3 h-3" />
            Running
          </Badge>
        )
      case 'Stopped':
        return (
          <Badge className="bg-gray-100 text-gray-800 flex items-center gap-1">
            <Square className="w-3 h-3" />
            Stopped
          </Badge>
        )
      case 'Error':
        return (
          <Badge className="bg-red-100 text-red-800 flex items-center gap-1">
            <XCircle className="w-3 h-3" />
            Error
          </Badge>
        )
      default:
        return <Badge>{status}</Badge>
    }
  }

  if (loading && jobs.length === 0) {
    return <Loading />
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="Jobs Monitor"
        breadcrumbs={[{ label: 'Admin' }, { label: 'Jobs' }]}
        actions={
          <Button onClick={loadJobs} variant="outline">
            <RotateCw className="w-4 h-4 mr-2" />
            Refresh
          </Button>
        }
      />

      <div className="flex-1 p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {jobs.map((job) => (
            <Card key={job.name}>
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{job.name}</CardTitle>
                  {getStatusBadge(job.status)}
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Last Run:</span>
                    <span className="font-mono">
                      {job.lastRun ? new Date(job.lastRun).toLocaleString() : 'Never'}
                    </span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Next Run:</span>
                    <span className="font-mono">
                      {job.nextRun ? new Date(job.nextRun).toLocaleString() : 'N/A'}
                    </span>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">Queue Size:</span>
                    <Badge variant="outline">{job.queueSize}</Badge>
                  </div>
                </div>

                <div className="flex items-center gap-2">
                  {job.status === 'Running' ? (
                    <>
                      <Button
                        variant="outline"
                        size="sm"
                        className="flex-1"
                        onClick={() => handleStopJob(job.name)}
                      >
                        <Square className="w-4 h-4 mr-1" />
                        Stop
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="flex-1"
                        onClick={() => handleRestartJob(job.name)}
                      >
                        <RotateCw className="w-4 h-4 mr-1" />
                        Restart
                      </Button>
                    </>
                  ) : (
                    <Button
                      variant="outline"
                      size="sm"
                      className="flex-1"
                      onClick={() => handleStartJob(job.name)}
                    >
                      <Play className="w-4 h-4 mr-1" />
                      Start
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}

