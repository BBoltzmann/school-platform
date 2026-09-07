import {
  BookOpenCheck,
  GraduationCap,
  ShieldCheck,
} from "lucide-react";

import { LoginForm } from "@/components/auth/login-form";

export default function LoginPage() {
  return (
    <main className="grid min-h-screen bg-background lg:grid-cols-[1.05fr_0.95fr]">
      <section className="relative hidden overflow-hidden bg-black p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(245,217,0,0.16),transparent_35%)]" />

        <div className="relative z-10">
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-lg bg-tenant-primary font-black text-black">
              ARC
            </div>

            <div>
              <div className="text-xl font-bold">
                Antioch Royal College
              </div>

              <div className="mt-1 text-sm text-white/55">
                School Administration Platform
              </div>
            </div>
          </div>
        </div>

        <div className="relative z-10 max-w-xl">
          <div className="mb-5 text-sm font-bold uppercase tracking-[0.18em] text-tenant-primary">
            Godliness · Diligence · Excellence
          </div>

          <h1 className="text-4xl font-bold leading-tight xl:text-5xl">
            Manage your school
            <br />
            from one place.
          </h1>

          <p className="mt-6 max-w-lg text-base leading-7 text-white/65">
            Admissions, students, academics,
            finance, inventory and school
            operations in one secure platform.
          </p>

          <div className="mt-10 grid gap-4 sm:grid-cols-3">
            <div className="rounded-xl border border-white/10 bg-white/5 p-4">
              <GraduationCap className="mb-3 h-5 w-5 text-tenant-primary" />
              <div className="text-sm font-semibold">
                Students
              </div>
            </div>

            <div className="rounded-xl border border-white/10 bg-white/5 p-4">
              <BookOpenCheck className="mb-3 h-5 w-5 text-tenant-primary" />
              <div className="text-sm font-semibold">
                Academics
              </div>
            </div>

            <div className="rounded-xl border border-white/10 bg-white/5 p-4">
              <ShieldCheck className="mb-3 h-5 w-5 text-tenant-primary" />
              <div className="text-sm font-semibold">
                Secure
              </div>
            </div>
          </div>
        </div>

        <div className="relative z-10 text-xs text-white/35">
          Antioch Royal College
        </div>
      </section>

      <section className="flex items-center justify-center px-6 py-12 sm:px-10">
        <div className="w-full max-w-md">
          <div className="mb-9 lg:hidden">
            <div className="flex items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-md bg-tenant-primary font-black text-black">
                ARC
              </div>

              <div>
                <div className="font-bold">
                  Antioch Royal College
                </div>

                <div className="text-xs text-muted-foreground">
                  School Administration
                </div>
              </div>
            </div>
          </div>

          <div className="mb-8">
            <h2 className="text-3xl font-bold tracking-tight">
              Welcome back
            </h2>

            <p className="mt-2 text-sm leading-6 text-muted-foreground">
              Sign in to access the Antioch
              Royal College administration
              portal.
            </p>
          </div>

          <LoginForm />

          <div className="mt-8 border-t pt-6 text-center text-xs text-muted-foreground">
            Having trouble signing in?
            Contact your school administrator.
          </div>
        </div>
      </section>
    </main>
  );
}
