"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";
import { DocumentService } from "@/lib/services";

interface DocumentUploadProps {
  clientId: number;
  onUploadComplete?: () => void;
}

export function DocumentUpload({ clientId, onUploadComplete }: DocumentUploadProps) {
  const [file, setFile] = useState<File | null>(null);
  const [taxYearId, setTaxYearId] = useState<number | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const { toast } = useToast();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  const handleUpload = async () => {
    if (!file) {
      toast({
        title: "Error",
        description: "Please select a file to upload",
        variant: "destructive",
      });
      return;
    }

    setIsUploading(true);
    try {
      const uploadRequest = {
        file: file,
        category: 'supporting-document' as const,
        taxYearId: taxYearId || undefined,
      };
      await DocumentService.upload(clientId, uploadRequest);
      toast({
        title: "Success",
        description: "Document uploaded successfully",
      });
      setFile(null);
      if (onUploadComplete) {
        onUploadComplete();
      }
    } catch (error) {
      console.error("Upload error:", error);
      toast({
        title: "Upload Failed",
        description: "There was a problem uploading your document. Please try again.",
        variant: "destructive",
      });
    } finally {
      setIsUploading(false);
    }
  };

  // List of tax years
  const taxYears = [
    { id: 1, year: "2025" },
    { id: 2, year: "2024" },
    { id: 3, year: "2023" },
    { id: 4, year: "2022" },
    { id: 5, year: "2021" }
  ];

  return (
    <Card className="w-full shadow-md">
      <CardHeader>
        <CardTitle>Upload Document</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="document">Select Document</Label>
          <Input
            id="document"
            type="file"
            onChange={handleFileChange}
            disabled={isUploading}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="taxYear">Tax Year (Optional)</Label>
          <Select
            value={taxYearId?.toString() || ""}
            onValueChange={(value) => setTaxYearId(value ? Number(value) : null)}
            disabled={isUploading}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select Tax Year" />
            </SelectTrigger>
            <SelectContent>
              {taxYears.map((year) => (
                <SelectItem key={year.id} value={year.id.toString()}>
                  {year.year}
                </SelectItem>
              ))}
              <SelectItem value="">Not Applicable</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </CardContent>
      <CardFooter className="flex justify-end">
        <Button onClick={handleUpload} disabled={!file || isUploading}>
          {isUploading ? "Uploading..." : "Upload"}
        </Button>
      </CardFooter>
    </Card>
  );
}
