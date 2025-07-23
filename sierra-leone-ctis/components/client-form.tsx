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
    name: "",
    type: "Limited Company",
    category: "Medium Taxpayer",
    tin: "",
    contact: "",
    status: "pending",
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
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      if (isEditMode && clientId) {
        await ClientService.update(clientId, formData);
        toast({
          title: "Success",
          description: "Client updated successfully.",
        });
      } else {
        await ClientService.create(formData);
        toast({
          title: "Success",
          description: "Client created successfully.",
        });
      }
      router.push("/clients");
    } catch (error: any) {
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
            <Label htmlFor="name">Client Name</Label>
            <Input
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              placeholder="Enter company or individual name"
              required
              disabled={isLoading}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="type">Client Type</Label>
              <Select
                value={formData.type}
                onValueChange={(value) => handleSelectChange("type", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select client type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Corporation">Corporation</SelectItem>
                  <SelectItem value="Limited Company">Limited Company</SelectItem>
                  <SelectItem value="Partnership">Partnership</SelectItem>
                  <SelectItem value="Sole Proprietor">Sole Proprietor</SelectItem>
                  <SelectItem value="Individual">Individual</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="category">Taxpayer Category</Label>
              <Select
                value={formData.category}
                onValueChange={(value) => handleSelectChange("category", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select category" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Large Taxpayer">Large Taxpayer</SelectItem>
                  <SelectItem value="Medium Taxpayer">Medium Taxpayer</SelectItem>
                  <SelectItem value="Small Taxpayer">Small Taxpayer</SelectItem>
                  <SelectItem value="Micro Taxpayer">Micro Taxpayer</SelectItem>
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
                value={formData.tin}
                onChange={handleChange}
                placeholder="e.g. TIN-123-2024"
                required
                disabled={isLoading}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="contact">Primary Contact</Label>
              <Input
                id="contact"
                name="contact"
                value={formData.contact}
                onChange={handleChange}
                placeholder="Contact person name"
                required
                disabled={isLoading}
              />
            </div>
          </div>

          {isEditMode && (
            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <Select
                value={formData.status || "pending"}
                onValueChange={(value) => handleSelectChange("status", value)}
                disabled={isLoading}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="compliant">Compliant</SelectItem>
                  <SelectItem value="pending">Pending</SelectItem>
                  <SelectItem value="warning">Warning</SelectItem>
                  <SelectItem value="overdue">Overdue</SelectItem>
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
