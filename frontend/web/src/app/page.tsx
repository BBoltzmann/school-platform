import { redirect } from "next/navigation";

import { getSessionContext } from "@/lib/auth/session";

export default async function Home() {
  const session = await getSessionContext();

  if (!session) {
    redirect("/login");
  }

  redirect(
    `/app/${session.tenantSlug}/dashboard`
  );
}
