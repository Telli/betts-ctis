namespace BettsTax.Core.Services
{
    public class EmailTemplateService
    {
        public string GetClientInvitationTemplate(string registrationUrl, string associateName)
        {
            return $@"
                <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background: #f8fafc;"">
                    <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: bold;"">The Betts Firm</h1>
                        <p style=""color: rgba(255,255,255,0.9); margin: 0.5rem 0 0 0; font-size: 16px;"">Sierra Leone Tax Information System</p>
                    </div>
                    <div style=""padding: 2rem; background: white; margin: 0;"">
                        <h2 style=""color: #1e40af; margin-top: 0;"">You're Invited to Join!</h2>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">Hello,</p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            You have been invited by <strong>{associateName}</strong> to join The Betts Firm's Client Tax Information System. 
                            Our platform will help you manage your tax obligations and stay compliant with Sierra Leone tax regulations.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            To complete your registration and access your client portal, please click the button below:
                        </p>
                        <div style=""text-align: center; margin: 2rem 0;"">
                            <a href=""{registrationUrl}"" 
                               style=""background: #1e40af; color: white; padding: 1rem 2rem; text-decoration: none; border-radius: 0.5rem; font-weight: bold; display: inline-block; font-size: 16px;"">
                                Complete Registration
                            </a>
                        </div>
                        <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 1rem; margin: 1.5rem 0;"">
                            <p style=""margin: 0; color: #92400e; font-weight: bold;"">⚠️ Important Security Notice:</p>
                            <p style=""margin: 0.5rem 0 0 0; color: #92400e;"">This invitation link will expire in 48 hours for security purposes. Please complete your registration promptly.</p>
                        </div>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            If you have any questions or need assistance, please don't hesitate to contact our support team.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151; margin-bottom: 0;"">
                            Best regards,<br>
                            <strong>The Betts Firm Team</strong><br>
                            Sierra Leone
                        </p>
                    </div>
                    <div style=""background: #f3f4f6; padding: 1rem; text-align: center;"">
                        <p style=""margin: 0; font-size: 12px; color: #6b7280;"">
                            © 2025 The Betts Firm. All rights reserved.<br>
                            This is an automated message. Please do not reply to this email.
                        </p>
                    </div>
                </div>";
        }

        public string GetWelcomeEmailTemplate(string clientName)
        {
            return $@"
                <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background: #f8fafc;"">
                    <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: bold;"">Welcome to The Betts Firm!</h1>
                        <p style=""color: rgba(255,255,255,0.9); margin: 0.5rem 0 0 0; font-size: 16px;"">Your registration is complete</p>
                    </div>
                    <div style=""padding: 2rem; background: white; margin: 0;"">
                        <h2 style=""color: #1e40af; margin-top: 0;"">Hello {clientName}!</h2>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            Congratulations! Your account has been successfully created and verified. You now have access to 
                            The Betts Firm's comprehensive tax information system.
                        </p>
                        <h3 style=""color: #1e40af; margin-top: 2rem;"">What's Next?</h3>
                        <ul style=""font-size: 16px; line-height: 1.8; color: #374151; padding-left: 1.5rem;"">
                            <li>Log in to your client portal to view your tax dashboard</li>
                            <li>Upload required documents for your tax filings</li>
                            <li>Track your compliance status and deadlines</li>
                            <li>Communicate directly with your assigned tax associate</li>
                            <li>Make secure payments and track payment history</li>
                        </ul>
                        <div style=""background: #ecfdf5; border-left: 4px solid #10b981; padding: 1rem; margin: 1.5rem 0;"">
                            <p style=""margin: 0; color: #047857; font-weight: bold;"">🎉 You're all set!</p>
                            <p style=""margin: 0.5rem 0 0 0; color: #047857;"">Your dedicated tax associate will be in touch soon to guide you through the next steps.</p>
                        </div>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            If you have any questions or need immediate assistance, please don't hesitate to contact us.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151; margin-bottom: 0;"">
                            Welcome aboard!<br>
                            <strong>The Betts Firm Team</strong><br>
                            Sierra Leone
                        </p>
                    </div>
                    <div style=""background: #f3f4f6; padding: 1rem; text-align: center;"">
                        <p style=""margin: 0; font-size: 12px; color: #6b7280;"">
                            © 2025 The Betts Firm. All rights reserved.<br>
                            This is an automated message. Please do not reply to this email.
                        </p>
                    </div>
                </div>";
        }

        public string GetEmailVerificationTemplate(string verificationUrl)
        {
            return $@"
                <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background: #f8fafc;"">
                    <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: bold;"">Verify Your Email</h1>
                        <p style=""color: rgba(255,255,255,0.9); margin: 0.5rem 0 0 0; font-size: 16px;"">The Betts Firm</p>
                    </div>
                    <div style=""padding: 2rem; background: white; margin: 0;"">
                        <h2 style=""color: #1e40af; margin-top: 0;"">Almost There!</h2>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            Please verify your email address to complete your registration with The Betts Firm's 
                            Client Tax Information System.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            Click the button below to verify your email address and activate your account:
                        </p>
                        <div style=""text-align: center; margin: 2rem 0;"">
                            <a href=""{verificationUrl}"" 
                               style=""background: #1e40af; color: white; padding: 1rem 2rem; text-decoration: none; border-radius: 0.5rem; font-weight: bold; display: inline-block; font-size: 16px;"">
                                Verify Email Address
                            </a>
                        </div>
                        <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 1rem; margin: 1.5rem 0;"">
                            <p style=""margin: 0; color: #92400e; font-weight: bold;"">⏰ Time Sensitive:</p>
                            <p style=""margin: 0.5rem 0 0 0; color: #92400e;"">This verification link will expire in 24 hours. Please verify your email promptly.</p>
                        </div>
                        <p style=""font-size: 14px; color: #6b7280; line-height: 1.6;"">
                            If the button doesn't work, copy and paste this link into your browser:<br>
                            <span style=""word-break: break-all;"">{verificationUrl}</span>
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151; margin-bottom: 0;"">
                            Best regards,<br>
                            <strong>The Betts Firm Team</strong>
                        </p>
                    </div>
                    <div style=""background: #f3f4f6; padding: 1rem; text-align: center;"">
                        <p style=""margin: 0; font-size: 12px; color: #6b7280;"">
                            © 2025 The Betts Firm. All rights reserved.<br>
                            If you didn't request this email, please ignore it.
                        </p>
                    </div>
                </div>";
        }

        public string GetRegistrationCompletedNotificationTemplate(string clientName)
        {
            return $@"
                <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background: #f8fafc;"">
                    <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: bold;"">New Client Registration</h1>
                        <p style=""color: rgba(255,255,255,0.9); margin: 0.5rem 0 0 0; font-size: 16px;"">Associate Notification</p>
                    </div>
                    <div style=""padding: 2rem; background: white; margin: 0;"">
                        <h2 style=""color: #1e40af; margin-top: 0;"">Registration Completed</h2>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            A new client has successfully completed their registration:
                        </p>
                        <div style=""background: #f0f9ff; border: 1px solid #0284c7; border-radius: 0.5rem; padding: 1.5rem; margin: 1.5rem 0;"">
                            <p style=""margin: 0; font-size: 18px; font-weight: bold; color: #0284c7;"">
                                📋 Client Name: {clientName}
                            </p>
                            <p style=""margin: 0.5rem 0 0 0; color: #0369a1;"">
                                Status: ✅ Registration Complete
                            </p>
                        </div>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            Please review their information and follow up as appropriate. The client now has access to their portal and is awaiting next steps.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151; margin-bottom: 0;"">
                            Best regards,<br>
                            <strong>The Betts Firm System</strong>
                        </p>
                    </div>
                    <div style=""background: #f3f4f6; padding: 1rem; text-align: center;"">
                        <p style=""margin: 0; font-size: 12px; color: #6b7280;"">
                            © 2025 The Betts Firm. All rights reserved.<br>
                            This is an automated notification.
                        </p>
                    </div>
                </div>";
        }

        public string GetPasswordResetTemplate(string resetUrl)
        {
            return $@"
                <div style=""max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; background: #f8fafc;"">
                    <div style=""background: linear-gradient(135deg, #1e40af 0%, #059669 100%); padding: 2rem; text-align: center;"">
                        <h1 style=""color: white; margin: 0; font-size: 28px; font-weight: bold;"">Password Reset</h1>
                        <p style=""color: rgba(255,255,255,0.9); margin: 0.5rem 0 0 0; font-size: 16px;"">The Betts Firm</p>
                    </div>
                    <div style=""padding: 2rem; background: white; margin: 0;"">
                        <h2 style=""color: #1e40af; margin-top: 0;"">Reset Your Password</h2>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            We received a request to reset your password for your Betts Firm account. 
                            Click the button below to create a new password:
                        </p>
                        <div style=""text-align: center; margin: 2rem 0;"">
                            <a href=""{resetUrl}"" 
                               style=""background: #1e40af; color: white; padding: 1rem 2rem; text-decoration: none; border-radius: 0.5rem; font-weight: bold; display: inline-block; font-size: 16px;"">
                                Reset Password
                            </a>
                        </div>
                        <div style=""background: #fef2f2; border-left: 4px solid #ef4444; padding: 1rem; margin: 1.5rem 0;"">
                            <p style=""margin: 0; color: #dc2626; font-weight: bold;"">🔒 Security Notice:</p>
                            <p style=""margin: 0.5rem 0 0 0; color: #dc2626;"">This password reset link will expire in 1 hour for security purposes.</p>
                        </div>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151;"">
                            If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
                        </p>
                        <p style=""font-size: 16px; line-height: 1.6; color: #374151; margin-bottom: 0;"">
                            Best regards,<br>
                            <strong>The Betts Firm Team</strong>
                        </p>
                    </div>
                    <div style=""background: #f3f4f6; padding: 1rem; text-align: center;"">
                        <p style=""margin: 0; font-size: 12px; color: #6b7280;"">
                            © 2025 The Betts Firm. All rights reserved.<br>
                            If you didn't request this email, please ignore it.
                        </p>
                    </div>
                </div>";
        }
    }
}