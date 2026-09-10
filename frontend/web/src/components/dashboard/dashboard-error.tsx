"use client";
import { useRouter } from "next/navigation";
import { useState } from "react";

export function DashboardError() {
  const router = useRouter();
  const [retrying, setRetrying] = useState(false);
  return <div className="rounded-xl border border-red-200 bg-red-50 p-8 text-center">
    <h2 className="text-lg font-semibold text-red-900">Dashboard data could not be loaded.</h2>
    <p className="mt-2 text-sm text-red-800">Check your connection and try again.</p>
    <button type="button" disabled={retrying} onClick={() => { setRetrying(true); router.refresh(); }} className="mt-5 rounded-md bg-black px-4 py-2 text-sm font-semibold text-white disabled:opacity-60">{retrying ? "Retrying…" : "Retry"}</button>
  </div>;
}
