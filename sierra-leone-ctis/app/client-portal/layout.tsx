import type React from "react"
import { ClientPortalLayout } from "@/components/client-portal/client-portal-layout"

export default function ClientPortalLayoutWrapper({
  children,
}: {
  children: React.ReactNode
}) {
  return <ClientPortalLayout>{children}</ClientPortalLayout>
}