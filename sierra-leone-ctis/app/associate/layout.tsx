import type React from "react"
import { AssociatePortalLayout } from "@/components/associate/associate-portal-layout"

export default function AssociatePortalLayoutWrapper({
  children,
}: {
  children: React.ReactNode
}) {
  return <AssociatePortalLayout>{children}</AssociatePortalLayout>
}