"use client"

import { useRouter } from "next/navigation"
import { ArrowLeft } from "lucide-react"
import { Button } from "@/components/ui/button"
import Link from "next/link"
import DocumentUploadForm from "@/components/document-upload-form"

export default function NewDocumentPage() {
  const router = useRouter()

  const handleUploadComplete = () => {
    router.push("/documents")
  }

  return (
    <div className="container mx-auto py-8">
      <div className="mb-6">
        <Link href="/documents">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Documents
          </Button>
        </Link>
      </div>

      <DocumentUploadForm 
        onUploadComplete={handleUploadComplete}
        onCancel={() => router.push("/documents")}
      />
    </div>
  )
}
