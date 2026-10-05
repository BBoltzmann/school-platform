type TenantBrandProps = {
  name: string;
  logoDataUrl?: string | null;
};

function initials(name: string) {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length > 1) return words.slice(0, 3).map(word => word[0]).join("").toUpperCase();
  return name.replace(/[^a-z0-9]/gi, "").slice(0, 3).toUpperCase() || "SCH";
}

export function TenantBrand({ name, logoDataUrl }: TenantBrandProps) {
  return (
    <div className="flex min-w-0 items-center gap-3">
      {logoDataUrl ? <img src={logoDataUrl} alt={`${name} logo`} className="h-10 w-10 shrink-0 rounded-md border border-white/20 object-contain" /> : <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-white/20 bg-tenant-primary text-xs font-black text-tenant-primary-foreground">{initials(name)}</div>}

      <div className="min-w-0">
        <div className="truncate text-base font-semibold text-white">
          {name}
        </div>
        <div className="text-xs text-white/55">
          School Administration
        </div>
      </div>
    </div>
  );
}
