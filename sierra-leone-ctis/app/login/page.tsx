import { LoginForm } from "@/components/login-form";
import Image from "next/image";

export default function LoginPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-blue-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="flex justify-center mb-6">
          <Image src="/logo.png" alt="Betts logo" width={64} height={64} className="rounded" />
        </div>
        <h1 className="text-3xl font-bold text-center text-gray-900 mb-8">The Betts Firm CTIS</h1>
        <LoginForm />
      </div>
    </div>
  );
}
