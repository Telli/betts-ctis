'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { CheckCircle, ArrowRight, Home, User } from 'lucide-react';
import Link from 'next/link';

export default function RegistrationSuccessPage() {
  const router = useRouter();

  useEffect(() => {
    // Auto redirect to login after 10 seconds
    const timer = setTimeout(() => {
      router.push('/login');
    }, 10000);

    return () => clearTimeout(timer);
  }, [router]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5 flex items-center justify-center p-4">
      <Card className="w-full max-w-2xl shadow-2xl border-0 bg-white/90 backdrop-blur">
        <CardContent className="p-8">
          <div className="text-center space-y-6">
            {/* Success Icon */}
            <div className="mx-auto w-20 h-20 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center">
              <CheckCircle className="h-10 w-10 text-white" />
            </div>

            {/* Success Message */}
            <div className="space-y-2">
              <h1 className="text-3xl font-bold tracking-tight text-gray-900">
                Welcome to The Betts Firm!
              </h1>
              <p className="text-xl text-green-600 font-semibold">
                Registration Complete ✨
              </p>
            </div>

            {/* Description */}
            <div className="max-w-md mx-auto space-y-4">
              <p className="text-muted-foreground">
                Congratulations! Your account has been successfully created and verified. 
                You now have access to Sierra Leone's most comprehensive tax information system.
              </p>
              
              <div className="bg-sierra-blue/5 border border-sierra-blue/20 rounded-lg p-4">
                <p className="text-sm text-sierra-blue">
                  <strong>What's Next:</strong> Your dedicated tax associate will be in touch within 24 hours 
                  to guide you through the next steps and help you get the most out of your new account.
                </p>
              </div>
            </div>

            {/* Features Preview */}
            <div className="max-w-lg mx-auto">
              <h3 className="font-semibold mb-4 text-gray-900">You Now Have Access To:</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Personal tax dashboard</span>
                </div>
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Document management</span>
                </div>
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Compliance tracking</span>
                </div>
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Tax calculators</span>
                </div>
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Payment processing</span>
                </div>
                <div className="flex items-center space-x-2">
                  <CheckCircle className="h-4 w-4 text-green-600 flex-shrink-0" />
                  <span>Expert support</span>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex flex-col sm:flex-row gap-3 justify-center pt-6">
              <Button asChild size="lg" className="bg-sierra-blue hover:bg-sierra-blue/90">
                <Link href="/login">
                  <User className="mr-2 h-5 w-5" />
                  Access Your Account
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
              
              <Button variant="outline" size="lg" asChild>
                <Link href="/">
                  <Home className="mr-2 h-4 w-4" />
                  Return to Home
                </Link>
              </Button>
            </div>

            {/* Auto-redirect Notice */}
            <p className="text-xs text-muted-foreground">
              You will be automatically redirected to the login page in 10 seconds
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}