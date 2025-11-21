'use client'

import { useState, useEffect } from 'react'
import { PageHeader } from '@/components/page-header'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'
import { useToast } from '@/hooks/use-toast'
import { AdminService, User, InviteUserDto } from '@/lib/services/admin-service'
import { UserPlus, Edit, Trash, CheckCircle, XCircle, Mail, Shield } from 'lucide-react'
import InviteUserDialog from '@/components/admin/invite-user-dialog'
import EditUserDialog from '@/components/admin/edit-user-dialog'
import Loading from '@/app/loading'

export default function UsersManagementPage() {
  const { toast } = useToast()
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const [showInviteDialog, setShowInviteDialog] = useState(false)
  const [editingUser, setEditingUser] = useState<User | null>(null)

  useEffect(() => {
    loadUsers()
  }, [])

  const loadUsers = async () => {
    try {
      setLoading(true)
      const data = await AdminService.getUsers()
      setUsers(data)
    } catch (error) {
      console.error('Error loading users:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to load users',
      })
    } finally {
      setLoading(false)
    }
  }

  const handleInviteUser = async (data: InviteUserDto) => {
    try {
      await AdminService.inviteUser(data)
      toast({
        title: 'Success',
        description: 'User invited successfully',
      })
      setShowInviteDialog(false)
      loadUsers()
    } catch (error) {
      console.error('Error inviting user:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to invite user',
      })
    }
  }

  const handleUpdateUser = async (userId: string, updates: any) => {
    try {
      await AdminService.updateUser(userId, updates)
      toast({
        title: 'Success',
        description: 'User updated successfully',
      })
      setEditingUser(null)
      loadUsers()
    } catch (error) {
      console.error('Error updating user:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to update user',
      })
    }
  }

  const handleToggleStatus = async (userId: string, currentStatus: boolean) => {
    try {
      await AdminService.updateUserStatus(userId, !currentStatus)
      toast({
        title: 'Success',
        description: 'User status updated',
      })
      loadUsers()
    } catch (error) {
      console.error('Error updating user status:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to update user status',
      })
    }
  }

  const handleDeleteUser = async (userId: string) => {
    if (!confirm('Are you sure you want to delete this user?')) return

    try {
      await AdminService.deleteUser(userId)
      toast({
        title: 'Success',
        description: 'User deleted successfully',
      })
      loadUsers()
    } catch (error) {
      console.error('Error deleting user:', error)
      toast({
        variant: 'destructive',
        title: 'Error',
        description: 'Failed to delete user',
      })
    }
  }

  const getRoleBadge = (role: string) => {
    switch (role) {
      case 'Admin':
        return <Badge className="bg-red-100 text-red-800">Admin</Badge>
      case 'Associate':
        return <Badge className="bg-blue-100 text-blue-800">Associate</Badge>
      case 'Client':
        return <Badge className="bg-green-100 text-green-800">Client</Badge>
      default:
        return <Badge>{role}</Badge>
    }
  }

  if (loading) {
    return <Loading />
  }

  return (
    <div className="flex-1 flex flex-col">
      <PageHeader
        title="User Management"
        breadcrumbs={[{ label: 'Admin' }, { label: 'Users' }]}
        actions={
          <Button onClick={() => setShowInviteDialog(true)}>
            <UserPlus className="w-4 h-4 mr-2" />
            Invite User
          </Button>
        }
      />

      <div className="flex-1 p-6">
        <Card>
          <CardHeader>
            <CardTitle>All Users</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="border rounded-lg">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Name</TableHead>
                    <TableHead>Email</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Last Login</TableHead>
                    <TableHead className="text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {users.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                        No users found
                      </TableCell>
                    </TableRow>
                  ) : (
                    users.map((user) => (
                      <TableRow key={user.id}>
                        <TableCell>
                          <div>
                            <p className="font-medium">{user.name}</p>
                            {user.companyName && (
                              <p className="text-sm text-muted-foreground">{user.companyName}</p>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-2">
                            <Mail className="w-4 h-4 text-muted-foreground" />
                            {user.email}
                          </div>
                        </TableCell>
                        <TableCell>{getRoleBadge(user.role)}</TableCell>
                        <TableCell>
                          <button
                            onClick={() => handleToggleStatus(user.id, user.isActive)}
                            className="flex items-center gap-1"
                          >
                            {user.isActive ? (
                              <>
                                <CheckCircle className="w-4 h-4 text-green-600" />
                                <span className="text-green-600">Active</span>
                              </>
                            ) : (
                              <>
                                <XCircle className="w-4 h-4 text-red-600" />
                                <span className="text-red-600">Inactive</span>
                              </>
                            )}
                          </button>
                        </TableCell>
                        <TableCell>
                          {user.lastLogin ? new Date(user.lastLogin).toLocaleDateString() : 'Never'}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            <Button variant="ghost" size="sm" onClick={() => setEditingUser(user)}>
                              <Edit className="w-4 h-4" />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => handleDeleteUser(user.id)}
                            >
                              <Trash className="w-4 h-4 text-red-500" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>

      <InviteUserDialog
        open={showInviteDialog}
        onClose={() => setShowInviteDialog(false)}
        onInvite={handleInviteUser}
      />

      {editingUser && (
        <EditUserDialog
          open={!!editingUser}
          user={editingUser}
          onClose={() => setEditingUser(null)}
          onUpdate={(updates) => handleUpdateUser(editingUser.id, updates)}
        />
      )}
    </div>
  )
}

