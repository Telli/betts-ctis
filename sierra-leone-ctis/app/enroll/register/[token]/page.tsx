'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ClientRegistrationForm } from '@/components/enrollment/client-registration-form';
import { Card, CardContent } from '@/components/ui/card';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { AlertTriangle, CheckCircle, Loader2, XCircle } from 'lucide-react';
import Link from 'next/link';
import { EnrollmentService, TokenValidationResult } from '@/lib/services/enrollment-service';

interface RegisterPageProps {
  params: Promise<{
    token: string;
  }>;
}

export default function RegisterPage({ params }: RegisterPageProps) {
  const [tokenValidation, setTokenValidation] = useState<TokenValidationResult | null>(null);
  const [isValidating, setIsValidating] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [token, setToken] = useState<string>('');
  const router = useRouter();

  useEffect(() => {
    async function loadParams() {
      const resolvedParams = await params;
      setToken(resolvedParams.token);
    }
    loadParams();
  }, [params]);

  useEffect(() => {
    if (token) {
      validateToken();
    }
  }, [token]);

  const validateToken = async () => {
    try {
      setIsValidating(true);
      setError(null);
      
      const result = await EnrollmentService.validateToken(token);
      setTokenValidation(result);
      
      if (!result.isValid) {
        setError(result.errorMessage || 'Invalid registration token');
      }
    } catch (err: any) {
      console.error('Token validation failed:', err);
      setError('Failed to validate registration token. Please try again.');
    } finally {
      setIsValidating(false);
    }
  };

  const handleRegistrationSuccess = () => {
    router.push('/enroll/success');
  };

  // Loading state
  if (isValidating) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5 flex items-center justify-center">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6">
            <div className="text-center space-y-4">
              <Loader2 className="h-8 w-8 animate-spin mx-auto text-sierra-blue" />
              <h2 className="text-xl font-semibold">Validating Registration Link</h2>
              <p className="text-muted-foreground">
                Please wait while we verify your registration token...
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Error state
  if (error || !tokenValidation?.isValid) {
    const isExpired = error?.includes('expired');
    
    return (
      <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6">
            <div className="text-center space-y-4">
              <div className="mx-auto w-12 h-12 bg-red-100 rounded-lg flex items-center justify-center">
                {isExpired ? (
                  <AlertTriangle className="h-6 w-6 text-orange-600" />
                ) : (
                  <XCircle className="h-6 w-6 text-red-600" />
                )}
              </div>
              
              <h2 className="text-xl font-semibold">
                {isExpired ? 'Registration Link Expired' : 'Invalid Registration Link'}
              </h2>
              
              <p className="text-muted-foreground">
                {error || 'This registration link is no longer valid.'}
              </p>
              
              <div className="space-y-2">
                {isExpired && (
                  <p className="text-sm text-muted-foreground">
                    Please contact your associate to send you a new invitation.
                  </p>
                )}
                
                <div className="flex flex-col gap-2">
                  <Button asChild>
                    <Link href="/enroll/self-register">
                      Try Self-Registration
                    </Link>
                  </Button>
                  
                  <Button variant="outline" asChild>
                    <Link href="/login">
                      Already have an account? Sign In
                    </Link>
                  </Button>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  // Success state - show registration form
  return (
    <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="mx-auto w-16 h-16 bg-gradient-to-br from-sierra-blue to-sierra-green rounded-full flex items-center justify-center mb-4">
            <CheckCircle className="h-8 w-8 text-white" />
          </div>
          <h1 className="text-3xl font-bold tracking-tight mb-2">Welcome to The Betts Firm</h1>
          <p className="text-muted-foreground max-w-2xl mx-auto">
            Complete your registration to access your personal tax information system and 
            work directly with our expert tax associates.
          </p>
        </div>

        {/* Token Expiration Warning */}
        {tokenValidation?.expirationDate && (
          <div className="max-w-2xl mx-auto mb-6">
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                This registration link expires on{' '}
                {new Date(tokenValidation.expirationDate).toLocaleDateString('en-US', {
                  weekday: 'long',
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                })}. Please complete your registration promptly.
              </AlertDescription>
            </Alert>
          </div>
        )}

        {/* Registration Form */}
        <div className="bg-white/60 backdrop-blur-sm rounded-2xl p-6 shadow-2xl">
          <ClientRegistrationForm
            token={token}
            email={tokenValidation?.email || ''}
            onSuccess={handleRegistrationSuccess}
          />
        </div>

        {/* Footer */}
        <div className="text-center mt-12">
          <p className="text-sm text-muted-foreground">
            Having trouble? Contact our support team for assistance.
          </p>
          <p className="text-xs text-muted-foreground mt-2">
            © 2025 The Betts Firm. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  );
}