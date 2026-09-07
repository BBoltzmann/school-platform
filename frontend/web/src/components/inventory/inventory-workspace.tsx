"use client";

import {
  useMemo,
  useState,
} from "react";
import {
  AlertTriangle,
  Boxes,
  ClipboardList,
  History,
  LoaderCircle,
  MapPin,
  PackagePlus,
  Plus,
  Truck,
} from "lucide-react";

import { Button } from "@/components/ui/button";

import type {
  InventoryDemand,
  InventoryItem,
  InventorySetup,
} from "@/types/inventory";

type Tab =
  | "overview"
  | "items"
  | "lists"
  | "movement"
  | "history";

const MOVEMENT_TYPES = [
  "StockIn",
  "Issue",
  "Sale",
  "Return",
  "Transfer",
  "Consumed",
  "Damaged",
  "Lost",
  "AdjustmentIn",
  "AdjustmentOut",
];

export function InventoryWorkspace({
  initialSetup,
}: {
  initialSetup: InventorySetup;
}) {
  const [setup, setSetup] =
    useState(initialSetup);

  const [tab, setTab] =
    useState<Tab>("overview");

  const [error, setError] =
    useState<string | null>(null);

  const [notice, setNotice] =
    useState<string | null>(null);

  const [saving, setSaving] =
    useState(false);

  const lowStockCount =
    setup.items.flatMap(
      x => x.variants
    ).filter(
      x => x.isLowStock
    ).length;

  const totalStock =
    setup.items
      .flatMap(x => x.variants)
      .reduce(
        (total, variant) =>
          total +
          Number(
            variant.totalQuantity
          ),
        0
      );

  function clearMessages() {
    setError(null);
    setNotice(null);
  }

  return (
    <div className="space-y-6">
      <div className="overflow-x-auto">
        <div className="inline-flex min-w-max rounded-xl border bg-card p-1">
          <TabButton
            active={tab === "overview"}
            onClick={() =>
              setTab("overview")
            }
          >
            Overview
          </TabButton>

          <TabButton
            active={tab === "items"}
            onClick={() =>
              setTab("items")
            }
          >
            Items & Variants
          </TabButton>

          <TabButton
            active={tab === "lists"}
            onClick={() =>
              setTab("lists")
            }
          >
            Inventory Lists
          </TabButton>

          <TabButton
            active={tab === "movement"}
            onClick={() =>
              setTab("movement")
            }
          >
            Stock Movement
          </TabButton>

          <TabButton
            active={tab === "history"}
            onClick={() =>
              setTab("history")
            }
          >
            History
          </TabButton>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          {error}
        </div>
      )}

      {notice && (
        <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-sm text-green-700">
          {notice}
        </div>
      )}

      {tab === "overview" && (
        <Overview
          setup={setup}
          totalStock={totalStock}
          lowStockCount={
            lowStockCount
          }
        />
      )}

      {tab === "items" && (
        <ItemsPanel
          setup={setup}
          setSetup={setSetup}
          saving={saving}
          setSaving={setSaving}
          setError={setError}
          setNotice={setNotice}
          clearMessages={
            clearMessages
          }
        />
      )}

      {tab === "lists" && (
        <ListsPanel
          setup={setup}
          setSetup={setSetup}
          saving={saving}
          setSaving={setSaving}
          setError={setError}
          setNotice={setNotice}
          clearMessages={
            clearMessages
          }
        />
      )}

      {tab === "movement" && (
        <MovementPanel
          setup={setup}
          saving={saving}
          setSaving={setSaving}
          setError={setError}
          setNotice={setNotice}
          clearMessages={
            clearMessages
          }
        />
      )}

      {tab === "history" && (
        <HistoryPanel
          setup={setup}
        />
      )}
    </div>
  );
}

function Overview({
  setup,
  totalStock,
  lowStockCount,
}: {
  setup: InventorySetup;
  totalStock: number;
  lowStockCount: number;
}) {
  const lowItems =
    setup.items.flatMap(item =>
      item.variants
        .filter(
          variant =>
            variant.isLowStock
        )
        .map(
          variant => ({
            item,
            variant,
          })
        )
    );

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <Metric
          label="Categories"
          value={
            setup.categories.length
          }
        />

        <Metric
          label="Inventory Items"
          value={setup.items.length}
        />

        <Metric
          label="Stock Locations"
          value={
            setup.locations.length
          }
        />

        <Metric
          label="Total Stock"
          value={format(totalStock)}
        />

        <Metric
          label="Low Stock"
          value={lowStockCount}
        />
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <section className="rounded-xl border bg-card">
          <SectionHeader
            icon={
              <AlertTriangle className="h-5 w-5" />
            }
            title="Low Stock"
            subtitle="Items at or below their reorder level."
          />

          {lowItems.length === 0 ? (
            <Empty>
              No low-stock items.
            </Empty>
          ) : (
            <div className="divide-y">
              {lowItems
                .slice(0, 10)
                .map(
                  ({
                    item,
                    variant,
                  }) => (
                    <div
                      key={
                        variant.id
                      }
                      className="flex items-center justify-between gap-4 p-4"
                    >
                      <div>
                        <div className="font-medium">
                          {item.name}
                        </div>

                        <div className="mt-1 text-xs text-muted-foreground">
                          {variant.name}
                          {" · "}
                          {
                            item.categoryName
                          }
                        </div>
                      </div>

                      <div className="text-right">
                        <div className="font-semibold text-amber-600">
                          {format(
                            variant.totalQuantity
                          )}{" "}
                          {item.unit}
                        </div>

                        <div className="text-xs text-muted-foreground">
                          Reorder:{" "}
                          {format(
                            variant.reorderLevel
                          )}
                        </div>
                      </div>
                    </div>
                  )
                )}
            </div>
          )}
        </section>

        <section className="rounded-xl border bg-card">
          <SectionHeader
            icon={
              <ClipboardList className="h-5 w-5" />
            }
            title="Inventory Lists"
            subtitle="Requirement lists created by the school."
          />

          {setup.lists.length ===
          0 ? (
            <Empty>
              No inventory lists.
            </Empty>
          ) : (
            <div className="divide-y">
              {setup.lists
                .slice(0, 10)
                .map(list => (
                  <div
                    key={list.id}
                    className="p-4"
                  >
                    <div className="font-medium">
                      {list.name}
                    </div>

                    <div className="mt-1 text-xs text-muted-foreground">
                      {list.listType}
                      {" · "}
                      {list.audienceName ??
                        list.audienceType}
                      {" · "}
                      {list.items.length}{" "}
                      items
                    </div>
                  </div>
                ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

type CommonProps = {
  setup: InventorySetup;
  saving: boolean;
  setSaving: React.Dispatch<
    React.SetStateAction<boolean>
  >;
  setError: React.Dispatch<
    React.SetStateAction<
      string | null
    >
  >;
  setNotice: React.Dispatch<
    React.SetStateAction<
      string | null
    >
  >;
  clearMessages: () => void;
};

type EditableProps =
  CommonProps & {
    setSetup: React.Dispatch<
      React.SetStateAction<InventorySetup>
    >;
  };

function ItemsPanel({
  setup,
  setSetup,
  saving,
  setSaving,
  setError,
  setNotice,
  clearMessages,
}: EditableProps) {
  const [
    categoryName,
    setCategoryName,
  ] = useState("");

  const [
    locationName,
    setLocationName,
  ] = useState("");

  const [
    categoryId,
    setCategoryId,
  ] = useState(
    setup.categories[0]?.id ??
      ""
  );

  const [itemName, setItemName] =
    useState("");

  const [unit, setUnit] =
    useState("Piece");

  const [
    trackVariants,
    setTrackVariants,
  ] = useState(false);

  const [
    firstVariant,
    setFirstVariant,
  ] = useState("Standard");

  const [sku, setSku] =
    useState("");

  const [
    reorderLevel,
    setReorderLevel,
  ] = useState("5");

  const [
    costPrice,
    setCostPrice,
  ] = useState("");

  const [
    sellingPrice,
    setSellingPrice,
  ] = useState("");

  const [
    variantItem,
    setVariantItem,
  ] =
    useState<InventoryItem | null>(
      null
    );

  async function createCategory() {
    clearMessages();

    if (
      !categoryName.trim()
    ) {
      setError(
        "Category name is required."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/inventory/categories",
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              name:
                categoryName.trim(),
              description: null,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to create category."
        );
      }

      setSetup(current => ({
        ...current,
        categories: [
          ...current.categories,
          result,
        ],
      }));

      setCategoryId(result.id);
      setCategoryName("");

      setNotice(
        "Category created."
      );
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    } finally {
      setSaving(false);
    }
  }

  async function createLocation() {
    clearMessages();

    if (
      !locationName.trim()
    ) {
      setError(
        "Location name is required."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/inventory/locations",
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              name:
                locationName.trim(),
              code: null,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to create location."
        );
      }

      setSetup(current => ({
        ...current,
        locations: [
          ...current.locations,
          result,
        ],
      }));

      setLocationName("");

      setNotice(
        "Stock location created."
      );
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    } finally {
      setSaving(false);
    }
  }

  async function createItem() {
    clearMessages();

    if (!categoryId) {
      setError(
        "Select an inventory category."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/inventory/items",
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              categoryId,
              name:
                itemName.trim(),
              unit:
                unit.trim(),
              description: null,
              trackVariants,
              defaultVariantName:
                trackVariants
                  ? firstVariant.trim()
                  : "Standard",
              sku:
                sku.trim() ||
                null,
              reorderLevel:
                Number(
                  reorderLevel
                ),
              costPrice:
                nullableNumber(
                  costPrice
                ),
              sellingPrice:
                nullableNumber(
                  sellingPrice
                ),
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to create item."
        );
      }

      setSetup(current => ({
        ...current,
        items: [
          ...current.items,
          result,
        ],
      }));

      setItemName("");
      setSku("");
      setTrackVariants(false);
      setFirstVariant(
        "Standard"
      );

      setNotice(
        "Inventory item created."
      );
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    } finally {
      setSaving(false);
    }
  }

  async function addVariant() {
    if (!variantItem) {
      return;
    }

    clearMessages();
    setSaving(true);

    try {
      const response =
        await fetch(
          `/api/inventory/items/${variantItem.id}/variants`,
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              name:
                firstVariant.trim(),
              sku:
                sku.trim() ||
                null,
              reorderLevel:
                Number(
                  reorderLevel
                ),
              costPrice:
                nullableNumber(
                  costPrice
                ),
              sellingPrice:
                nullableNumber(
                  sellingPrice
                ),
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to add variant."
        );
      }

      setSetup(current => ({
        ...current,
        items:
          current.items.map(
            item =>
              item.id ===
              variantItem.id
                ? {
                    ...item,
                    variants: [
                      ...item.variants,
                      result,
                    ],
                  }
                : item
          ),
      }));

      setVariantItem(null);
      setFirstVariant("");
      setSku("");

      setNotice(
        "Variant created."
      );
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border bg-card p-5">
        <h2 className="font-semibold">
          Inventory Setup
        </h2>

        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <div>
            <Label>
              New Category
            </Label>

            <div className="flex gap-2">
              <input
                value={
                  categoryName
                }
                onChange={event =>
                  setCategoryName(
                    event.target
                      .value
                  )
                }
                placeholder="Textbooks"
                className={
                  inputClass
                }
              />

              <Button
                onClick={
                  createCategory
                }
                disabled={saving}
              >
                Add
              </Button>
            </div>
          </div>

          <div>
            <Label>
              New Stock Location
            </Label>

            <div className="flex gap-2">
              <input
                value={
                  locationName
                }
                onChange={event =>
                  setLocationName(
                    event.target
                      .value
                  )
                }
                placeholder="Main Store"
                className={
                  inputClass
                }
              />

              <Button
                onClick={
                  createLocation
                }
                disabled={saving}
              >
                <MapPin className="mr-2 h-4 w-4" />
                Add
              </Button>
            </div>
          </div>
        </div>
      </section>

      <section className="rounded-xl border bg-card">
        <SectionHeader
          icon={
            <PackagePlus className="h-5 w-5" />
          }
          title="Create Inventory Item"
          subtitle="Create textbooks, uniforms, books, supplies or any custom stock item."
        />

        <div className="grid gap-4 p-5 md:grid-cols-2 xl:grid-cols-4">
          <SelectInput
            label="Category"
            value={categoryId}
            onChange={
              setCategoryId
            }
            options={
              setup.categories
            }
          />

          <TextInput
            label="Item Name"
            value={itemName}
            onChange={
              setItemName
            }
            placeholder="School Shirt"
          />

          <TextInput
            label="Stock Unit"
            value={unit}
            onChange={setUnit}
            placeholder="Piece"
          />

          <div>
            <Label>
              Has Sizes /
              Variants?
            </Label>

            <label className="flex h-10 items-center gap-2 rounded-md border px-3">
              <input
                type="checkbox"
                checked={
                  trackVariants
                }
                onChange={event =>
                  setTrackVariants(
                    event.target
                      .checked
                  )
                }
              />

              Yes
            </label>
          </div>

          {trackVariants && (
            <TextInput
              label="First Variant"
              value={
                firstVariant
              }
              onChange={
                setFirstVariant
              }
              placeholder="Size 30"
            />
          )}

          <TextInput
            label="SKU"
            value={sku}
            onChange={setSku}
          />

          <TextInput
            label="Reorder Level"
            value={
              reorderLevel
            }
            onChange={
              setReorderLevel
            }
            type="number"
          />

          <TextInput
            label="Cost Price"
            value={costPrice}
            onChange={
              setCostPrice
            }
            type="number"
          />

          <TextInput
            label="Selling Price"
            value={
              sellingPrice
            }
            onChange={
              setSellingPrice
            }
            type="number"
          />

          <div className="flex items-end">
            <Button
              onClick={createItem}
              disabled={saving}
              className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
            >
              {saving && (
                <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
              )}

              Create Item
            </Button>
          </div>
        </div>
      </section>

      <section className="rounded-xl border bg-card">
        <SectionHeader
          icon={
            <Boxes className="h-5 w-5" />
          }
          title="Items & Variants"
          subtitle="Current inventory catalogue."
        />

        {setup.items.length ===
        0 ? (
          <Empty>
            No inventory items.
          </Empty>
        ) : (
          <div className="divide-y">
            {setup.items.map(
              item => (
                <div
                  key={item.id}
                  className="p-5"
                >
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <div className="font-semibold">
                        {item.name}
                      </div>

                      <div className="mt-1 text-xs text-muted-foreground">
                        {
                          item.categoryName
                        }
                        {" · "}
                        {item.unit}
                      </div>
                    </div>

                    {item.trackVariants && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          setVariantItem(
                            item
                          );
                          setFirstVariant(
                            ""
                          );
                          setSku("");
                        }}
                      >
                        <Plus className="mr-2 h-4 w-4" />
                        Add Variant
                      </Button>
                    )}
                  </div>

                  <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                    {item.variants.map(
                      variant => (
                        <div
                          key={
                            variant.id
                          }
                          className="rounded-lg border p-4"
                        >
                          <div className="font-medium">
                            {
                              variant.name
                            }
                          </div>

                          <div className="mt-2 text-2xl font-bold">
                            {format(
                              variant.totalQuantity
                            )}
                          </div>

                          <div className="text-xs text-muted-foreground">
                            {item.unit} in
                            stock
                          </div>

                          {variant.sku && (
                            <div className="mt-2 text-xs text-muted-foreground">
                              SKU:{" "}
                              {
                                variant.sku
                              }
                            </div>
                          )}

                          {variant.isLowStock && (
                            <div className="mt-3 flex items-center gap-1 text-xs font-medium text-amber-600">
                              <AlertTriangle className="h-4 w-4" />
                              Low stock
                            </div>
                          )}
                        </div>
                      )
                    )}
                  </div>
                </div>
              )
            )}
          </div>
        )}
      </section>

      {variantItem && (
        <section className="rounded-xl border bg-card p-5">
          <h3 className="font-semibold">
            Add Variant —{" "}
            {variantItem.name}
          </h3>

          <div className="mt-4 grid gap-4 md:grid-cols-3">
            <TextInput
              label="Variant / Size"
              value={
                firstVariant
              }
              onChange={
                setFirstVariant
              }
              placeholder="Size 32"
            />

            <TextInput
              label="SKU"
              value={sku}
              onChange={setSku}
            />

            <TextInput
              label="Reorder Level"
              value={
                reorderLevel
              }
              onChange={
                setReorderLevel
              }
              type="number"
            />
          </div>

          <div className="mt-4 flex gap-2">
            <Button
              onClick={addVariant}
              disabled={saving}
            >
              Add Variant
            </Button>

            <Button
              variant="outline"
              onClick={() =>
                setVariantItem(null)
              }
            >
              Cancel
            </Button>
          </div>
        </section>
      )}
    </div>
  );
}

function ListsPanel({
  setup,
  setSetup,
  saving,
  setSaving,
  setError,
  setNotice,
  clearMessages,
}: EditableProps) {
  const [name, setName] =
    useState("");

  const [
    listType,
    setListType,
  ] = useState(
    "Textbook List"
  );

  const [
    audienceType,
    setAudienceType,
  ] = useState("Class");

  const [
    audienceId,
    setAudienceId,
  ] = useState("");

  const [rows, setRows] =
    useState<
      {
        variantId: string;
        quantity: string;
      }[]
    >([]);

  const [demand, setDemand] =
    useState<InventoryDemand | null>(
      null
    );

  const variants =
    useMemo(
      () =>
        setup.items.flatMap(
          item =>
            item.variants.map(
              variant => ({
                id: variant.id,
                name:
                  variant.name ===
                  "Standard"
                    ? item.name
                    : `${item.name} — ${variant.name}`,
              })
            )
        ),
      [setup.items]
    );

  const audienceOptions =
    audienceType === "Class"
      ? setup.classes
      : audienceType ===
          "Level"
        ? setup.levels
        : [];

  async function createList() {
    clearMessages();

    if (!name.trim()) {
      setError(
        "List name is required."
      );
      return;
    }

    if (rows.length === 0) {
      setError(
        "Add at least one required item."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/inventory/lists",
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              name: name.trim(),
              listType,
              audienceType,
              audienceId:
                audienceType ===
                  "School" ||
                audienceType ===
                  "Staff"
                  ? null
                  : audienceId,
              academicSessionId:
                null,
              academicTermId:
                null,
              items: rows.map(
                row => ({
                  inventoryItemVariantId:
                    row.variantId,
                  quantityPerRecipient:
                    Number(
                      row.quantity
                    ),
                  isRequired: true,
                })
              ),
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to create inventory list."
        );
      }

      setSetup(current => ({
        ...current,
        lists: [
          ...current.lists,
          result,
        ],
      }));

      setName("");
      setRows([]);

      setNotice(
        "Inventory list created."
      );
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    } finally {
      setSaving(false);
    }
  }

  async function checkDemand(
    id: string
  ) {
    clearMessages();

    try {
      const response =
        await fetch(
          `/api/inventory/lists/${id}/demand`
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to calculate demand."
        );
      }

      setDemand(result);
    } catch (exception) {
      setError(
        getMessage(exception)
      );
    }
  }

  return (
    <div className="space-y-6">
      <section className="rounded-xl border bg-card">
        <SectionHeader
          icon={
            <ClipboardList className="h-5 w-5" />
          }
          title="Create Inventory List"
          subtitle="Define required textbooks, books, uniforms or supplies for a group."
        />

        <div className="space-y-5 p-5">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            <TextInput
              label="List Name"
              value={name}
              onChange={setName}
              placeholder="Primary 3 Textbook List"
            />

            <TextInput
              label="List Type"
              value={listType}
              onChange={
                setListType
              }
            />

            <div>
              <Label>
                Audience
              </Label>

              <select
                value={
                  audienceType
                }
                onChange={event => {
                  setAudienceType(
                    event.target
                      .value
                  );
                  setAudienceId("");
                }}
                className={inputClass}
              >
                <option value="Class">
                  Class
                </option>

                <option value="Level">
                  Level
                </option>

                <option value="School">
                  Entire School
                </option>

                <option value="Staff">
                  All Staff
                </option>
              </select>
            </div>

            {audienceOptions.length >
              0 && (
              <SelectInput
                label={
                  audienceType
                }
                value={audienceId}
                onChange={
                  setAudienceId
                }
                options={
                  audienceOptions
                }
              />
            )}
          </div>

          <div>
            <div className="mb-3 flex items-center justify-between">
              <Label>
                Required Items
              </Label>

              <Button
                size="sm"
                variant="outline"
                disabled={
                  variants.length ===
                  0
                }
                onClick={() =>
                  setRows(
                    current => [
                      ...current,
                      {
                        variantId:
                          variants[0]
                            ?.id ??
                          "",
                        quantity:
                          "1",
                      },
                    ]
                  )
                }
              >
                <Plus className="mr-2 h-4 w-4" />
                Add Item
              </Button>
            </div>

            <div className="space-y-2">
              {rows.map(
                (row, index) => (
                  <div
                    key={index}
                    className="grid gap-2 md:grid-cols-[1fr_160px_auto]"
                  >
                    <select
                      value={
                        row.variantId
                      }
                      onChange={event =>
                        setRows(
                          current =>
                            current.map(
                              (
                                item,
                                i
                              ) =>
                                i ===
                                index
                                  ? {
                                      ...item,
                                      variantId:
                                        event
                                          .target
                                          .value,
                                    }
                                  : item
                            )
                        )
                      }
                      className={
                        inputClass
                      }
                    >
                      {variants.map(
                        variant => (
                          <option
                            key={
                              variant.id
                            }
                            value={
                              variant.id
                            }
                          >
                            {
                              variant.name
                            }
                          </option>
                        )
                      )}
                    </select>

                    <input
                      type="number"
                      min="0.001"
                      step="0.001"
                      value={
                        row.quantity
                      }
                      onChange={event =>
                        setRows(
                          current =>
                            current.map(
                              (
                                item,
                                i
                              ) =>
                                i ===
                                index
                                  ? {
                                      ...item,
                                      quantity:
                                        event
                                          .target
                                          .value,
                                    }
                                  : item
                            )
                        )
                      }
                      className={
                        inputClass
                      }
                    />

                    <Button
                      variant="ghost"
                      onClick={() =>
                        setRows(
                          current =>
                            current.filter(
                              (
                                _,
                                i
                              ) =>
                                i !==
                                index
                            )
                        )
                      }
                    >
                      Remove
                    </Button>
                  </div>
                )
              )}
            </div>
          </div>

          <Button
            onClick={createList}
            disabled={saving}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {saving && (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            )}

            Create List
          </Button>
        </div>
      </section>

      <section className="rounded-xl border bg-card">
        <SectionHeader
          icon={
            <ClipboardList className="h-5 w-5" />
          }
          title="Existing Lists"
          subtitle="Check stock requirement and shortages."
        />

        {setup.lists.length ===
        0 ? (
          <Empty>
            No inventory lists.
          </Empty>
        ) : (
          <div className="divide-y">
            {setup.lists.map(
              list => (
                <div
                  key={list.id}
                  className="flex flex-wrap items-center justify-between gap-4 p-5"
                >
                  <div>
                    <div className="font-medium">
                      {list.name}
                    </div>

                    <div className="mt-1 text-xs text-muted-foreground">
                      {list.listType}
                      {" · "}
                      {list.audienceName ??
                        list.audienceType}
                      {" · "}
                      {list.items.length}{" "}
                      items
                    </div>
                  </div>

                  <Button
                    variant="outline"
                    onClick={() =>
                      checkDemand(
                        list.id
                      )
                    }
                  >
                    Check Demand
                  </Button>
                </div>
              )
            )}
          </div>
        )}
      </section>

      {demand && (
        <section className="overflow-hidden rounded-xl border bg-card">
          <SectionHeader
            icon={
              <AlertTriangle className="h-5 w-5" />
            }
            title={
              demand.listName
            }
            subtitle={`${demand.recipientCount} recipients`}
          />

          <div className="overflow-x-auto">
            <table className="w-full min-w-[850px] text-sm">
              <thead className="border-b bg-muted/30 text-left">
                <tr>
                  <th className="px-4 py-3">
                    Item
                  </th>
                  <th className="px-4 py-3">
                    Required
                  </th>
                  <th className="px-4 py-3">
                    Issued
                  </th>
                  <th className="px-4 py-3">
                    Remaining
                  </th>
                  <th className="px-4 py-3">
                    Available
                  </th>
                  <th className="px-4 py-3">
                    Shortfall
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y">
                {demand.items.map(
                  item => (
                    <tr
                      key={
                        item.inventoryListItemId
                      }
                    >
                      <td className="px-4 py-3 font-medium">
                        {item.itemName}
                        {item.variantName !==
                          "Standard" &&
                          ` — ${item.variantName}`}
                      </td>

                      <td className="px-4 py-3">
                        {format(
                          item.requiredQuantity
                        )}
                      </td>

                      <td className="px-4 py-3">
                        {format(
                          item.issuedQuantity
                        )}
                      </td>

                      <td className="px-4 py-3">
                        {format(
                          item.remainingRequirement
                        )}
                      </td>

                      <td className="px-4 py-3">
                        {format(
                          item.availableQuantity
                        )}
                      </td>

                      <td
                        className={
                          item.shortfall >
                          0
                            ? "px-4 py-3 font-semibold text-red-600"
                            : "px-4 py-3 font-semibold text-green-600"
                        }
                      >
                        {format(
                          item.shortfall
                        )}
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
}

function MovementPanel({
  setup,
  saving,
  setSaving,
  setError,
  setNotice,
  clearMessages,
}: CommonProps) {
  const variants =
    setup.items.flatMap(
      item =>
        item.variants.map(
          variant => ({
            id: variant.id,
            name:
              variant.name ===
              "Standard"
                ? item.name
                : `${item.name} — ${variant.name}`,
          })
        )
    );

  const [
    variantId,
    setVariantId,
  ] = useState(
    variants[0]?.id ?? ""
  );

  const [type, setType] =
    useState("StockIn");

  const [
    quantity,
    setQuantity,
  ] = useState("1");

  const [
    fromLocationId,
    setFromLocationId,
  ] = useState(
    setup.locations[0]?.id ??
      ""
  );

  const [
    toLocationId,
    setToLocationId,
  ] = useState(
    setup.locations[0]?.id ??
      ""
  );

  const [
    recipientType,
    setRecipientType,
  ] = useState("Student");

  const [
    recipientId,
    setRecipientId,
  ] = useState("");

  const [
    inventoryListId,
    setInventoryListId,
  ] = useState("");

  const [notes, setNotes] =
    useState("");

  const needsFrom = [
    "Issue",
    "Sale",
    "Transfer",
    "Consumed",
    "Damaged",
    "Lost",
    "AdjustmentOut",
  ].includes(type);

  const needsTo = [
    "StockIn",
    "Return",
    "Transfer",
    "AdjustmentIn",
  ].includes(type);

  const needsRecipient =
    type === "Issue" ||
    type === "Sale";

  const recipients =
    recipientType === "Student"
      ? setup.students
      : setup.staff;

  const selectedList =
    setup.lists.find(
      list =>
        list.id ===
        inventoryListId
    );

  const selectedListItem =
    selectedList?.items.find(
      item =>
        item.inventoryItemVariantId ===
        variantId
    );

  async function saveMovement() {
    clearMessages();

    if (!variantId) {
      setError(
        "Select an inventory item."
      );
      return;
    }

    if (
      needsRecipient &&
      !recipientId
    ) {
      setError(
        "Select a recipient."
      );
      return;
    }

    if (
      inventoryListId &&
      !selectedListItem
    ) {
      setError(
        "The selected item is not part of that inventory list."
      );
      return;
    }

    setSaving(true);

    try {
      const response =
        await fetch(
          "/api/inventory/movements",
          {
            method: "POST",
            headers: jsonHeaders,
            body: JSON.stringify({
              inventoryItemVariantId:
                variantId,
              type,
              quantity:
                Number(quantity),
              fromLocationId:
                needsFrom
                  ? fromLocationId
                  : null,
              toLocationId:
                needsTo
                  ? toLocationId
                  : null,
              recipientType:
                needsRecipient
                  ? recipientType
                  : null,
              recipientId:
                needsRecipient
                  ? recipientId
                  : null,
              recipientName:
                null,
              unitPrice: null,
              inventoryListId:
                inventoryListId ||
                null,
              inventoryListItemId:
                selectedListItem?.id ??
                null,
              notes:
                notes.trim() ||
                null,
            }),
          }
        );

      const result =
        await response.json();

      if (!response.ok) {
        throw new Error(
          result.error ??
            "Unable to save stock movement."
        );
      }

      setNotice(
        "Stock movement recorded."
      );

      window.location.reload();
    } catch (exception) {
      setError(
        getMessage(exception)
      );
      setSaving(false);
    }
  }

  return (
    <section className="rounded-xl border bg-card">
      <SectionHeader
        icon={
          <Truck className="h-5 w-5" />
        }
        title="Stock Movement"
        subtitle="Receive, issue, sell, transfer, return or adjust stock."
      />

      <div className="grid gap-4 p-5 md:grid-cols-2 xl:grid-cols-3">
        <div>
          <Label>
            Inventory Item
          </Label>

          <select
            value={variantId}
            onChange={event =>
              setVariantId(
                event.target.value
              )
            }
            className={inputClass}
          >
            {variants.map(
              variant => (
                <option
                  key={variant.id}
                  value={variant.id}
                >
                  {variant.name}
                </option>
              )
            )}
          </select>
        </div>

        <div>
          <Label>
            Movement Type
          </Label>

          <select
            value={type}
            onChange={event =>
              setType(
                event.target.value
              )
            }
            className={inputClass}
          >
            {MOVEMENT_TYPES.map(
              value => (
                <option
                  key={value}
                  value={value}
                >
                  {humanize(
                    value
                  )}
                </option>
              )
            )}
          </select>
        </div>

        <TextInput
          label="Quantity"
          value={quantity}
          onChange={setQuantity}
          type="number"
        />

        {needsFrom && (
          <SelectInput
            label="From Location"
            value={
              fromLocationId
            }
            onChange={
              setFromLocationId
            }
            options={
              setup.locations
            }
          />
        )}

        {needsTo && (
          <SelectInput
            label="To Location"
            value={
              toLocationId
            }
            onChange={
              setToLocationId
            }
            options={
              setup.locations
            }
          />
        )}

        {needsRecipient && (
          <>
            <div>
              <Label>
                Recipient Type
              </Label>

              <select
                value={
                  recipientType
                }
                onChange={event => {
                  setRecipientType(
                    event.target
                      .value
                  );
                  setRecipientId("");
                }}
                className={inputClass}
              >
                <option value="Student">
                  Student
                </option>

                <option value="Staff">
                  Staff
                </option>
              </select>
            </div>

            <div>
              <Label>
                Recipient
              </Label>

              <select
                value={recipientId}
                onChange={event =>
                  setRecipientId(
                    event.target
                      .value
                  )
                }
                className={inputClass}
              >
                <option value="">
                  Select recipient
                </option>

                {recipients.map(
                  recipient => (
                    <option
                      key={
                        recipient.id
                      }
                      value={
                        recipient.id
                      }
                    >
                      {recipient.name}
                      {" — "}
                      {
                        recipient.reference
                      }
                    </option>
                  )
                )}
              </select>
            </div>

            <div>
              <Label>
                Requirement List
                (optional)
              </Label>

              <select
                value={
                  inventoryListId
                }
                onChange={event =>
                  setInventoryListId(
                    event.target
                      .value
                  )
                }
                className={inputClass}
              >
                <option value="">
                  None
                </option>

                {setup.lists.map(
                  list => (
                    <option
                      key={list.id}
                      value={list.id}
                    >
                      {list.name}
                    </option>
                  )
                )}
              </select>
            </div>
          </>
        )}

        <div className="md:col-span-2 xl:col-span-3">
          <TextInput
            label="Notes"
            value={notes}
            onChange={setNotes}
          />
        </div>

        <div>
          <Button
            onClick={
              saveMovement
            }
            disabled={saving}
            className="bg-tenant-primary text-black hover:bg-tenant-primary/90"
          >
            {saving && (
              <LoaderCircle className="mr-2 h-4 w-4 animate-spin" />
            )}

            Save Movement
          </Button>
        </div>
      </div>
    </section>
  );
}

function HistoryPanel({
  setup,
}: {
  setup: InventorySetup;
}) {
  return (
    <section className="overflow-hidden rounded-xl border bg-card">
      <SectionHeader
        icon={
          <History className="h-5 w-5" />
        }
        title="Inventory History"
        subtitle="Recent stock movements and issuance."
      />

      {setup.recentTransactions
        .length === 0 ? (
        <Empty>
          No inventory activity yet.
        </Empty>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[1000px] text-sm">
            <thead className="border-b bg-muted/30 text-left">
              <tr>
                <th className="px-4 py-3">
                  Date
                </th>
                <th className="px-4 py-3">
                  Item
                </th>
                <th className="px-4 py-3">
                  Type
                </th>
                <th className="px-4 py-3">
                  Quantity
                </th>
                <th className="px-4 py-3">
                  From
                </th>
                <th className="px-4 py-3">
                  To
                </th>
                <th className="px-4 py-3">
                  Recipient
                </th>
                <th className="px-4 py-3">
                  Notes
                </th>
              </tr>
            </thead>

            <tbody className="divide-y">
              {setup.recentTransactions.map(
                transaction => (
                  <tr
                    key={
                      transaction.id
                    }
                  >
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Intl.DateTimeFormat(
                        "en-GB",
                        {
                          day: "2-digit",
                          month: "short",
                          year: "numeric",
                        }
                      ).format(
                        new Date(
                          transaction.createdAtUtc
                        )
                      )}
                    </td>

                    <td className="px-4 py-3 font-medium">
                      {
                        transaction.itemName
                      }

                      {transaction.variantName !==
                        "Standard" &&
                        ` — ${transaction.variantName}`}
                    </td>

                    <td className="px-4 py-3">
                      {humanize(
                        transaction.type
                      )}
                    </td>

                    <td className="px-4 py-3">
                      {format(
                        transaction.quantity
                      )}
                    </td>

                    <td className="px-4 py-3">
                      {transaction.fromLocationName ??
                        "—"}
                    </td>

                    <td className="px-4 py-3">
                      {transaction.toLocationName ??
                        "—"}
                    </td>

                    <td className="px-4 py-3">
                      {transaction.recipientName ??
                        "—"}
                    </td>

                    <td className="px-4 py-3 text-muted-foreground">
                      {transaction.notes ??
                        "—"}
                    </td>
                  </tr>
                )
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

const jsonHeaders = {
  "Content-Type":
    "application/json",
};

const inputClass =
  "h-10 w-full rounded-md border bg-background px-3 text-sm";

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={
        active
          ? "rounded-lg bg-tenant-primary px-4 py-2 text-sm font-medium text-black"
          : "rounded-lg px-4 py-2 text-sm text-muted-foreground hover:bg-muted"
      }
    >
      {children}
    </button>
  );
}

function SectionHeader({
  icon,
  title,
  subtitle,
}: {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
}) {
  return (
    <div className="flex items-start gap-3 border-b p-5">
      {icon}

      <div>
        <h2 className="font-semibold">
          {title}
        </h2>

        <p className="mt-1 text-xs text-muted-foreground">
          {subtitle}
        </p>
      </div>
    </div>
  );
}

function Metric({
  label,
  value,
}: {
  label: string;
  value: string | number;
}) {
  return (
    <div className="rounded-xl border bg-card p-5">
      <div className="text-xs text-muted-foreground">
        {label}
      </div>

      <div className="mt-2 text-2xl font-bold">
        {value}
      </div>
    </div>
  );
}

function Label({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <label className="mb-2 block text-sm font-medium">
      {children}
    </label>
  );
}

function TextInput({
  label,
  value,
  onChange,
  type = "text",
  placeholder,
}: {
  label: string;
  value: string;
  onChange: (
    value: string
  ) => void;
  type?: string;
  placeholder?: string;
}) {
  return (
    <div>
      <Label>{label}</Label>

      <input
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={event =>
          onChange(
            event.target.value
          )
        }
        className={inputClass}
      />
    </div>
  );
}

function SelectInput({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (
    value: string
  ) => void;
  options: {
    id: string;
    name: string;
  }[];
}) {
  return (
    <div>
      <Label>{label}</Label>

      <select
        value={value}
        onChange={event =>
          onChange(
            event.target.value
          )
        }
        className={inputClass}
      >
        <option value="">
          Select
        </option>

        {options.map(
          option => (
            <option
              key={option.id}
              value={option.id}
            >
              {option.name}
            </option>
          )
        )}
      </select>
    </div>
  );
}

function Empty({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="p-10 text-center text-sm text-muted-foreground">
      {children}
    </div>
  );
}

function nullableNumber(
  value: string
) {
  return value.trim() === ""
    ? null
    : Number(value);
}

function format(
  value: number
) {
  return new Intl.NumberFormat(
    "en-GB",
    {
      maximumFractionDigits:
        3,
    }
  ).format(value);
}

function humanize(
  value: string
) {
  return value.replace(
    /([a-z])([A-Z])/g,
    "$1 $2"
  );
}

function getMessage(
  exception: unknown
) {
  return exception instanceof Error
    ? exception.message
    : "Something went wrong.";
}
