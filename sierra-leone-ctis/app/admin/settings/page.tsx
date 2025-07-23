'use client';

import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useToast } from '@/hooks/use-toast';
import { Loader2, Mail, Send, Settings, Shield } from 'lucide-react';
import { AdminSettingsService, type EmailSettingsDto, type TestEmailDto } from '@/lib/services/admin-settings-service';

const emailSettingsSchema = z.object({
  smtpHost: z.string().min(1, 'SMTP Host is required'),
  smtpPort: z.number().min(1).max(65535, 'Port must be between 1 and 65535'),
  username: z.string().min(1, 'Username is required'),
  password: z.string().min(1, 'Password is required'),
  fromEmail: z.string().email('Invalid email address'),
  fromName: z.string().min(1, 'From name is required'),
  useSSL: z.boolean(),
  useTLS: z.boolean(),
});

const testEmailSchema = z.object({
  toEmail: z.string().email('Invalid email address'),
  subject: z.string().optional(),
  body: z.string().optional(),
});

type EmailSettingsFormData = z.infer<typeof emailSettingsSchema>;
type TestEmailFormData = z.infer<typeof testEmailSchema>;

export default function AdminSettingsPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [isTestingEmail, setIsTestingEmail] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const { toast } = useToast();

  const emailForm = useForm<EmailSettingsFormData>({
    resolver: zodResolver(emailSettingsSchema),
    defaultValues: {
      smtpHost: '',
      smtpPort: 587,
      username: '',
      password: '',
      fromEmail: 'noreply@thebettsfirmsl.com',
      fromName: 'The Betts Firm',
      useSSL: true,
      useTLS: true,
    },
  });

  const testEmailForm = useForm<TestEmailFormData>({
    resolver: zodResolver(testEmailSchema),
    defaultValues: {
      toEmail: '',
      subject: 'Test Email from The Betts Firm',
      body: 'This is a test email to verify your email configuration.',
    },
  });

  useEffect(() => {
    loadEmailSettings();
  }, []);

  const loadEmailSettings = async () => {
    try {
      setIsLoading(true);
      const settings = await AdminSettingsService.getEmailSettings();
      
      emailForm.reset({
        smtpHost: settings.smtpHost,
        smtpPort: settings.smtpPort,
        username: settings.username,
        password: settings.password,
        fromEmail: settings.fromEmail,
        fromName: settings.fromName,
        useSSL: settings.useSSL,
        useTLS: settings.useTLS,
      });
    } catch (error: any) {
      toast({
        title: 'Error',
        description: 'Failed to load email settings',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  const onSaveEmailSettings = async (data: EmailSettingsFormData) => {
    try {
      setIsSaving(true);
      await AdminSettingsService.updateEmailSettings(data);
      
      toast({
        title: 'Settings Saved',
        description: 'Email settings have been updated successfully',
      });
    } catch (error: any) {
      toast({
        title: 'Error',
        description: error.message || 'Failed to save email settings',
        variant: 'destructive',
      });
    } finally {
      setIsSaving(false);
    }
  };

  const onSendTestEmail = async (data: TestEmailFormData) => {
    try {
      setIsTestingEmail(true);
      await AdminSettingsService.sendTestEmail(data);
      
      toast({
        title: 'Test Email Sent',
        description: `Test email sent successfully to ${data.toEmail}`,
      });
      
      testEmailForm.reset();
    } catch (error: any) {
      toast({
        title: 'Test Email Failed',
        description: error.message || 'Failed to send test email',
        variant: 'destructive',
      });
    } finally {
      setIsTestingEmail(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex items-center space-x-2">
        <Settings className="h-6 w-6" />
        <h1 className="text-3xl font-bold">Admin Settings</h1>
      </div>

      <Tabs defaultValue="email" className="space-y-4">
        <TabsList>
          <TabsTrigger value="email" className="flex items-center space-x-2">
            <Mail className="h-4 w-4" />
            <span>Email Settings</span>
          </TabsTrigger>
          <TabsTrigger value="security" className="flex items-center space-x-2">
            <Shield className="h-4 w-4" />
            <span>Security</span>
          </TabsTrigger>
        </TabsList>

        <TabsContent value="email" className="space-y-6">
          <div className="grid gap-6 md:grid-cols-2">
            {/* Email Configuration */}
            <Card>
              <CardHeader>
                <CardTitle>SMTP Configuration</CardTitle>
                <CardDescription>
                  Configure your SMTP server settings for sending emails
                </CardDescription>
              </CardHeader>
              <CardContent>
                <form onSubmit={emailForm.handleSubmit(onSaveEmailSettings)} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="smtpHost">SMTP Host</Label>
                    <Input
                      id="smtpHost"
                      placeholder="smtp.gmail.com"
                      {...emailForm.register('smtpHost')}
                    />
                    {emailForm.formState.errors.smtpHost && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.smtpHost.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="smtpPort">SMTP Port</Label>
                    <Input
                      id="smtpPort"
                      type="number"
                      placeholder="587"
                      {...emailForm.register('smtpPort', { valueAsNumber: true })}
                    />
                    {emailForm.formState.errors.smtpPort && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.smtpPort.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="username">Username</Label>
                    <Input
                      id="username"
                      placeholder="your-email@gmail.com"
                      {...emailForm.register('username')}
                    />
                    {emailForm.formState.errors.username && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.username.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="password">Password</Label>
                    <Input
                      id="password"
                      type="password"
                      placeholder="Your SMTP password"
                      {...emailForm.register('password')}
                    />
                    {emailForm.formState.errors.password && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.password.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="fromEmail">From Email</Label>
                    <Input
                      id="fromEmail"
                      type="email"
                      placeholder="noreply@thebettsfirmsl.com"
                      {...emailForm.register('fromEmail')}
                    />
                    {emailForm.formState.errors.fromEmail && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.fromEmail.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="fromName">From Name</Label>
                    <Input
                      id="fromName"
                      placeholder="The Betts Firm"
                      {...emailForm.register('fromName')}
                    />
                    {emailForm.formState.errors.fromName && (
                      <p className="text-sm text-red-600">{emailForm.formState.errors.fromName.message}</p>
                    )}
                  </div>

                  <div className="flex items-center space-x-4">
                    <div className="flex items-center space-x-2">
                      <Switch
                        id="useSSL"
                        checked={emailForm.watch('useSSL')}
                        onCheckedChange={(checked) => emailForm.setValue('useSSL', checked)}
                      />
                      <Label htmlFor="useSSL">Use SSL</Label>
                    </div>

                    <div className="flex items-center space-x-2">
                      <Switch
                        id="useTLS"
                        checked={emailForm.watch('useTLS')}
                        onCheckedChange={(checked) => emailForm.setValue('useTLS', checked)}
                      />
                      <Label htmlFor="useTLS">Use TLS</Label>
                    </div>
                  </div>

                  <Button type="submit" disabled={isSaving} className="w-full">
                    {isSaving ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      'Save Email Settings'
                    )}
                  </Button>
                </form>
              </CardContent>
            </Card>

            {/* Test Email */}
            <Card>
              <CardHeader>
                <CardTitle>Test Email Configuration</CardTitle>
                <CardDescription>
                  Send a test email to verify your SMTP settings
                </CardDescription>
              </CardHeader>
              <CardContent>
                <form onSubmit={testEmailForm.handleSubmit(onSendTestEmail)} className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="toEmail">Test Email Address</Label>
                    <Input
                      id="toEmail"
                      type="email"
                      placeholder="test@example.com"
                      {...testEmailForm.register('toEmail')}
                    />
                    {testEmailForm.formState.errors.toEmail && (
                      <p className="text-sm text-red-600">{testEmailForm.formState.errors.toEmail.message}</p>
                    )}
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="subject">Subject (Optional)</Label>
                    <Input
                      id="subject"
                      placeholder="Test Email from The Betts Firm"
                      {...testEmailForm.register('subject')}
                    />
                  </div>

                  <Button type="submit" disabled={isTestingEmail} className="w-full">
                    {isTestingEmail ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Sending Test Email...
                      </>
                    ) : (
                      <>
                        <Send className="mr-2 h-4 w-4" />
                        Send Test Email
                      </>
                    )}
                  </Button>
                </form>

                <div className="mt-4 p-4 bg-blue-50 rounded-lg">
                  <h4 className="font-medium text-blue-900 mb-2">Common SMTP Settings:</h4>
                  <div className="text-sm text-blue-700 space-y-1">
                    <p><strong>Gmail:</strong> smtp.gmail.com:587 (TLS) or smtp.gmail.com:465 (SSL)</p>
                    <p><strong>Outlook:</strong> smtp-mail.outlook.com:587 (TLS)</p>
                    <p><strong>Yahoo:</strong> smtp.mail.yahoo.com:587 (TLS)</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="security">
          <Card>
            <CardHeader>
              <CardTitle>Security Settings</CardTitle>
              <CardDescription>
                Configure security-related settings (Coming Soon)
              </CardDescription>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">Security settings panel will be available in a future update.</p>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}