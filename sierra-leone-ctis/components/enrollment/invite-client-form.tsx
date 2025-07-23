'use client';

import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, Mail, UserPlus } from 'lucide-react';

import { Button } from '@/components/ui/button';
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
import { useToast } from '@/hooks/use-toast';
import { EnrollmentService } from '@/lib/services/enrollment-service';
import {
  inviteClientSchema,
  type InviteClientFormData,
} from '@/lib/validations/enrollment';

interface InviteClientFormProps {
  onSuccess?: () => void;
}

export function InviteClientForm({ onSuccess }: InviteClientFormProps) {
  const [isLoading, setIsLoading] = useState(false);
  const { toast } = useToast();

  const form = useForm<InviteClientFormData>({
    resolver: zodResolver(inviteClientSchema),
    defaultValues: {
      email: '',
    },
  });

  const onSubmit = async (data: InviteClientFormData) => {
    setIsLoading(true);
    try {
      await EnrollmentService.sendInvitation(data);
      
      toast({
        title: 'Invitation Sent',
        description: `A registration invitation has been sent to ${data.email}`,
        variant: 'default',
      });

      form.reset();
      onSuccess?.();
    } catch (error: any) {
      console.error('Failed to send invitation:', error);
      
      toast({
        title: 'Error',
        description: error.message || 'Failed to send invitation. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <div className="mx-auto w-12 h-12 bg-sierra-blue/10 rounded-lg flex items-center justify-center">
          <UserPlus className="h-6 w-6 text-sierra-blue" />
        </div>
        <h2 className="text-2xl font-bold tracking-tight">Invite New Client</h2>
        <p className="text-muted-foreground">
          Send a secure invitation link to a prospective client to join the system
        </p>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Client Email Address</FormLabel>
                <FormControl>
                  <div className="relative">
                    <Mail className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                    <Input
                      type="email"
                      placeholder="client@example.com"
                      className="pl-10"
                      {...field}
                    />
                  </div>
                </FormControl>
                <FormDescription>
                  We'll send a secure registration link to this email address. The link will expire in 48 hours.
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <div className="bg-sierra-gold/5 border border-sierra-gold/20 rounded-lg p-4">
            <div className="flex items-start space-x-3">
              <div className="w-2 h-2 rounded-full bg-sierra-gold flex-shrink-0 mt-2" />
              <div className="text-sm text-sierra-gold">
                <p className="font-medium mb-1">What happens next:</p>
                <ul className="list-disc list-inside space-y-1 text-sierra-gold/80">
                  <li>Client receives a professional invitation email</li>
                  <li>They click the secure link to complete registration</li>
                  <li>You'll be notified when registration is complete</li>
                  <li>Client is automatically assigned to you</li>
                </ul>
              </div>
            </div>
          </div>

          <Button 
            type="submit" 
            className="w-full" 
            disabled={isLoading}
          >
            {isLoading ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Sending Invitation...
              </>
            ) : (
              <>
                <Mail className="mr-2 h-4 w-4" />
                Send Invitation
              </>
            )}
          </Button>
        </form>
      </Form>
    </div>
  );
}