"use client"

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { useToast } from '@/hooks/use-toast';
import { 
  User, 
  Building2, 
  Mail, 
  Phone, 
  MapPin, 
  FileText,
  Shield,
  Calendar,
  AlertCircle
} from 'lucide-react';
import { ProfileForm } from '@/components/client-portal/forms/profile-form';
import { ClientPortalService, ClientProfile } from '@/lib/services/client-portal-service';

export default function ClientProfilePage() {
  const [profile, setProfile] = useState<ClientProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const { toast } = useToast();

  const fetchProfile = async () => {
    try {
      setLoading(true);
      const data = await ClientPortalService.getProfile();
      setProfile(data);
    } catch (error) {
      console.error('Error fetching profile:', error);
      toast({
        title: "Error",
        description: "Failed to load profile information. Please try again.",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleProfileUpdate = (updatedProfile: ClientProfile) => {
    setProfile(updatedProfile);
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'active': return 'bg-green-100 text-green-800';
      case 'inactive': return 'bg-yellow-100 text-yellow-800';
      case 'suspended': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getCategoryDescription = (category: string) => {
    switch (category.toLowerCase()) {
      case 'large': return 'Annual turnover > 2 billion SLE';
      case 'medium': return 'Annual turnover 500M - 2B SLE';
      case 'small': return 'Annual turnover 100M - 500M SLE';
      case 'micro': return 'Annual turnover < 100M SLE';
      default: return '';
    }
  };

  if (loading) {
    return (
      <div className="p-6 space-y-6">
        <div className="flex items-center justify-between">
          <div>
            <Skeleton className="h-8 w-64 mb-2" />
            <Skeleton className="h-4 w-96" />
          </div>
        </div>
        
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <Card className="lg:col-span-1">
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {[...Array(5)].map((_, i) => (
                  <div key={i} className="space-y-2">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-6 w-full" />
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
          
          <Card className="lg:col-span-2">
            <CardHeader>
              <Skeleton className="h-6 w-32" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-96 w-full" />
            </CardContent>
          </Card>
        </div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="p-6">
        <div className="text-center py-12">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Profile Not Found</h2>
          <p className="text-gray-600">Unable to load your profile information.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Business Profile</h1>
          <p className="text-gray-600">Manage your organization's information and contact details</p>
        </div>
        <Badge className={getStatusColor(profile.status)}>
          <Shield className="h-3 w-3 mr-1" />
          {profile.status}
        </Badge>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Profile Overview */}
        <Card className="lg:col-span-1">
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Building2 className="h-5 w-5" />
              <span>Organization Overview</span>
            </CardTitle>
            <CardDescription>
              Quick view of your business information
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Business Name */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <Building2 className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">Business Name</span>
              </div>
              <p className="font-semibold text-gray-900">{profile.businessName}</p>
            </div>

            {/* Contact Person */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <User className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">Contact Person</span>
              </div>
              <p className="text-gray-900">{profile.contactPerson}</p>
            </div>

            {/* Email */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <Mail className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">Email</span>
              </div>
              <p className="text-gray-900">{profile.email}</p>
            </div>

            {/* Phone */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <Phone className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">Phone</span>
              </div>
              <p className="text-gray-900">{profile.phoneNumber}</p>
            </div>

            {/* Address */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <MapPin className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">Address</span>
              </div>
              <p className="text-gray-900 text-sm leading-relaxed">{profile.address}</p>
            </div>

            {/* TIN */}
            <div>
              <div className="flex items-center space-x-2 mb-1">
                <FileText className="h-4 w-4 text-gray-500" />
                <span className="text-sm font-medium text-gray-600">TIN</span>
              </div>
              <p className="text-gray-900">{profile.tin || 'Not provided'}</p>
            </div>

            {/* Business Classification */}
            <div className="pt-4 border-t">
              <h4 className="font-medium text-gray-900 mb-3">Business Classification</h4>
              <div className="space-y-3">
                <div>
                  <span className="text-sm text-gray-600">Taxpayer Category</span>
                  <div className="flex items-center space-x-2 mt-1">
                    <Badge variant="outline">{profile.taxpayerCategory}</Badge>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">
                    {getCategoryDescription(profile.taxpayerCategory)}
                  </p>
                </div>
                <div>
                  <span className="text-sm text-gray-600">Business Type</span>
                  <div className="flex items-center space-x-2 mt-1">
                    <Badge variant="outline">{profile.clientType}</Badge>
                  </div>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Profile Form */}
        <div className="lg:col-span-2">
          <ProfileForm 
            initialData={profile}
            onSave={handleProfileUpdate}
          />
        </div>
      </div>

      {/* Additional Information */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <Calendar className="h-5 w-5" />
            <span>Account Information</span>
          </CardTitle>
          <CardDescription>
            Important details about your account status and compliance requirements
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center p-4 bg-sierra-blue-50 rounded-lg">
              <div className="text-2xl font-bold text-sierra-blue-700">#{profile.clientId}</div>
              <div className="text-sm text-sierra-blue-600">Client ID</div>
            </div>
            <div className="text-center p-4 bg-sierra-green-50 rounded-lg">
              <div className="text-2xl font-bold text-sierra-green-700">{profile.taxpayerCategory}</div>
              <div className="text-sm text-sierra-green-600">Taxpayer Category</div>
            </div>
            <div className="text-center p-4 bg-sierra-gold-50 rounded-lg">
              <div className="text-2xl font-bold text-sierra-gold-700">{profile.status}</div>
              <div className="text-sm text-sierra-gold-600">Account Status</div>
            </div>
          </div>

          {/* Important Notices */}
          <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex items-start space-x-3">
              <FileText className="h-5 w-5 text-blue-600 mt-0.5" />
              <div>
                <h4 className="font-medium text-blue-900">Keep Your Information Updated</h4>
                <p className="text-sm text-blue-800 mt-1">
                  Ensure your business information is always current to maintain compliance with Sierra Leone tax regulations. 
                  Any changes to your business structure, contact information, or TIN should be updated immediately.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}