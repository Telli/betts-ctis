import type React from "react"
import type { Metadata } from "next"
import { Inter } from "next/font/google"
import "./globals.css"
import { AuthProvider } from "@/context/auth-context"
import { ErrorBoundary } from "@/components/ui/error-boundary"
import { Toaster } from "@/components/ui/toaster"
import { ConditionalLayout } from "@/components/conditional-layout"
import { Providers } from "@/lib/providers"

const inter = Inter({ subsets: ["latin"] })

export const metadata: Metadata = {
  title: "CTIS - Client Tax Information System | The Betts Firm",
  description: "Comprehensive tax management system for Sierra Leone businesses compliant with Finance Act 2025",
  keywords: "Sierra Leone, tax management, CTIS, The Betts Firm, Finance Act 2025, NRA",
  generator: 'v0.dev'
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <ErrorBoundary>
          <Providers>
            <AuthProvider>
              <ConditionalLayout>
                {children}
              </ConditionalLayout>
              <Toaster />
            </AuthProvider>
          </Providers>
        </ErrorBoundary>
      </body>
    </html>
  )
}
