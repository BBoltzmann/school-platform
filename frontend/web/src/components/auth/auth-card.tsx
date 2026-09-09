import Link from "next/link";
import type { ReactNode } from "react";

export function AuthCard({ title, description, children }: { title: string; description: string; children: ReactNode }) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 py-12">
      <section className="w-full max-w-md space-y-6 rounded-xl border p-6 shadow-sm sm:p-8">
        <div className="space-y-2">
          <p className="text-sm font-semibold text-muted-foreground">School Platform</p>
          <h1 className="text-2xl font-bold">{title}</h1>
          <p className="text-sm text-muted-foreground">{description}</p>
        </div>
        {children}
        <Link href="/login" className="block text-sm underline underline-offset-4">Back to sign in</Link>
      </section>
    </main>
  );
}
