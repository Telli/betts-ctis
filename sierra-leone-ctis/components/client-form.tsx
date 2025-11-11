"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";
import { ClientService, ClientDto } from "@/lib/services";
import { useRouter } from "next/navigation";

interface ClientFormProps {
  clientId?: number;
  isEditMode?: boolean;
}

export function ClientForm({ clientId, isEditMode = false }: ClientFormProps) {
  const [formData, setFormData] = useState<ClientDto>({
    clientNumber: "",
    businessName: "",
    contactPerson: "",
    email: "",
    phoneNumber: "",
    address: "",
    clientType: 2, // Corporation = 2
    taxpayerCategory: 1, // Medium = 1
    annualTurnover: 0,
    tin: "",
    status: 0 // 0 = Active (matches backend enum)
  });
  const [isLoading, setIsLoading] = useState(false);
  const [loadingClient, setLoadingClient] = useState(isEditMode);
  const { toast } = useToast();
  const router = useRouter();

  useEffect(() => {
    const fetchClient = async () => {
      if (isEditMode && clientId) {
        try {
          setLoadingClient(true);
          const client = await ClientService.getById(clientId);
          setFormData(client);
        } catch (error: any) {
          toast({
            title: "Error",
            description: "Failed to load client details.",
            variant: "destructive",
          });
        } finally {
          setLoadingClient(false);
        }
      }
    };

    fetchClient();
  }, [clientId, isEditMode, toast]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSelectChange = (name: string, value: string) => {
    // Convert to number for enum fields
    const enumFields = ['status', 'clientType', 'taxpayerCategory'];
    const parsedValue = enumFields.includes(name) ? parseInt(value, 10) : value;
    setFormData((prev) => ({ ...prev, [name]: parsedValue }));
  };

  const handleNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    // Allow empty string for better UX, convert to number only if value exists
    const numValue = value === '' ? '' : parseFloat(value) || 0;
    setFormData((prev) => ({ ...prev, [name]: numValue }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      // Prepare payload - ensure we're sending the correct field names and types
      const payload: ClientDto = {
        ...formData,
        clientNumber: formData.clientNumber || `CLN-${Date.now()}`,
        // Ensure enum fields are numbers, not strings
        clientType: typeof formData.clientType === 'string' ? parseInt(formData.clientType) : formData.clientType,
        taxpayerCategory: typeof formData.taxpayerCategory === 'string' ? parseInt(formData.taxpayerCategory) : formData.taxpayerCategory,
        status: typeof formData.status === 'string' ? parseInt(formData.status) : formData.status,
        // Ensure annualTurnover is a number
        annualTurnover: typeof formData.annualTurnover === 'string' ? parseFloat(formData.annualTurnover) || 0 : formData.annualTurnover,
      };


      if (isEditMode && clientId) {
        await ClientService.update(clientId, payload);
        toast({
          title: "Success",
          description: "Client updated successfully.",
        });
      } else {
        await ClientService.create(payload);
        toast({
          title: "Success",
          description: "Client created successfully.",
        });
      }
      router.push("/clients");
    } catch (error: any) {
      console.error('Client save error:', error);
      toast({
        title: "Error",
        description: error.message || "Failed to save client. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsLoading(false);
    }
  };

  if (loadingClient) {
    return (
      <Card className="w-full shadow-md">
        <CardHeader>
          <CardTitle>Loading client information...</CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <Card className="w-full shadow-md">
      <CardHeader>
        <CardTitle>{isEditMode ? "Edit Client" : "Add New Client"}</CardTitle>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="businessName">Business Name</Label>
            <Input
              id="businessName"
              name="businessName"
              value={formData.businessName}
              onChange={handleChange}
              placeholder="Enter company or individual name"
              required
              disabled={isLoading}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="clientType">Client Type</Label>
              <Select
                value={String(formData.clientType)}
                onValueChange={(value) => handleSelectChange("clientType", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select client type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Individual</SelectItem>
                  <SelectItem value="1">Partnership</SelectItem>
                  <SelectItem value="2">Corporation</SelectItem>
                  <SelectItem value="3">NGO</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="taxpayerCategory">Taxpayer Category</Label>
              <Select
                value={String(formData.taxpayerCategory)}
                onValueChange={(value) => handleSelectChange("taxpayerCategory", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select category" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Large Taxpayer</SelectItem>
                  <SelectItem value="1">Medium Taxpayer</SelectItem>
                  <SelectItem value="2">Small Taxpayer</SelectItem>
                  <SelectItem value="3">Micro Taxpayer</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="tin">TIN (Tax Identification Number)</Label>
              <Input
                id="tin"
                name="tin"
                value={formData.tin || ""}
                onChange={handleChange}
                placeholder="e.g. TIN-123-2024"
                disabled={isLoading}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="contactPerson">Primary Contact</Label>
              <Input
                id="contactPerson"
                name="contactPerson"
                value={formData.contactPerson}
                onChange={handleChange}
                placeholder="Contact person name"
                required
                disabled={isLoading}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email Address</Label>
              <Input
                id="email"
                name="email"
                type="email"
                value={formData.email}
                onChange={handleChange}
                placeholder="email@example.com"
                required
                disabled={isLoading}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="phoneNumber">Phone Number</Label>
              <Input
                id="phoneNumber"
                name="phoneNumber"
                value={formData.phoneNumber}
                onChange={handleChange}
                placeholder="+232-XX-XXX-XXX"
                required
                disabled={isLoading}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="address">Address</Label>
            <Input
              id="address"
              name="address"
              value={formData.address}
              onChange={handleChange}
              placeholder="Full business address"
              required
              disabled={isLoading}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="annualTurnover">Annual Turnover (SLE)</Label>
            <Input
              id="annualTurnover"
              name="annualTurnover"
              type="number"
              step="0.01"
              min="0"
              value={formData.annualTurnover === 0 ? '' : formData.annualTurnover}
              onChange={handleNumberChange}
              placeholder="Enter annual turnover"
              required
              disabled={isLoading}
            />
          </div>

          {isEditMode && (
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select
                value={String(formData.status)}
                onValueChange={(value) => handleSelectChange("status", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Active</SelectItem>
                  <SelectItem value="1">Inactive</SelectItem>
                  <SelectItem value="2">Suspended</SelectItem>
                </SelectContent>
              </Select>
            </div>
          )}
        </CardContent>
        <CardFooter className="flex justify-end space-x-4">
          <Button 
            variant="outline" 
            type="button" 
            onClick={() => router.push("/clients")}
            disabled={isLoading}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={isLoading}>
            {isLoading 
              ? (isEditMode ? "Updating..." : "Creating...") 
              : (isEditMode ? "Update Client" : "Create Client")
            }
          </Button>
        </CardFooter>
      </form>
    </Card>
  );
}
