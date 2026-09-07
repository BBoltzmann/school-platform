export type InventoryCategory = {
  id: string;
  name: string;
  description: string | null;
};

export type InventoryLocation = {
  id: string;
  name: string;
  code: string | null;
};

export type InventoryVariant = {
  id: string;
  inventoryItemId: string;
  name: string;
  sku: string | null;
  reorderLevel: number;
  costPrice: number | null;
  sellingPrice: number | null;
  totalQuantity: number;
  isLowStock: boolean;
};

export type InventoryItem = {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  unit: string;
  description: string | null;
  trackVariants: boolean;
  variants: InventoryVariant[];
};

export type InventoryListItem = {
  id: string;
  inventoryItemVariantId: string;
  itemName: string;
  variantName: string;
  unit: string;
  quantityPerRecipient: number;
  isRequired: boolean;
};

export type InventoryList = {
  id: string;
  name: string;
  listType: string;
  audienceType: string;
  audienceId: string | null;
  audienceName: string | null;
  academicSessionId: string | null;
  academicTermId: string | null;
  items: InventoryListItem[];
};

export type InventoryTransaction = {
  id: string;
  inventoryItemVariantId: string;
  itemName: string;
  variantName: string;
  type: string;
  quantity: number;
  fromLocationName: string | null;
  toLocationName: string | null;
  recipientType: string | null;
  recipientId: string | null;
  recipientName: string | null;
  unitPrice: number | null;
  notes: string | null;
  createdAtUtc: string;
};

export type InventoryDemandItem = {
  inventoryListItemId: string;
  itemName: string;
  variantName: string;
  unit: string;
  quantityPerRecipient: number;
  requiredQuantity: number;
  issuedQuantity: number;
  remainingRequirement: number;
  availableQuantity: number;
  shortfall: number;
};

export type InventoryDemand = {
  inventoryListId: string;
  listName: string;
  recipientCount: number;
  items: InventoryDemandItem[];
};

export type InventoryAudienceOption = {
  id: string;
  name: string;
};

export type InventoryRecipient = {
  id: string;
  name: string;
  reference: string;
};

export type InventorySetup = {
  categories: InventoryCategory[];
  locations: InventoryLocation[];
  items: InventoryItem[];
  lists: InventoryList[];
  recentTransactions: InventoryTransaction[];
  levels: InventoryAudienceOption[];
  classes: InventoryAudienceOption[];
  students: InventoryRecipient[];
  staff: InventoryRecipient[];
};
