"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/context/auth-context";
import Link from "next/link";
import { Building2, Shield, FileText, BarChart3 } from "lucide-react";

export default function HomePage() {
  const router = useRouter();
  const { isLoggedIn } = useAuth();

  // Redirect authenticated users to dashboard
  useEffect(() => {
    if (isLoggedIn) {
      router.push("/dashboard");
    }
  }, [isLoggedIn, router]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-sierra-blue to-sierra-gold/20">
      <div className="container mx-auto px-4 py-16">
        {/* Hero Section */}
        <div className="text-center mb-16">
          <h1 className="text-4xl font-bold text-white mb-4">
            Sierra Leone Client Tax Information System
          </h1>
          <p className="text-xl text-white/90 mb-8">
            Comprehensive tax management for The Betts Firm
          </p>
          <div className="flex gap-4 justify-center">
            <Link href="/login">
              <Button size="lg" className="bg-white text-sierra-blue hover:bg-gray-100">
                Login to System
              </Button>
            </Link>
            <Link href="/register">
              <Button size="lg" variant="outline" className="border-white text-white hover:bg-white/10">
                Register Account
              </Button>
            </Link>
          </div>
        </div>

        {/* Features Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card className="bg-white/10 backdrop-blur-sm border-white/20">
            <CardHeader>
              <Building2 className="h-8 w-8 text-white mb-2" />
              <CardTitle className="text-white">Client Management</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-white/80">Manage all your clients and their tax information in one place.</p>
            </CardContent>
          </Card>

          <Card className="bg-white/10 backdrop-blur-sm border-white/20">
            <CardHeader>
              <FileText className="h-8 w-8 text-white mb-2" />
              <CardTitle className="text-white">Tax Filing</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-white/80">Submit and track tax filings according to Sierra Leone regulations.</p>
            </CardContent>
          </Card>

          <Card className="bg-white/10 backdrop-blur-sm border-white/20">
            <CardHeader>
              <Shield className="h-8 w-8 text-white mb-2" />
              <CardTitle className="text-white">Compliance</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-white/80">Ensure compliance with the Sierra Leone Finance Act 2025.</p>
            </CardContent>
          </Card>

          <Card className="bg-white/10 backdrop-blur-sm border-white/20">
            <CardHeader>
              <BarChart3 className="h-8 w-8 text-white mb-2" />
              <CardTitle className="text-white">Analytics</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-white/80">Track performance and generate comprehensive reports.</p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
