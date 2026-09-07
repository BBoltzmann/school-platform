type TenantBrandProps = {
  name: string;
};

export function TenantBrand({ name }: TenantBrandProps) {
  return (
    <div className="flex min-w-0 items-center gap-3">
      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md border border-white/20 bg-tenant-primary text-xs font-black text-tenant-primary-foreground">
        ARC
      </div>

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
