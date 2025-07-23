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
  Users, 
  Search,
  Building2,
  Mail,
  Phone,
  FileText,
  CreditCard,
  Calendar,
  Activity,
  Eye,
  Clock
} from 'lucide-react';
import Link from 'next/link';
import { 
  AssociatePermissionService,
  OnBehalfActionService,
  type ClientSummaryDto
} from '@/lib/services';

interface ClientWithStats extends ClientSummaryDto {
  contactPerson?: string;
  email?: string;
  phoneNumber?: string;
  tin?: string;
  taxpayerCategory?: string;
  clientType?: string;
  annualTurnover?: number;
  complianceScore?: number;
  isActive?: boolean;
  recentActionsCount: number;
  upcomingDeadlinesCount: number;
  lastActionDate?: string;
  nextDeadline?: string;
}

export default function AssociateClientsPage() {
  const [isLoading, setIsLoading] = useState(true);
  const [clients, setClients] = useState<ClientWithStats[]>([]);
  const [filteredClients, setFilteredClients] = useState<ClientWithStats[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedArea, setSelectedArea] = useState<string>('TaxFilings');
  const { user } = useAuth();
  const { toast } = useToast();

  useEffect(() => {
    if (user?.id) {
      loadClients();
    }
  }, [user, selectedArea]);

  useEffect(() => {
    filterClients();
  }, [clients, searchTerm, selectedCategory]);

  const loadClients = async () => {
    if (!user?.id) return;

    try {
      setIsLoading(true);
      
      // Make API call to get delegated client summaries
      const response = await fetch(`/api/associate-dashboard/${user.id}/clients?area=${selectedArea}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('auth_token')}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to load clients');
      }

      const result = await response.json();
      setClients(result.data);
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load delegated clients',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const filterClients = () => {
    let filtered = clients;

    // Filter by search term
    if (searchTerm) {
      filtered = filtered.filter(client => 
        client.businessName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        client.clientName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (client.contactPerson && client.contactPerson.toLowerCase().includes(searchTerm.toLowerCase()))
      );
    }

    // Filter by taxpayer category
    if (selectedCategory !== 'all') {
      filtered = filtered.filter(client => client.taxpayerCategory === selectedCategory);
    }

    setFilteredClients(filtered);
  };

  const getTaxpayerCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'large':
        return 'bg-red-100 text-red-800';
      case 'medium':
        return 'bg-orange-100 text-orange-800';
      case 'small':
        return 'bg-yellow-100 text-yellow-800';
      case 'micro':
        return 'bg-green-100 text-green-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getClientTypeColor = (clientType: string) => {
    switch (clientType.toLowerCase()) {
      case 'individual':
        return 'bg-blue-100 text-blue-800';
      case 'business':
        return 'bg-green-100 text-green-800';
      case 'partnership':
        return 'bg-purple-100 text-purple-800';
      case 'corporation':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-SL', {
      style: 'currency',
      currency: 'SLE',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(amount);
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
          <h1 className="text-3xl font-bold">My Delegated Clients</h1>
          <p className="text-muted-foreground">
            Clients you have permissions to manage
          </p>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Clients</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{clients.length}</div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Active Clients</CardTitle>
            <Building2 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {clients.filter(c => c.isActive ?? true).length}
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">With Deadlines</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-orange-600">
              {clients.filter(c => c.upcomingDeadlinesCount > 0).length}
            </div>
          </CardContent>
        </Card>
        
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Recent Activity</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">
              {clients.filter(c => c.recentActionsCount > 0).length}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Filter Clients</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col space-y-4 md:flex-row md:space-y-0 md:space-x-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Search by business name, contact person, or TIN..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-8"
                />
              </div>
            </div>
            
            <Select value={selectedArea} onValueChange={setSelectedArea}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Permission Area" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="TaxFilings">Tax Filings</SelectItem>
                <SelectItem value="Documents">Documents</SelectItem>
                <SelectItem value="Payments">Payments</SelectItem>
                <SelectItem value="ClientProfile">Client Profile</SelectItem>
              </SelectContent>
            </Select>
            
            <Select value={selectedCategory} onValueChange={setSelectedCategory}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="All Categories" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Categories</SelectItem>
                <SelectItem value="Large">Large</SelectItem>
                <SelectItem value="Medium">Medium</SelectItem>
                <SelectItem value="Small">Small</SelectItem>
                <SelectItem value="Micro">Micro</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Client List */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {filteredClients.map((client) => (
          <Card key={client.clientId} className="hover:shadow-md transition-shadow">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg">{client.businessName}</CardTitle>
                <div className="flex space-x-1">
                  {!client.isActive && (
                    <Badge variant="secondary">Inactive</Badge>
                  )}
                  {client.upcomingDeadlinesCount > 0 && (
                    <Badge variant="destructive" className="text-xs">
                      <Clock className="mr-1 h-3 w-3" />
                      {client.upcomingDeadlinesCount}
                    </Badge>
                  )}
                </div>
              </div>
              <CardDescription>
                <div className="flex items-center space-x-2">
                  <span>{client.contactPerson || 'Contact not available'}</span>
                  {client.taxpayerCategory && (
                    <Badge 
                      variant="outline" 
                      className={getTaxpayerCategoryColor(client.taxpayerCategory)}
                    >
                      {client.taxpayerCategory}
                    </Badge>
                  )}
                </div>
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {/* Contact Information */}
                <div className="space-y-2">
                  {client.email && (
                    <div className="flex items-center space-x-2 text-sm">
                      <Mail className="h-4 w-4 text-muted-foreground" />
                      <span className="text-muted-foreground">{client.email}</span>
                    </div>
                  )}
                  {client.phoneNumber && (
                    <div className="flex items-center space-x-2 text-sm">
                      <Phone className="h-4 w-4 text-muted-foreground" />
                      <span className="text-muted-foreground">{client.phoneNumber}</span>
                    </div>
                  )}
                  {client.tin && (
                    <div className="flex items-center space-x-2 text-sm">
                      <FileText className="h-4 w-4 text-muted-foreground" />
                      <span className="text-muted-foreground">TIN: {client.tin}</span>
                    </div>
                  )}
                </div>

                {/* Client Details */}
                {client.clientType && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Type:</span>
                    <Badge 
                      variant="outline" 
                      className={getClientTypeColor(client.clientType)}
                    >
                      {client.clientType}
                    </Badge>
                  </div>
                )}
                
                {client.annualTurnover && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Annual Turnover:</span>
                    <span className="font-medium">{formatCurrency(client.annualTurnover)}</span>
                  </div>
                )}

                {/* Activity Summary */}
                <div className="border-t pt-3 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Recent Actions:</span>
                    <span className="font-medium">{client.recentActionsCount}</span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Upcoming Deadlines:</span>
                    <span className="font-medium">{client.upcomingDeadlinesCount}</span>
                  </div>
                  {client.lastActionDate && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Last Activity:</span>
                      <span className="font-medium">
                        {new Date(client.lastActionDate).toLocaleDateString()}
                      </span>
                    </div>
                  )}
                  {client.nextDeadline && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Next Deadline:</span>
                      <span className="font-medium text-orange-600">
                        {new Date(client.nextDeadline).toLocaleDateString()}
                      </span>
                    </div>
                  )}
                </div>

                {/* Actions */}
                <div className="flex space-x-2 pt-2">
                  <Link href={`/clients/${client.clientId}`} className="flex-1">
                    <Button size="sm" variant="outline" className="w-full">
                      <Eye className="mr-1 h-3 w-3" />
                      View
                    </Button>
                  </Link>
                  <Link href={`/tax-filings?clientId=${client.clientId}`}>
                    <Button size="sm" variant="outline">
                      <FileText className="mr-1 h-3 w-3" />
                      Filings
                    </Button>
                  </Link>
                  <Link href={`/payments?clientId=${client.clientId}`}>
                    <Button size="sm" variant="outline">
                      <CreditCard className="mr-1 h-3 w-3" />
                      Payments
                    </Button>
                  </Link>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      {filteredClients.length === 0 && (
        <Card>
          <CardContent className="text-center py-8">
            <Users className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-medium text-muted-foreground mb-2">
              {clients.length === 0 
                ? 'No delegated clients found'
                : 'No clients match your current filters'
              }
            </h3>
            <p className="text-sm text-muted-foreground">
              {clients.length === 0 
                ? 'You have not been granted access to any clients yet.'
                : 'Try adjusting your search criteria or filters.'
              }
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}