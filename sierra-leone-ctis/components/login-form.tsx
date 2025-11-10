"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { useToast } from "@/hooks/use-toast";
import { AuthService } from "@/lib/services";
import { isAuthenticated } from "@/lib/api-client";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/auth-context";
import Link from "next/link";

export function LoginForm() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const { toast } = useToast();
  const { checkAuthStatus } = useAuth();
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    // Trim spaces from email and password
    const trimmedEmail = email.trim();
    const trimmedPassword = password.trim();

    try {
      console.log("Attempting login...");
      console.log("Email (length):", trimmedEmail, `(${trimmedEmail.length} chars)`);
      console.log("Password (length):", "***", `(${trimmedPassword.length} chars)`);
      
      await AuthService.login({ Email: trimmedEmail, Password: trimmedPassword });
      console.log("Login successful!");

      // Verify token was stored and authentication works
      const authenticated = isAuthenticated();
      if (!authenticated) {
        throw new Error("Authentication failed - token not stored properly");
      }

      // Update auth context state
      checkAuthStatus();

      toast({
        title: "Login successful",
        description: "You have been logged in successfully.",
        duration: 5000,
      });

      // Redirect to dashboard
      router.push("/dashboard");
    } catch (error: any) {
      console.error("Login error:", error);
      console.error("Error details:", {
        message: error.message,
        code: error.code,
        status: error.status,
        details: error.details
      });
      
      let errorMessage = "Invalid credentials. Please try again.";
      if (error.status === 401) {
        errorMessage = "Invalid email or password. Please check your credentials.";
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      toast({
        title: "Login failed",
        description: errorMessage,
        variant: "destructive",
        duration: 10000, // Longer duration to see error
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Card className="w-full max-w-md mx-auto shadow-lg">
      <CardHeader>
        <CardTitle className="text-2xl">Log in</CardTitle>
        <CardDescription>Enter your email and password to log in to your account</CardDescription>
      </CardHeader>
      <form onSubmit={handleSubmit} data-testid="login-form">
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              name="email"
              data-testid="email-input"
              placeholder="name@example.com"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              disabled={isLoading}
            />
          </div>
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label htmlFor="password">Password</Label>
              <Link href="/forgot-password" className="text-sm text-blue-600 hover:text-blue-500">
                Forgot password?
              </Link>
            </div>
            <Input
              id="password"
              name="password"
              data-testid="password-input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              disabled={isLoading}
            />
          </div>
        </CardContent>
        <CardFooter className="flex flex-col space-y-4">
          <Button type="submit" className="w-full" disabled={isLoading} data-testid="login-button">
            {isLoading ? "Logging in..." : "Log in"}
          </Button>
          <div className="text-center text-sm">
            Don't have an account?{" "}
            <Link href="/register" className="text-blue-600 hover:text-blue-500">
              Register
            </Link>
          </div>
        </CardFooter>
      </form>
    </Card>
  );
}
