'use client';

import { useAuth } from '@/context/auth-context';
import { InviteClientForm } from '@/components/enrollment/invite-client-form';
import { Card, CardContent } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { ArrowLeft, Shield } from 'lucide-react';
import Link from 'next/link';
import { Button } from '@/components/ui/button';

export default function InviteClientPage() {
  const { user } = useAuth();

  if (!user) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-sierra-blue"></div>
      </div>
    );
  }

  // Check if user has permission to invite clients
  const canInviteClients = user?.role && ['Admin', 'Associate', 'SystemAdmin'].includes(user.role);

  if (!canInviteClients) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6">
            <div className="text-center space-y-4">
              <div className="mx-auto w-12 h-12 bg-red-100 rounded-lg flex items-center justify-center">
                <Shield className="h-6 w-6 text-red-600" />
              </div>
              <h2 className="text-xl font-semibold">Access Denied</h2>
              <p className="text-muted-foreground">
                Only associates and administrators can invite new clients.
              </p>
              <Button asChild>
                <Link href="/dashboard">
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Return to Dashboard
                </Link>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  const handleInviteSuccess = () => {
    // Could navigate to pending invitations or show success state
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4">
            <Link href="/dashboard">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Dashboard
            </Link>
          </Button>
          
          <div className="text-center mb-6">
            <h1 className="text-3xl font-bold tracking-tight mb-2">Client Enrollment</h1>
            <p className="text-muted-foreground max-w-2xl mx-auto">
              Invite new clients to join The Betts Firm's tax information system. 
              They'll receive a secure registration link to complete their setup.
            </p>
          </div>
        </div>

        {/* Security Notice */}
        <div className="max-w-2xl mx-auto mb-8">
          <Alert>
            <Shield className="h-4 w-4" />
            <AlertDescription>
              All invitation links are secure, single-use, and expire after 48 hours. 
              Clients will be automatically assigned to you upon successful registration.
            </AlertDescription>
          </Alert>
        </div>

        {/* Invitation Form */}
        <div className="max-w-md mx-auto">
          <Card className="shadow-xl border-0 bg-white/80 backdrop-blur">
            <CardContent className="p-8">
              <InviteClientForm onSuccess={handleInviteSuccess} />
            </CardContent>
          </Card>
        </div>

        {/* Info Section */}
        <div className="max-w-4xl mx-auto mt-12">
          <div className="grid md:grid-cols-3 gap-6">
            <div className="text-center p-6">
              <div className="w-12 h-12 bg-sierra-blue/10 rounded-lg mx-auto mb-4 flex items-center justify-center">
                <span className="text-sierra-blue font-bold">1</span>
              </div>
              <h3 className="font-semibold mb-2">Send Invitation</h3>
              <p className="text-sm text-muted-foreground">
                Enter the client's email and send a secure invitation link
              </p>
            </div>
            
            <div className="text-center p-6">
              <div className="w-12 h-12 bg-sierra-green/10 rounded-lg mx-auto mb-4 flex items-center justify-center">
                <span className="text-sierra-green font-bold">2</span>
              </div>
              <h3 className="font-semibold mb-2">Client Registration</h3>
              <p className="text-sm text-muted-foreground">
                Client completes registration with business and tax information
              </p>
            </div>
            
            <div className="text-center p-6">
              <div className="w-12 h-12 bg-sierra-gold/10 rounded-lg mx-auto mb-4 flex items-center justify-center">
                <span className="text-sierra-gold font-bold">3</span>
              </div>
              <h3 className="font-semibold mb-2">Get Notified</h3>
              <p className="text-sm text-muted-foreground">
                Receive notification when client completes registration
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}