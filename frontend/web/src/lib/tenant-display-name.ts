export function tenantDisplayName(slug: string) {
  if (slug === "antioch-college") return "Antioch Royal College";
  return slug.split("-").map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(" ");
}
