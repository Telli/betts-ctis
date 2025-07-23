"use client"

import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { useToast } from '@/hooks/use-toast';
import { Loader2, Save, User, Building2, Mail, Phone, MapPin, FileText } from 'lucide-react';
import { clientProfileSchema, ClientProfileFormData } from '@/lib/validations/client-portal';
import { ClientPortalService, ClientProfile } from '@/lib/services/client-portal-service';

interface ProfileFormProps {
  initialData?: ClientProfile;
  onSave?: (data: ClientProfile) => void;
}

export function ProfileForm({ initialData, onSave }: ProfileFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const { toast } = useToast();

  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
    reset
  } = useForm<ClientProfileFormData>({
    resolver: zodResolver(clientProfileSchema),
    defaultValues: initialData ? {
      businessName: initialData.businessName,
      contactPerson: initialData.contactPerson,
      email: initialData.email,
      phoneNumber: initialData.phoneNumber,
      address: initialData.address,
      tin: initialData.tin || ''
    } : undefined
  });

  const onSubmit = async (data: ClientProfileFormData) => {
    try {
      setIsLoading(true);
      const updatedProfile = await ClientPortalService.updateProfile(data);
      
      toast({
        title: "Profile Updated",
        description: "Your business profile has been successfully updated.",
      });

      onSave?.(updatedProfile);
      reset(data); // Reset form dirty state
    } catch (error) {
      console.error('Error updating profile:', error);
      toast({
        title: "Update Failed",
        description: "Failed to update your profile. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center space-x-2">
          <User className="h-5 w-5" />
          <span>Business Profile</span>
        </CardTitle>
        <CardDescription>
          Update your organization's business information and contact details
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          {/* Business Name */}
          <div className="space-y-2">
            <Label htmlFor="businessName" className="flex items-center space-x-2">
              <Building2 className="h-4 w-4" />
              <span>Business Name *</span>
            </Label>
            <Input
              id="businessName"
              {...register('businessName')}
              placeholder="Enter your business name"
              className={errors.businessName ? "border-red-500" : ""}
            />
            {errors.businessName && (
              <p className="text-sm text-red-600">{errors.businessName.message}</p>
            )}
          </div>

          {/* Contact Person */}
          <div className="space-y-2">
            <Label htmlFor="contactPerson" className="flex items-center space-x-2">
              <User className="h-4 w-4" />
              <span>Contact Person *</span>
            </Label>
            <Input
              id="contactPerson"
              {...register('contactPerson')}
              placeholder="Enter primary contact person"
              className={errors.contactPerson ? "border-red-500" : ""}
            />
            {errors.contactPerson && (
              <p className="text-sm text-red-600">{errors.contactPerson.message}</p>
            )}
          </div>

          {/* Email */}
          <div className="space-y-2">
            <Label htmlFor="email" className="flex items-center space-x-2">
              <Mail className="h-4 w-4" />
              <span>Email Address *</span>
            </Label>
            <Input
              id="email"
              type="email"
              {...register('email')}
              placeholder="Enter email address"
              className={errors.email ? "border-red-500" : ""}
            />
            {errors.email && (
              <p className="text-sm text-red-600">{errors.email.message}</p>
            )}
          </div>

          {/* Phone Number */}
          <div className="space-y-2">
            <Label htmlFor="phoneNumber" className="flex items-center space-x-2">
              <Phone className="h-4 w-4" />
              <span>Phone Number *</span>
            </Label>
            <Input
              id="phoneNumber"
              {...register('phoneNumber')}
              placeholder="Enter phone number"
              className={errors.phoneNumber ? "border-red-500" : ""}
            />
            {errors.phoneNumber && (
              <p className="text-sm text-red-600">{errors.phoneNumber.message}</p>
            )}
          </div>

          {/* Address */}
          <div className="space-y-2">
            <Label htmlFor="address" className="flex items-center space-x-2">
              <MapPin className="h-4 w-4" />
              <span>Business Address *</span>
            </Label>
            <Textarea
              id="address"
              {...register('address')}
              placeholder="Enter your business address"
              rows={3}
              className={errors.address ? "border-red-500" : ""}
            />
            {errors.address && (
              <p className="text-sm text-red-600">{errors.address.message}</p>
            )}
          </div>

          {/* TIN */}
          <div className="space-y-2">
            <Label htmlFor="tin" className="flex items-center space-x-2">
              <FileText className="h-4 w-4" />
              <span>Tax Identification Number (TIN)</span>
            </Label>
            <Input
              id="tin"
              {...register('tin')}
              placeholder="Enter your TIN (optional)"
              className={errors.tin ? "border-red-500" : ""}
            />
            {errors.tin && (
              <p className="text-sm text-red-600">{errors.tin.message}</p>
            )}
            <p className="text-sm text-gray-600">
              Your TIN is required for certain tax filings and compliance reports
            </p>
          </div>

          {/* Form Actions */}
          <div className="flex items-center justify-between pt-6 border-t">
            <p className="text-sm text-gray-600">
              * Required fields
            </p>
            <div className="flex space-x-3">
              <Button
                type="button"
                variant="outline"
                onClick={() => reset()}
                disabled={!isDirty || isLoading}
              >
                Reset Changes
              </Button>
              <Button
                type="submit"
                disabled={!isDirty || isLoading}
                className="min-w-32"
              >
                {isLoading ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="h-4 w-4 mr-2" />
                    Save Changes
                  </>
                )}
              </Button>
            </div>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}