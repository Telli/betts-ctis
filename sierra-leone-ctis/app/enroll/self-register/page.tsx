'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, Mail, Loader2, CheckCircle, UserPlus } from 'lucide-react';
import Link from 'next/link';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { useToast } from '@/hooks/use-toast';
import { EnrollmentService } from '@/lib/services/enrollment-service';
import {
  selfRegistrationSchema,
  type SelfRegistrationFormData,
} from '@/lib/validations/enrollment';

export default function SelfRegisterPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState('');
  const { toast } = useToast();

  const form = useForm<SelfRegistrationFormData>({
    resolver: zodResolver(selfRegistrationSchema),
    defaultValues: {
      email: '',
    },
  });

  const onSubmit = async (data: SelfRegistrationFormData) => {
    setIsLoading(true);
    try {
      await EnrollmentService.initiateSelfRegistration(data);
      
      setRegisteredEmail(data.email);
      setIsSuccess(true);
      
      toast({
        title: 'Registration Link Sent!',
        description: `Please check your email at ${data.email} for the registration link.`,
        variant: 'default',
      });
    } catch (error: any) {
      console.error('Self-registration failed:', error);
      
      toast({
        title: 'Registration Failed',
        description: error.message || 'Failed to send registration link. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  if (isSuccess) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5 flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6">
            <div className="text-center space-y-4">
              <div className="mx-auto w-12 h-12 bg-green-100 rounded-lg flex items-center justify-center">
                <CheckCircle className="h-6 w-6 text-green-600" />
              </div>
              
              <h2 className="text-xl font-semibold">Check Your Email!</h2>
              
              <p className="text-muted-foreground">
                We've sent a registration link to <strong>{registeredEmail}</strong>
              </p>
              
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 text-left">
                <p className="text-sm text-blue-800 mb-2">
                  <strong>Next Steps:</strong>
                </p>
                <ul className="text-sm text-blue-700 space-y-1">
                  <li>• Check your email inbox (and spam folder)</li>
                  <li>• Click the secure registration link</li>
                  <li>• Complete your business information</li>
                  <li>• Start managing your tax compliance</li>
                </ul>
              </div>
              
              <div className="space-y-2">
                <Button
                  variant="outline"
                  onClick={() => {
                    setIsSuccess(false);
                    form.reset();
                  }}
                  className="w-full"
                >
                  Try Another Email
                </Button>
                
                <Button variant="ghost" asChild className="w-full">
                  <Link href="/login">
                    Already have an account? Sign In
                  </Link>
                </Button>
              </div>
              
              <p className="text-xs text-muted-foreground">
                Didn't receive the email? Check your spam folder or try again in a few minutes.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-sierra-blue/5 to-sierra-green/5">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4">
            <Link href="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Home
            </Link>
          </Button>
        </div>

        <div className="max-w-md mx-auto">
          <Card className="shadow-xl border-0 bg-white/80 backdrop-blur">
            <CardHeader className="text-center">
              <div className="mx-auto w-12 h-12 bg-sierra-blue/10 rounded-lg flex items-center justify-center mb-4">
                <UserPlus className="h-6 w-6 text-sierra-blue" />
              </div>
              <CardTitle className="text-2xl font-bold">Join The Betts Firm</CardTitle>
              <CardDescription className="text-base">
                Start your tax compliance journey with Sierra Leone's premier tax advisors
              </CardDescription>
            </CardHeader>
            
            <CardContent className="space-y-6">
              <Form {...form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                  <FormField
                    control={form.control}
                    name="email"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Email Address</FormLabel>
                        <FormControl>
                          <div className="relative">
                            <Mail className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                            <Input
                              type="email"
                              placeholder="Enter your business email"
                              className="pl-10"
                              {...field}
                            />
                          </div>
                        </FormControl>
                        <FormDescription>
                          We'll send you a secure link to complete your registration
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <Button 
                    type="submit" 
                    className="w-full" 
                    disabled={isLoading}
                  >
                    {isLoading ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Sending Registration Link...
                      </>
                    ) : (
                      <>
                        <Mail className="mr-2 h-4 w-4" />
                        Send Registration Link
                      </>
                    )}
                  </Button>
                </form>
              </Form>

              <div className="text-center">
                <p className="text-sm text-muted-foreground">
                  Already have an account?{' '}
                  <Link 
                    href="/login" 
                    className="font-medium text-sierra-blue hover:text-sierra-blue/80 transition-colors"
                  >
                    Sign in here
                  </Link>
                </p>
              </div>
            </CardContent>
          </Card>

          {/* Info Section */}
          <div className="mt-8 space-y-4">
            <Alert>
              <CheckCircle className="h-4 w-4" />
              <AlertDescription>
                Registration is <strong>free</strong> and gives you access to comprehensive 
                tax compliance tools and expert guidance from our Sierra Leone tax specialists.
              </AlertDescription>
            </Alert>

            <div className="bg-white/60 backdrop-blur rounded-lg p-6">
              <h3 className="font-semibold mb-3 text-center">What You'll Get:</h3>
              <ul className="space-y-2 text-sm">
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Personal tax compliance dashboard
                </li>
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Dedicated tax associate assignment
                </li>
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Automated deadline reminders
                </li>
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Document management system
                </li>
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Real-time compliance status tracking
                </li>
                <li className="flex items-center">
                  <CheckCircle className="h-4 w-4 text-green-600 mr-2 flex-shrink-0" />
                  Sierra Leone tax calculators
                </li>
              </ul>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center mt-12">
          <p className="text-xs text-muted-foreground">
            © 2025 The Betts Firm. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  );
}