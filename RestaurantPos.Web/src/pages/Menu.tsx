import { useEffect, useMemo, useState } from 'react'
import {
  createMenuCategory,
  createMenuItem,
  deleteMenuCategory,
  deleteMenuItem,
  fetchMenuCategories,
  fetchMenuItems,
  updateMenuItem,
  uploadMenuImage
} from '../api/menu'
import type { MenuItem } from '../types/api'
import type { MenuCategory, MenuItemCreateRequest } from '../types/menu'
import { API_BASE_URL } from '../api/config'

const emptyForm: MenuItemCreateRequest = {
  name: '',
  description: '',
  imageUrl: '',
  price: 0,
  isActive: true,
  categoryId: null
}

export default function Menu() {
  const [items, setItems] = useState<MenuItem[]>([])
  const [categories, setCategories] = useState<MenuCategory[]>([])
  const [form, setForm] = useState<MenuItemCreateRequest>(emptyForm)
  const [categoryName, setCategoryName] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [undoItem, setUndoItem] = useState<MenuItem | null>(null)
  const [undoCategory, setUndoCategory] = useState<MenuCategory | null>(null)

  useEffect(() => {
    let active = true

    const load = async () => {
      try {
        const [menuData, categoryData] = await Promise.all([
          fetchMenuItems(),
          fetchMenuCategories()
        ])
        if (!active) return
        setItems(menuData)
        setCategories(categoryData)
      } catch {
        if (active) setError('Unable to load menu items.')
      } finally {
        if (active) setIsLoading(false)
      }
    }

    load()
    return () => {
      active = false
    }
  }, [])

  const handleSave = async () => {
    if (!form.name.trim()) {
      setError('Menu item name is required.')
      return
    }

    setIsSaving(true)
    try {
      const created = await createMenuItem({
        ...form,
        name: form.name.trim(),
        description: form.description?.trim() || null,
        imageUrl: form.imageUrl?.trim() || null,
        categoryId: form.categoryId ? Number(form.categoryId) : null
      })
      setItems((prev) => [created, ...prev])
      setForm(emptyForm)
      setError(null)
    } catch {
      setError('Unable to save menu item.')
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async (item: MenuItem) => {
    if (!window.confirm('Delete menu item "' + item.name + '"?')) return
    await deleteMenuItem(item.id)
    setItems((prev) => prev.filter((menu) => menu.id !== item.id))
    setUndoItem(item)
    setTimeout(() => setUndoItem((prev) => (prev?.id === item.id ? null : prev)), 8000)
  }

  const handleUndoItem = async () => {
    if (!undoItem) return
    try {
      const restored = await createMenuItem({
        name: undoItem.name,
        description: undoItem.description ?? null,
        imageUrl: undoItem.imageUrl ?? null,
        price: undoItem.price,
        isActive: undoItem.isActive,
        categoryId: undoItem.categoryId ?? null
      })
      setItems((prev) => [restored, ...prev])
      setUndoItem(null)
    } catch {
      setError('Unable to undo delete.')
    }
  }

  const handleUpdate = async (item: MenuItem) => {
    await updateMenuItem(item.id, {
      name: item.name,
      description: item.description ?? null,
      imageUrl: item.imageUrl ?? null,
      price: item.price,
      isActive: item.isActive,
      categoryId: item.categoryId ?? null
    })
  }

  const handleAddCategory = async () => {
    if (!categoryName.trim()) return
    const created = await createMenuCategory(categoryName.trim())
    setCategories((prev) => [...prev, created])
    setCategoryName('')
  }

  const handleDeleteCategory = async (category: MenuCategory) => {
    if (!window.confirm('Delete category "' + category.name + '"?')) return
    await deleteMenuCategory(category.id)
    setCategories((prev) => prev.filter((item) => item.id !== category.id))
    setUndoCategory(category)
    setTimeout(() => setUndoCategory((prev) => (prev?.id === category.id ? null : prev)), 8000)
  }

  const handleUndoCategory = async () => {
    if (!undoCategory) return
    try {
      const restored = await createMenuCategory(undoCategory.name)
      setCategories((prev) => [...prev, restored])
      setUndoCategory(null)
    } catch {
      setError('Unable to undo delete.')
    }
  }

  const onUpload = async (itemId: number, file: File) => {
    const updated = await uploadMenuImage(itemId, file)
    setItems((prev) => prev.map((item) => (item.id === itemId ? updated : item)))
  }

  const categoryNameFor = (id?: number | null) => {
    if (!id) return 'Unassigned'
    return categories.find((category) => category.id === id)?.name ?? 'Unassigned'
  }

  const emptyState = useMemo(() => (!isLoading && items.length === 0 ? true : false), [isLoading, items])

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Menu Studio</h3>
          <p className="text-sm text-stone-500 dark:text-stone-400">Create dishes and update images.</p>
        </div>
        <button
          className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)]"
          onClick={handleSave}
          disabled={isSaving}
          data-loading={isSaving}
        >
          {isSaving ? 'Saving...' : 'Save Menu Item'}
        </button>
      </div>

      {error && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-3xl border border-rose-200 bg-rose-50 px-6 py-4 text-sm text-rose-600">
          <span>{error}</span>
          <button
            className="rounded-full border border-rose-200 px-3 py-1 text-xs uppercase tracking-[0.2em] text-rose-700"
            onClick={() => window.location.reload()}
          >
            Retry
          </button>
        </div>
      )}

      {undoItem && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-3xl border border-amber-200 bg-amber-50 px-6 py-4 text-sm text-amber-700">
          <span>Deleted "{undoItem.name}".</span>
          <button
            className="btn btn-ghost btn-pill btn-xs"
            onClick={handleUndoItem}
          >
            Undo
          </button>
        </div>
      )}

      {undoCategory && (
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-3xl border border-amber-200 bg-amber-50 px-6 py-4 text-sm text-amber-700">
          <span>Deleted category "{undoCategory.name}".</span>
          <button
            className="btn btn-ghost btn-pill btn-xs"
            onClick={handleUndoCategory}
          >
            Undo
          </button>
        </div>
      )}

      <section className="grid gap-6 lg:grid-cols-[1.1fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-[var(--ink)]">New Menu Item</h4>
          <div className="mt-4 grid gap-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Name</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none"
                value={form.name}
                onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
                placeholder="Cedar Salmon"
              />
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Description</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none"
                value={form.description ?? ''}
                onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
                placeholder="Seasonal plating"
              />
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Price</label>
              <input
                type="number"
                min={0}
                step="0.01"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={form.price}
                onChange={(event) => setForm((prev) => ({ ...prev, price: Number(event.target.value) }))}
              />
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Category</label>
              <select
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={form.categoryId ?? ''}
                onChange={(event) =>
                  setForm((prev) => ({
                    ...prev,
                    categoryId: event.target.value ? Number(event.target.value) : null
                  }))
                }
              >
                <option value="">Unassigned</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(event) => setForm((prev) => ({ ...prev, isActive: event.target.checked }))}
              />
              <span className="text-sm text-[var(--muted)]">Active</span>
            </div>
          </div>
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-[var(--ink)]">Categories</h4>
          <div className="mt-4 space-y-4">
            <div className="flex gap-2">
              <input
                className="flex-1 rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] placeholder:text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none"
                value={categoryName}
                onChange={(event) => setCategoryName(event.target.value)}
                placeholder="Mains"
              />
              <button
                className="btn btn-primary btn-soft btn-sm"
                onClick={handleAddCategory}
              >
                Add
              </button>
            </div>
            <div className="space-y-3">
              {categories.map((category) => (
                <div key={category.id} className="flex items-center justify-between rounded-2xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3">
                  <span className="text-sm font-semibold text-[var(--ink)]">{category.name}</span>
                  <button
                    className="rounded-full border border-[var(--danger-border)] px-3 py-1 text-xs uppercase tracking-[0.2em] text-[var(--danger)]"
                    onClick={() => handleDeleteCategory(category)}
                  >
                    Delete
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {emptyState && (
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--surface)] px-6 py-4 text-sm text-[var(--muted)]">
          No menu items yet. Create your first dish.
        </div>
      )}

      <section className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        {items.map((item) => (
          <MenuCard key={item.id} item={item} onUpdate={handleUpdate} onDelete={handleDelete} onUpload={onUpload} category={categoryNameFor(item.categoryId)} />
        ))}
      </section>
    </div>
  )
}

function MenuCard({
  item,
  category,
  onUpdate,
  onDelete,
  onUpload
}: {
  item: MenuItem
  category: string
  onUpdate: (item: MenuItem) => void
  onDelete: (item: MenuItem) => Promise<void> | void
  onUpload: (itemId: number, file: File) => Promise<void> | void
}) {
  const [isEditing, setIsEditing] = useState(false)
  const [formState, setFormState] = useState<MenuItemCreateRequest>({
    name: item.name,
    description: item.description ?? '',
    imageUrl: item.imageUrl ?? '',
    price: item.price,
    isActive: item.isActive,
    categoryId: item.categoryId ?? null
  })
  const [isUploading, setIsUploading] = useState(false)

  const imageUrl = item.imageUrl?.startsWith('http')
    ? item.imageUrl
    : item.imageUrl
      ? `${API_BASE_URL}/${item.imageUrl}`
      : ''

  return (
    <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-5 shadow-sm">
      <div className="flex flex-col gap-4">
        <div className="flex items-start justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.2em] text-[var(--accent)]/80">{category}</p>
            <h4 className="text-lg font-semibold text-[var(--ink)]">{item.name}</h4>
            <p className="text-sm text-[var(--muted)]">{item.description}</p>
          </div>
          <div className="flex items-center gap-2">
            <span
              className={`rounded-full px-3 py-1 text-xs ${
                item.isActive ? 'bg-[var(--success-bg)] text-[var(--success)]' : 'bg-[var(--surface)] text-[var(--muted)]'
              }`}
            >
              {item.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        </div>

        {imageUrl ? (
          <img src={imageUrl} className="h-40 w-full rounded-2xl object-cover" alt={item.name} />
        ) : (
          <div className="flex h-40 items-center justify-center rounded-2xl bg-[var(--surface)] text-xs text-[var(--muted)]">
            No image
          </div>
        )}

        {isEditing ? (
          <div className="grid gap-3">
            <input
              className="rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
              value={formState.name}
              onChange={(event) => setFormState((prev) => ({ ...prev, name: event.target.value }))}
            />
            <input
              className="rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
              value={formState.description ?? ''}
              onChange={(event) => setFormState((prev) => ({ ...prev, description: event.target.value }))}
            />
            <input
              type="number"
              step="0.01"
              className="rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
              value={formState.price}
              onChange={(event) => setFormState((prev) => ({ ...prev, price: Number(event.target.value) }))}
            />
            <label className="flex items-center gap-2 text-xs text-[var(--muted)]">
              <input
                type="checkbox"
                checked={formState.isActive}
                onChange={(event) => setFormState((prev) => ({ ...prev, isActive: event.target.checked }))}
              />
              Active
            </label>
          </div>
        ) : (
          <div className="flex items-center justify-between">
            <p className="text-sm font-semibold text-[var(--ink)]">${item.price.toFixed(2)}</p>
            <span className="text-xs text-[var(--muted)]">{item.isActive ? 'Visible' : 'Hidden'}</span>
          </div>
        )}

        <div className="flex flex-wrap gap-2">
          {isEditing ? (
            <button
              className="flex-1 rounded-full border border-[var(--border)] px-4 py-2 text-xs uppercase tracking-[0.2em] text-[var(--accent)]"
              onClick={() => {
                onUpdate({
                  ...item,
                  ...formState
                })
                setIsEditing(false)
              }}
            >
              Save
            </button>
          ) : (
            <button
              className="flex-1 rounded-full border border-[var(--border)] px-4 py-2 text-xs uppercase tracking-[0.2em] text-[var(--accent)]"
              onClick={() => setIsEditing(true)}
            >
              Edit
            </button>
          )}
          <button
            className="flex-1 rounded-full border border-[var(--danger-border)] px-4 py-2 text-xs uppercase tracking-[0.2em] text-[var(--danger)]"
            onClick={() => onDelete(item)}
          >
            Delete
          </button>
        </div>

        <div className="mt-2">
          <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Upload Image</label>
          <input
            type="file"
            accept="image/*"
            className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-2 text-sm text-[var(--muted)] focus:border-[var(--accent)] focus:outline-none"
            onChange={(event) => {
              const file = event.target.files?.[0]
              if (!file) return
              setIsUploading(true)
              Promise.resolve(onUpload(item.id, file))
                .finally(() => setIsUploading(false))
            }}
          />
          {isUploading && (
            <p className="mt-2 text-xs text-[var(--muted)]">Uploading...</p>
          )}
        </div>
      </div>
    </div>
  )
}

