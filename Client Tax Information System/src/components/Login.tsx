import { useState } from "react";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "./ui/card";
import logo from "figma:asset/c09e3416d3f18d5dd7594d245d067b31f50605af.png";

interface LoginProps {
  onLogin: (role: "client" | "staff") => void;
}

export function Login({ onLogin }: LoginProps) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    // For demo purposes, staff emails have @bettsfirm.com
    const role = email.includes("@bettsfirm.com") ? "staff" : "client";
    onLogin(role);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-primary/5 via-background to-info/5 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-4 text-center">
          <div className="flex justify-center mb-2">
            <img src={logo} alt="The Betts Firm" className="h-12" />
          </div>
          <CardTitle>Client Tax Information System</CardTitle>
          <CardDescription>
            Sign in to access your tax management dashboard
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleLogin} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email Address</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <Button type="submit" className="w-full">
              Sign In
            </Button>
            <div className="text-center">
              <Button variant="link" type="button" className="text-sm">
                Forgot password?
              </Button>
            </div>
          </form>

          <div className="mt-6 p-4 bg-muted/50 rounded-lg">
            <p className="text-sm font-medium mb-2">Demo Credentials</p>
            <div className="text-xs space-y-1 text-muted-foreground">
              <p>
                <strong>Staff:</strong> staff@bettsfirm.com / password
              </p>
              <p>
                <strong>Client:</strong> client@example.com / password
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
