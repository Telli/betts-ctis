'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowLeft, ArrowRight, Check, Eye, EyeOff, Loader2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
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
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Progress } from '@/components/ui/progress';
import { useToast } from '@/hooks/use-toast';
import { EnrollmentService } from '@/lib/services/enrollment-service';
import {
  clientRegistrationSchema,
  type ClientRegistrationFormData,
} from '@/lib/validations/enrollment';

interface ClientRegistrationFormProps {
  token: string;
  email: string;
  onSuccess: () => void;
}

const steps = [
  { id: 1, title: 'Personal Information', description: 'Your basic details' },
  { id: 2, title: 'Security', description: 'Create your password' },
  { id: 3, title: 'Business Information', description: 'About your business' },
  { id: 4, title: 'Tax Information', description: 'Tax-related details' },
  { id: 5, title: 'Review', description: 'Confirm your information' },
];

export function ClientRegistrationForm({ token, email, onSuccess }: ClientRegistrationFormProps) {
  const [currentStep, setCurrentStep] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const { toast } = useToast();

  const form = useForm<ClientRegistrationFormData>({
    resolver: zodResolver(clientRegistrationSchema),
    defaultValues: {
      email,
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
      businessName: '',
      phoneNumber: '',
      taxpayerCategory: undefined,
      clientType: undefined,
      registrationToken: token,
      taxpayerIdentificationNumber: '',
      businessAddress: '',
      contactPersonName: '',
      contactPersonPhone: '',
      annualTurnover: undefined,
    },
    mode: 'onChange',
  });

  const nextStep = async () => {
    const fieldsToValidate = getFieldsForStep(currentStep);
    const isValid = await form.trigger(fieldsToValidate);
    
    if (isValid && currentStep < steps.length) {
      setCurrentStep(currentStep + 1);
    }
  };

  const prevStep = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const onSubmit = async (data: ClientRegistrationFormData) => {
    setIsLoading(true);
    try {
      await EnrollmentService.completeRegistration(data);
      
      toast({
        title: 'Registration Complete!',
        description: 'Welcome to The Betts Firm. You can now access your client portal.',
        variant: 'default',
      });

      onSuccess();
    } catch (error: any) {
      console.error('Registration failed:', error);
      
      toast({
        title: 'Registration Failed',
        description: error.message || 'Something went wrong. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const getFieldsForStep = (step: number) => {
    switch (step) {
      case 1:
        return ['firstName', 'lastName', 'phoneNumber'] as const;
      case 2:
        return ['password', 'confirmPassword'] as const;
      case 3:
        return ['businessName', 'businessAddress', 'clientType'] as const;
      case 4:
        return ['taxpayerCategory'] as const;
      default:
        return [] as const;
    }
  };

  const progress = (currentStep / steps.length) * 100;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      {/* Progress Header */}
      <div className="space-y-4">
        <div className="text-center">
          <h1 className="text-2xl font-bold tracking-tight">Complete Your Registration</h1>
          <p className="text-muted-foreground">
            Step {currentStep} of {steps.length}: {steps[currentStep - 1].title}
          </p>
        </div>
        <Progress value={progress} className="w-full" />
        
        <div className="flex justify-between text-xs text-muted-foreground">
          {steps.map((step, index) => (
            <div
              key={step.id}
              className={`flex flex-col items-center space-y-1 ${
                index + 1 <= currentStep ? 'text-sierra-blue' : ''
              }`}
            >
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
                  index + 1 <= currentStep
                    ? 'bg-sierra-blue border-sierra-blue text-white'
                    : 'border-gray-300'
                }`}
              >
                {index + 1 < currentStep ? <Check className="h-4 w-4" /> : index + 1}
              </div>
              <span className="hidden sm:block text-center">{step.title}</span>
            </div>
          ))}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{steps[currentStep - 1].title}</CardTitle>
          <CardDescription>{steps[currentStep - 1].description}</CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              {/* Step 1: Personal Information */}
              {currentStep === 1 && (
                <div className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="firstName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>First Name</FormLabel>
                          <FormControl>
                            <Input placeholder="John" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={form.control}
                      name="lastName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Last Name</FormLabel>
                          <FormControl>
                            <Input placeholder="Doe" {...field} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                  
                  <FormField
                    control={form.control}
                    name="phoneNumber"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Phone Number</FormLabel>
                        <FormControl>
                          <Input placeholder="+232 XX XXX XXXX" {...field} />
                        </FormControl>
                        <FormDescription>
                          Include country code (e.g., +232 for Sierra Leone)
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                    <p className="text-sm text-blue-800">
                      <strong>Email:</strong> {email}
                    </p>
                    <p className="text-xs text-blue-600 mt-1">
                      This email address will be used for your account login
                    </p>
                  </div>
                </div>
              )}

              {/* Step 2: Security */}
              {currentStep === 2 && (
                <div className="space-y-4">
                  <FormField
                    control={form.control}
                    name="password"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Password</FormLabel>
                        <FormControl>
                          <div className="relative">
                            <Input
                              type={showPassword ? "text" : "password"}
                              placeholder="Create a strong password"
                              {...field}
                            />
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                              onClick={() => setShowPassword(!showPassword)}
                            >
                              {showPassword ? (
                                <EyeOff className="h-4 w-4" />
                              ) : (
                                <Eye className="h-4 w-4" />
                              )}
                            </Button>
                          </div>
                        </FormControl>
                        <FormDescription>
                          Must be at least 8 characters with uppercase, lowercase, and number
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="confirmPassword"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Confirm Password</FormLabel>
                        <FormControl>
                          <div className="relative">
                            <Input
                              type={showConfirmPassword ? "text" : "password"}
                              placeholder="Confirm your password"
                              {...field}
                            />
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                              onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                            >
                              {showConfirmPassword ? (
                                <EyeOff className="h-4 w-4" />
                              ) : (
                                <Eye className="h-4 w-4" />
                              )}
                            </Button>
                          </div>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              )}

              {/* Step 3: Business Information */}
              {currentStep === 3 && (
                <div className="space-y-4">
                  <FormField
                    control={form.control}
                    name="businessName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Business Name</FormLabel>
                        <FormControl>
                          <Input placeholder="Your Business Name Ltd." {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="clientType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Business Type</FormLabel>
                        <Select onValueChange={field.onChange} defaultValue={field.value}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder="Select business type" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="Individual">Individual</SelectItem>
                            <SelectItem value="Partnership">Partnership</SelectItem>
                            <SelectItem value="Corporation">Corporation</SelectItem>
                            <SelectItem value="NGO">NGO</SelectItem>
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="businessAddress"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Business Address (Optional)</FormLabel>
                        <FormControl>
                          <Textarea 
                            placeholder="Enter your business address"
                            className="resize-none"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              )}

              {/* Step 4: Tax Information */}
              {currentStep === 4 && (
                <div className="space-y-4">
                  <FormField
                    control={form.control}
                    name="taxpayerCategory"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Taxpayer Category</FormLabel>
                        <Select onValueChange={field.onChange} defaultValue={field.value}>
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder="Select your business size" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="Large">Large (Annual turnover &gt; Le 3 billion)</SelectItem>
                            <SelectItem value="Medium">Medium (Annual turnover Le 500M - 3B)</SelectItem>
                            <SelectItem value="Small">Small (Annual turnover Le 50M - 500M)</SelectItem>
                            <SelectItem value="Micro">Micro (Annual turnover &lt; Le 50M)</SelectItem>
                          </SelectContent>
                        </Select>
                        <FormDescription>
                          This determines your tax obligations and filing requirements
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="taxpayerIdentificationNumber"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Taxpayer Identification Number (TIN) - Optional</FormLabel>
                        <FormControl>
                          <Input placeholder="Enter your TIN if available" {...field} />
                        </FormControl>
                        <FormDescription>
                          If you don't have a TIN yet, we can help you obtain one
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  
                  <FormField
                    control={form.control}
                    name="annualTurnover"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Estimated Annual Turnover (Optional)</FormLabel>
                        <FormControl>
                          <Input 
                            type="number" 
                            placeholder="Enter amount in Sierra Leone Leones"
                            {...field}
                            onChange={(e) => field.onChange(e.target.value ? Number(e.target.value) : undefined)}
                          />
                        </FormControl>
                        <FormDescription>
                          This helps us provide better service and compliance guidance
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              )}

              {/* Step 5: Review */}
              {currentStep === 5 && (
                <div className="space-y-6">
                  <div className="bg-gray-50 rounded-lg p-6 space-y-4">
                    <h3 className="font-semibold text-lg">Review Your Information</h3>
                    
                    <div className="grid gap-4">
                      <div>
                        <p className="font-medium">Personal Information</p>
                        <p className="text-sm text-gray-600">
                          {form.getValues('firstName')} {form.getValues('lastName')}
                        </p>
                        <p className="text-sm text-gray-600">{email}</p>
                        <p className="text-sm text-gray-600">{form.getValues('phoneNumber')}</p>
                      </div>
                      
                      <div>
                        <p className="font-medium">Business Information</p>
                        <p className="text-sm text-gray-600">{form.getValues('businessName')}</p>
                        <p className="text-sm text-gray-600">Type: {form.getValues('clientType')}</p>
                        {form.getValues('businessAddress') && (
                          <p className="text-sm text-gray-600">{form.getValues('businessAddress')}</p>
                        )}
                      </div>
                      
                      <div>
                        <p className="font-medium">Tax Information</p>
                        <p className="text-sm text-gray-600">Category: {form.getValues('taxpayerCategory')}</p>
                        {form.getValues('taxpayerIdentificationNumber') && (
                          <p className="text-sm text-gray-600">TIN: {form.getValues('taxpayerIdentificationNumber')}</p>
                        )}
                      </div>
                    </div>
                  </div>
                  
                  <div className="bg-sierra-blue/5 border border-sierra-blue/20 rounded-lg p-4">
                    <p className="text-sm text-sierra-blue">
                      By completing this registration, you agree to work with The Betts Firm 
                      for your tax compliance needs and acknowledge that the information provided is accurate.
                    </p>
                  </div>
                </div>
              )}

              {/* Navigation Buttons */}
              <div className="flex justify-between pt-6">
                <Button
                  type="button"
                  variant="outline"
                  onClick={prevStep}
                  disabled={currentStep === 1}
                >
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Previous
                </Button>

                {currentStep < steps.length ? (
                  <Button type="button" onClick={nextStep}>
                    Next
                    <ArrowRight className="ml-2 h-4 w-4" />
                  </Button>
                ) : (
                  <Button type="submit" disabled={isLoading}>
                    {isLoading ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Completing Registration...
                      </>
                    ) : (
                      <>
                        Complete Registration
                        <Check className="ml-2 h-4 w-4" />
                      </>
                    )}
                  </Button>
                )}
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}