'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import { useAuth } from '@/context/auth-context';
import { 
  Loader2, 
  Shield, 
  Search,
  Filter,
  Clock,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Eye,
  FileText,
  CreditCard,
  Upload,
  User
} from 'lucide-react';
import Link from 'next/link';
import { 
  AssociatePermissionService,
  type AssociateClientPermissionDto,
  AssociatePermissionLevel
} from '@/lib/services';

export default function AssociatePermissionsPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [permissions, setPermissions] = useState<AssociateClientPermissionDto[]>([]);
  const [filteredPermissions, setFilteredPermissions] = useState<AssociateClientPermissionDto[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedArea, setSelectedArea] = useState<string>('all');
  const [selectedStatus, setSelectedStatus] = useState<string>('all');
  const { user } = useAuth();
  const { toast } = useToast();

  useEffect(() => {
    if (user?.id) {
      loadPermissions();
    }
  }, [user]);

  useEffect(() => {
    filterPermissions();
  }, [permissions, searchTerm, selectedArea, selectedStatus]);

  const loadPermissions = async () => {
    if (!user?.id) return;

    try {
      setIsLoading(true);
      const response = await AssociatePermissionService.getAssociatePermissions(user.id);
      setPermissions(response.data);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load permissions',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const filterPermissions = () => {
    let filtered = permissions;

    // Filter by search term
    if (searchTerm) {
      filtered = filtered.filter(permission => 
        permission.clientName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        permission.permissionArea.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    // Filter by area
    if (selectedArea !== 'all') {
      filtered = filtered.filter(permission => permission.permissionArea === selectedArea);
    }

    // Filter by status
    if (selectedStatus !== 'all') {
      filtered = filtered.filter(permission => {
        const isExpired = permission.expiryDate && new Date(permission.expiryDate) < new Date();
        const isExpiring = permission.expiryDate && 
          new Date(permission.expiryDate) > new Date() && 
          new Date(permission.expiryDate) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
        
        switch (selectedStatus) {
          case 'active':
            return permission.isActive && !isExpired && !isExpiring;
          case 'expiring':
            return isExpiring;
          case 'expired':
            return isExpired;
          case 'inactive':
            return !permission.isActive;
          default:
            return true;
        }
      });
    }

    setFilteredPermissions(filtered);
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
    if (!permission.isActive) {
      return <XCircle className="h-4 w-4 text-gray-500" />;
    }
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date()) {
      return <XCircle className="h-4 w-4 text-red-500" />;
    }
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)) {
      return <AlertTriangle className="h-4 w-4 text-yellow-500" />;
    }
    return <CheckCircle className="h-4 w-4 text-green-500" />;
  };

  const getStatusText = (permission: AssociateClientPermissionDto) => {
    if (!permission.isActive) {
      return { text: 'Inactive', color: 'text-gray-600' };
    }
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date()) {
      return { text: 'Expired', color: 'text-red-600' };
    }
    if (permission.expiryDate && new Date(permission.expiryDate) < new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)) {
      return { text: 'Expiring Soon', color: 'text-yellow-600' };
    }
    return { text: 'Active', color: 'text-green-600' };
  };

  const getAreaIcon = (area: string) => {
    switch (area.toLowerCase()) {
      case 'taxfilings':
        return <FileText className="h-4 w-4" />;
      case 'documents':
        return <Upload className="h-4 w-4" />;
      case 'payments':
        return <CreditCard className="h-4 w-4" />;
      case 'clientprofile':
        return <User className="h-4 w-4" />;
      default:
        return <Shield className="h-4 w-4" />;
    }
  };

  const getExpiryText = (expiryDate?: string) => {
    if (!expiryDate) return 'No expiry';
    
    const expiry = new Date(expiryDate);
    const now = new Date();
    const daysUntilExpiry = Math.ceil((expiry.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    
    if (daysUntilExpiry < 0) {
      return `Expired ${Math.abs(daysUntilExpiry)} days ago`;
    } else if (daysUntilExpiry === 0) {
      return 'Expires today';
    } else if (daysUntilExpiry <= 7) {
      return `Expires in ${daysUntilExpiry} day${daysUntilExpiry !== 1 ? 's' : ''}`;
    } else {
      return `Expires ${expiry.toLocaleDateString()}`;
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">My Permissions</h1>
          <p className="text-muted-foreground">
            Manage and view your delegated client permissions
          </p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Permissions</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{permissions.length}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active</CardTitle>
            <CheckCircle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {permissions.filter(p => p.isActive && (!p.expiryDate || new Date(p.expiryDate) > new Date())).length}
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Expiring Soon</CardTitle>
            <AlertTriangle className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">
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
            <div className="text-2xl font-bold text-red-600">
              {permissions.filter(p => p.expiryDate && new Date(p.expiryDate) < new Date()).length}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filter Permissions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col space-y-4 md:flex-row md:space-y-0 md:space-x-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by client name or permission area..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8"
                />
              </div>
            </div>
            
            <Select value={selectedArea} onValueChange={setSelectedArea}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="All Areas" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Areas</SelectItem>
                <SelectItem value="TaxFilings">Tax Filings</SelectItem>
                <SelectItem value="Documents">Documents</SelectItem>
                <SelectItem value="Payments">Payments</SelectItem>
                <SelectItem value="ClientProfile">Client Profile</SelectItem>
              </SelectContent>
            </Select>
            
            <Select value={selectedStatus} onValueChange={setSelectedStatus}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="active">Active</SelectItem>
                <SelectItem value="expiring">Expiring Soon</SelectItem>
                <SelectItem value="expired">Expired</SelectItem>
                <SelectItem value="inactive">Inactive</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Permissions List */}
      <Card>
        <CardHeader>
          <CardTitle>Permission Details</CardTitle>
          <CardDescription>
            {filteredPermissions.length} of {permissions.length} permissions shown
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {filteredPermissions.map((permission) => {
              const status = getStatusText(permission);
              return (
                <div key={permission.id} className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors">
                  <div className="flex items-center space-x-4">
                    {getStatusIcon(permission)}
                    <div className="flex items-center space-x-2">
                      {getAreaIcon(permission.permissionArea)}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-1">
                        <h3 className="font-medium">{permission.clientName}</h3>
                        <Badge variant="outline">{permission.permissionArea}</Badge>
                        {getPermissionLevelBadge(permission.level)}
                      </div>
                      <div className="flex items-center space-x-4 text-sm text-muted-foreground">
                        <span className={status.color}>
                          {status.text}
                        </span>
                        <span>•</span>
                        <span>{getExpiryText(permission.expiryDate)}</span>
                        <span>•</span>
                        <span>Granted: {new Date(permission.grantedDate).toLocaleDateString()}</span>
                        {permission.notes && (
                          <>
                            <span>•</span>
                            <span>{permission.notes}</span>
                          </>
                        )}
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex space-x-2">
                    <Link href={`/clients/${permission.clientId}`}>
                      <Button size="sm" variant="outline">
                        <Eye className="mr-1 h-3 w-3" />
                        View Client
                      </Button>
                    </Link>
                  </div>
                </div>
              );
            })}
            
            {filteredPermissions.length === 0 && (
              <div className="text-center py-8 text-muted-foreground">
                {permissions.length === 0 
                  ? 'No permissions assigned to you yet'
                  : 'No permissions match your current filters'
                }
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}