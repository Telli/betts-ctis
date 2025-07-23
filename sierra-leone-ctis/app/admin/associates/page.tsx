'use client';

import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { useToast } from '@/hooks/use-toast';
import { 
  Loader2, 
  Users, 
  UserPlus, 
  Settings, 
  Eye, 
  Edit, 
  Trash2, 
  Clock,
  AlertTriangle,
  CheckCircle,
  XCircle
} from 'lucide-react';
import { 
  AssociatePermissionService, 
  ClientDelegationService,
  type AssociateDto, 
  type AssociateClientPermissionDto,
  type GrantPermissionRequest,
  AssociatePermissionLevel
} from '@/lib/services';

const grantPermissionSchema = z.object({
  associateId: z.string().min(1, 'Associate is required'),
  clientId: z.number().min(1, 'Client is required'),
  permissionArea: z.string().min(1, 'Permission area is required'),
  permissionLevel: z.nativeEnum(AssociatePermissionLevel),
  expiryDate: z.string().optional(),
  reason: z.string().min(1, 'Reason is required'),
});

type GrantPermissionFormData = z.infer<typeof grantPermissionSchema>;

export default function AssociateManagementPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [associates, setAssociates] = useState<AssociateDto[]>([]);
  const [permissions, setPermissions] = useState<AssociateClientPermissionDto[]>([]);
  const [selectedAssociate, setSelectedAssociate] = useState<string | null>(null);
  const [isGrantDialogOpen, setIsGrantDialogOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { toast } = useToast();

  const grantForm = useForm<GrantPermissionFormData>({
    resolver: zodResolver(grantPermissionSchema),
    defaultValues: {
      associateId: '',
      clientId: 0,
      permissionArea: 'TaxFilings',
      permissionLevel: AssociatePermissionLevel.Read,
      expiryDate: '',
      reason: '',
    },
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setIsLoading(true);
      const associatesResponse = await ClientDelegationService.getAvailableAssociates();
      setAssociates(associatesResponse.data);
      
      // Don't load all permissions initially - only when selecting an associate
      setPermissions([]);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load associate data',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const loadAssociatePermissions = async (associateId: string) => {
    try {
      const response = await AssociatePermissionService.getAssociatePermissions(associateId);
      setPermissions(response.data);
      setSelectedAssociate(associateId);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load associate permissions',
        variant: 'destructive',
      });
    }
  };

  const onGrantPermission = async (data: GrantPermissionFormData) => {
    try {
      setIsSubmitting(true);
      
      const request: GrantPermissionRequest = {
        associateId: data.associateId,
        clientIds: [data.clientId],
        permissionArea: data.permissionArea,
        level: data.permissionLevel,
        expiryDate: data.expiryDate || undefined,
        notes: data.reason,
      };

      await AssociatePermissionService.grantPermission(request);
      
      toast({
        title: 'Permission Granted',
        description: 'Associate permission has been granted successfully',
      });
      
      setIsGrantDialogOpen(false);
      grantForm.reset();
      
      // Reload data
      if (selectedAssociate) {
        await loadAssociatePermissions(selectedAssociate);
      } else {
        await loadData();
      }
    } catch (error: any) {
      toast({
        title: 'Error',
        description: error.message || 'Failed to grant permission',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const revokePermission = async (associateId: string, clientId: number, area: string) => {
    try {
      await AssociatePermissionService.revokePermission(associateId, clientId, area, 'Revoked by admin');
      
      toast({
        title: 'Permission Revoked',
        description: 'Associate permission has been revoked successfully',
      });
      
      // Reload permissions
      if (selectedAssociate) {
        await loadAssociatePermissions(selectedAssociate);
      } else {
        await loadData();
      }
    } catch (error: any) {
      toast({
        title: 'Error',
        description: error.message || 'Failed to revoke permission',
        variant: 'destructive',
      });
    }
  };

  const getPermissionLevelBadge = (level: AssociatePermissionLevel) => {
    const levelMap: Record<AssociatePermissionLevel, { label: string; variant: 'secondary' | 'default' | 'destructive' }> = {
      [AssociatePermissionLevel.None]: { label: 'None', variant: 'secondary' as const },
      [AssociatePermissionLevel.Read]: { label: 'Read', variant: 'secondary' as const },
      [AssociatePermissionLevel.Create]: { label: 'Create', variant: 'default' as const },
      [AssociatePermissionLevel.Update]: { label: 'Update', variant: 'default' as const },
      [AssociatePermissionLevel.Delete]: { label: 'Delete', variant: 'destructive' as const },
      [AssociatePermissionLevel.Submit]: { label: 'Submit', variant: 'default' as const },
      [AssociatePermissionLevel.Approve]: { label: 'Approve', variant: 'default' as const },
      [AssociatePermissionLevel.All]: { label: 'All', variant: 'default' as const },
    };

    const config = levelMap[level] || { label: 'Unknown', variant: 'secondary' as const };
    
    return (
      <Badge variant={config.variant}>
        {config.label}
      </Badge>
    );
  };

  const getStatusIcon = (permission: AssociateClientPermissionDto) => {
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date()) {
      return <XCircle className="h-4 w-4 text-red-500" />;
    }
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)) {
      return <AlertTriangle className="h-4 w-4 text-yellow-500" />;
    }
    return <CheckCircle className="h-4 w-4 text-green-500" />;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  const filteredPermissions = selectedAssociate 
    ? permissions.filter(p => p.associateId === selectedAssociate)
    : permissions;

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-2">
          <Users className="h-6 w-6" />
          <h1 className="text-3xl font-bold">Associate Management</h1>
        </div>
        
        <Dialog open={isGrantDialogOpen} onOpenChange={setIsGrantDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <UserPlus className="mr-2 h-4 w-4" />
              Grant Permission
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-[500px]">
            <DialogHeader>
              <DialogTitle>Grant Associate Permission</DialogTitle>
              <DialogDescription>
                Grant permission to an associate for a specific client and area.
              </DialogDescription>
            </DialogHeader>
            
            <form onSubmit={grantForm.handleSubmit(onGrantPermission)} className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="associateId">Associate</Label>
                <Select 
                  value={grantForm.watch('associateId')} 
                  onValueChange={(value) => grantForm.setValue('associateId', value)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select associate" />
                  </SelectTrigger>
                  <SelectContent>
                    {associates.map((associate) => (
                      <SelectItem key={associate.id} value={associate.id}>
                        {associate.firstName} {associate.lastName} ({associate.email})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {grantForm.formState.errors.associateId && (
                  <p className="text-sm text-red-600">{grantForm.formState.errors.associateId.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="clientId">Client ID</Label>
                <Input
                  id="clientId"
                  type="number"
                  placeholder="Enter client ID"
                  {...grantForm.register('clientId', { valueAsNumber: true })}
                />
                {grantForm.formState.errors.clientId && (
                  <p className="text-sm text-red-600">{grantForm.formState.errors.clientId.message}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="permissionArea">Permission Area</Label>
                <Select 
                  value={grantForm.watch('permissionArea')} 
                  onValueChange={(value) => grantForm.setValue('permissionArea', value)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="TaxFilings">Tax Filings</SelectItem>
                    <SelectItem value="Documents">Documents</SelectItem>
                    <SelectItem value="Payments">Payments</SelectItem>
                    <SelectItem value="ClientProfile">Client Profile</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="permissionLevel">Permission Level</Label>
                <Select 
                  value={grantForm.watch('permissionLevel').toString()} 
                  onValueChange={(value) => grantForm.setValue('permissionLevel', parseInt(value) as AssociatePermissionLevel)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={AssociatePermissionLevel.Read.toString()}>Read</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.Create.toString()}>Create</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.Update.toString()}>Update</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.Delete.toString()}>Delete</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.Submit.toString()}>Submit</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.Approve.toString()}>Approve</SelectItem>
                    <SelectItem value={AssociatePermissionLevel.All.toString()}>All</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="expiryDate">Expiry Date (Optional)</Label>
                <Input
                  id="expiryDate"
                  type="date"
                  {...grantForm.register('expiryDate')}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="reason">Reason</Label>
                <Input
                  id="reason"
                  placeholder="Enter reason for granting permission"
                  {...grantForm.register('reason')}
                />
                {grantForm.formState.errors.reason && (
                  <p className="text-sm text-red-600">{grantForm.formState.errors.reason.message}</p>
                )}
              </div>

              <div className="flex justify-end space-x-2">
                <Button 
                  type="button" 
                  variant="outline" 
                  onClick={() => setIsGrantDialogOpen(false)}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isSubmitting}>
                  {isSubmitting ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Granting...
                    </>
                  ) : (
                    'Grant Permission'
                  )}
                </Button>
              </div>
            </form>
          </DialogContent>
        </Dialog>
      </div>

      <Tabs defaultValue="overview" className="space-y-4">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="associates">Associates</TabsTrigger>
          <TabsTrigger value="permissions">Permissions</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Associates</CardTitle>
                <Users className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{associates.length}</div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Active Permissions</CardTitle>
                <CheckCircle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {permissions.filter(p => !p.expiryDate || new Date(p.expiryDate) > new Date()).length}
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Expiring Soon</CardTitle>
                <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {permissions.filter(p => 
                    p.expiryDate && 
                    new Date(p.expiryDate) > new Date() && 
                    new Date(p.expiryDate) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
                  ).length}
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Expired</CardTitle>
                <XCircle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {permissions.filter(p => p.expiryDate && new Date(p.expiryDate) < new Date()).length}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="associates" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {associates.map((associate) => (
              <Card key={associate.id} className="cursor-pointer hover:shadow-md transition-shadow">
                <CardHeader>
                  <CardTitle className="text-lg">
                    {associate.firstName} {associate.lastName}
                  </CardTitle>
                  <CardDescription>{associate.email}</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                      <span>Clients:</span>
                      <span>{associate.clientCount || 0}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Permissions:</span>
                      <span>{associate.permissionCount || 0}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Status:</span>
                      <Badge variant={associate.isActive ? 'default' : 'secondary'}>
                        {associate.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                    </div>
                  </div>
                  
                  <div className="flex space-x-2 mt-4">
                    <Button 
                      size="sm" 
                      variant="outline"
                      onClick={() => loadAssociatePermissions(associate.id)}
                    >
                      <Eye className="mr-1 h-3 w-3" />
                      View
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="permissions" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Associate Permissions</CardTitle>
              <CardDescription>
                {selectedAssociate 
                  ? `Showing permissions for ${associates.find(a => a.id === selectedAssociate)?.firstName} ${associates.find(a => a.id === selectedAssociate)?.lastName}`
                  : 'Showing all permissions'
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {filteredPermissions.map((permission) => (
                  <div key={permission.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center space-x-4">
                      {getStatusIcon(permission)}
                      <div>
                        <div className="flex items-center space-x-2">
                          <span className="font-medium">Client {permission.clientId}</span>
                          <Badge variant="outline">{permission.permissionArea}</Badge>
                          {getPermissionLevelBadge(permission.level)}
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {permission.expiryDate ? (
                            <>Expires: {new Date(permission.expiryDate).toLocaleDateString()}</>
                          ) : (
                            'No expiry'
                          )}
                        </div>
                      </div>
                    </div>
                    
                    <div className="flex space-x-2">
                      <Button 
                        size="sm" 
                        variant="outline"
                        onClick={() => revokePermission(permission.associateId, permission.clientId, permission.permissionArea)}
                      >
                        <Trash2 className="mr-1 h-3 w-3" />
                        Revoke
                      </Button>
                    </div>
                  </div>
                ))}
                
                {filteredPermissions.length === 0 && (
                  <div className="text-center py-8 text-muted-foreground">
                    No permissions found
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}