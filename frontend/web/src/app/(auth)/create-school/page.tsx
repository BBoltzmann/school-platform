import { notFound } from "next/navigation";
import { AuthCard } from "@/components/auth/auth-card";
import { CreateSchoolForm } from "@/components/auth/create-school-form";

export const dynamic = "force-dynamic";

export default function CreateSchoolPage() {
  if (process.env.ALLOW_PUBLIC_SCHOOL_SIGNUP !== "true") notFound();
  return <AuthCard title="Create your school" description="Create a new school and its first administrator. To join an existing school, contact its administrator for an invitation.">
    <CreateSchoolForm />
  </AuthCard>;
}
