import { useEffect, useMemo, useState } from 'react'
import {
  adjustStock,
  createIngredient,
  deleteIngredient,
  fetchIngredients,
  fetchLowStock,
  updateIngredient
} from '../api/inventory'
import type { Ingredient } from '../types/inventory'
import type { IngredientCreateRequest } from '../api/inventory'

const emptyIngredient: IngredientCreateRequest = {
  name: '',
  unit: 'each',
  stockQuantity: 0,
  reorderLevel: 0,
  costPerUnit: 0,
  isActive: true
}

export default function Inventory() {
  const [items, setItems] = useState<Ingredient[]>([])
  const [lowStock, setLowStock] = useState<Ingredient[]>([])
  const [ingredientId, setIngredientId] = useState<number | ''>('')
  const [quantityChange, setQuantityChange] = useState(0)
  const [reason, setReason] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [newIngredient, setNewIngredient] = useState<IngredientCreateRequest>(emptyIngredient)
  const [editId, setEditId] = useState<number | null>(null)
  const [editForm, setEditForm] = useState<IngredientCreateRequest>(emptyIngredient)

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const [ingredientData, lowStockData] = await Promise.all([
          fetchIngredients(),
          fetchLowStock()
        ])
        if (!active) return
        setItems(ingredientData)
        setLowStock(lowStockData)
      } catch {
        if (active) setError('Unable to load inventory.')
      } finally {
        if (active) setIsLoading(false)
      }
    }

    load()
    return () => {
      active = false
    }
  }, [])

  const selectedIngredient = useMemo(
    () => items.find((item) => item.id === ingredientId),
    [items, ingredientId]
  )

  const handleAdjust = async () => {
    if (!ingredientId || !reason.trim()) {
      setError('Select ingredient and reason.')
      return
    }

    if (quantityChange === 0) {
      setError('Quantity change must not be zero.')
      return
    }

    setIsSubmitting(true)
    try {
      await adjustStock({
        ingredientId: Number(ingredientId),
        quantityChange,
        reason: reason.trim()
      })

      setItems((prev) =>
        prev.map((item) =>
          item.id === ingredientId
            ? { ...item, stockQuantity: item.stockQuantity + quantityChange }
            : item
        )
      )

      setLowStock((prev) =>
        prev
          .map((item) =>
            item.id === ingredientId
              ? { ...item, stockQuantity: item.stockQuantity + quantityChange }
              : item
          )
          .filter((item) => item.stockQuantity <= item.reorderLevel)
      )

      setQuantityChange(0)
      setReason('')
      setError(null)
    } catch {
      setError('Unable to adjust stock.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleCreateIngredient = async () => {
    if (!newIngredient.name.trim()) {
      setError('Ingredient name is required.')
      return
    }

    setIsSubmitting(true)
    try {
      const created = await createIngredient({
        ...newIngredient,
        name: newIngredient.name.trim(),
        unit: newIngredient.unit.trim()
      })
      setItems((prev) => [created, ...prev])
      if (created.stockQuantity <= created.reorderLevel) {
        setLowStock((prev) => [created, ...prev])
      }
      setNewIngredient(emptyIngredient)
      setError(null)
    } catch {
      setError('Unable to create ingredient.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const startEdit = (item: Ingredient) => {
    setEditId(item.id)
    setEditForm({
      name: item.name,
      unit: item.unit,
      stockQuantity: item.stockQuantity,
      reorderLevel: item.reorderLevel,
      costPerUnit: item.costPerUnit,
      isActive: item.isActive
    })
  }

  const handleUpdate = async () => {
    if (!editId) return
    setIsSubmitting(true)
    try {
      await updateIngredient(editId, {
        ...editForm,
        name: editForm.name.trim(),
        unit: editForm.unit.trim()
      })
      setItems((prev) =>
        prev.map((item) =>
          item.id === editId
            ? {
                ...item,
                name: editForm.name.trim(),
                unit: editForm.unit.trim(),
                stockQuantity: editForm.stockQuantity,
                reorderLevel: editForm.reorderLevel,
                costPerUnit: editForm.costPerUnit,
                isActive: editForm.isActive
              }
            : item
        )
      )
      setLowStock((prev) =>
        prev
          .map((item) =>
            item.id === editId
              ? {
                  ...item,
                  name: editForm.name.trim(),
                  unit: editForm.unit.trim(),
                  stockQuantity: editForm.stockQuantity,
                  reorderLevel: editForm.reorderLevel,
                  costPerUnit: editForm.costPerUnit,
                  isActive: editForm.isActive
                }
              : item
          )
          .filter((item) => item.stockQuantity <= item.reorderLevel)
      )
      setEditId(null)
      setError(null)
    } catch {
      setError('Unable to update ingredient.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleApplySampleCosts = async () => {
    setIsSubmitting(true)
    try {
      const updated = await Promise.all(
        items.map(async (item) => {
          const costPerUnit = item.costPerUnit > 0 ? item.costPerUnit : Number((Math.random() * 8 + 1).toFixed(2))
          await updateIngredient(item.id, {
            name: item.name,
            unit: item.unit,
            stockQuantity: item.stockQuantity,
            reorderLevel: item.reorderLevel,
            costPerUnit,
            isActive: item.isActive
          })
          return { ...item, costPerUnit }
        })
      )
      setItems(updated)
      setLowStock((prev) => prev.map((item) => updated.find((u) => u.id === item.id) ?? item))
      setError(null)
    } catch {
      setError('Unable to apply sample costs.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleDelete = async (item: Ingredient) => {
    if (!window.confirm('Delete ingredient "' + item.name + '"?')) return
    await deleteIngredient(item.id)
    setItems((prev) => prev.filter((entry) => entry.id !== item.id))
    setLowStock((prev) => prev.filter((entry) => entry.id !== item.id))
  }

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900">Inventory</h3>
          <p className="text-sm text-stone-500">Track stock, see alerts, and adjust usage.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button
            className="btn btn-ghost btn-pill btn-sm"
            onClick={handleApplySampleCosts}
            disabled={isSubmitting}
            data-loading={isSubmitting}
          >
            Set Sample Costs
          </button>
          <button
            className="btn btn-primary btn-pill btn-md"
            onClick={handleAdjust}
            disabled={isSubmitting}
            data-loading={isSubmitting}
          >
            {isSubmitting ? 'Saving...' : 'Apply Adjustment'}
          </button>
        </div>
      </div>

      {error && (
        <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-4 text-sm text-rose-600">
          {error}
        </div>
      )}

      <section className="grid gap-6 lg:grid-cols-[1fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">Adjust Stock</h4>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Ingredient</label>
              <select
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={ingredientId}
                onChange={(event) => setIngredientId(event.target.value ? Number(event.target.value) : '')}
              >
                <option value="">Select ingredient</option>
                {items.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} ({item.stockQuantity.toFixed(2)} {item.unit})
                  </option>
                ))}
              </select>
              {selectedIngredient && (
                <p className="mt-2 text-xs text-stone-500">
                  Current: {selectedIngredient.stockQuantity.toFixed(2)} {selectedIngredient.unit}
                </p>
              )}
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Quantity Change</label>
              <input
                type="number"
                step="0.01"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={quantityChange}
                onChange={(event) => setQuantityChange(Number(event.target.value))}
              />
              <p className="mt-1 text-xs text-stone-500">Use negative numbers for waste or usage.</p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Reason</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                placeholder="Received shipment / Waste"
              />
            </div>
          </div>
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">Low Stock Alerts</h4>
          {isLoading ? (
            <p className="mt-4 text-sm text-stone-500">Loading…</p>
          ) : (
            <div className="mt-4 space-y-3">
              {lowStock.length === 0 ? (
                <p className="text-sm text-stone-500">All ingredients are healthy.</p>
              ) : (
                lowStock.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-2xl border border-rose-100 bg-rose-50/40 px-4 py-3"
                  >
                    <div>
                      <p className="text-sm font-semibold text-stone-900">{item.name}</p>
                      <p className="text-xs text-stone-500">
                        {item.stockQuantity.toFixed(2)} {item.unit} / reorder at {item.reorderLevel.toFixed(2)}
                      </p>
                    </div>
                    <span className="rounded-full bg-rose-100 px-3 py-1 text-xs text-rose-700">Low</span>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      </section>

      <section className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">All Ingredients</h4>
          {isLoading ? (
            <p className="mt-4 text-sm text-stone-500">Loading…</p>
          ) : (
            <div className="mt-4 grid gap-4 md:grid-cols-2">
              {items.map((item) => (
                <div
                  key={item.id}
                  className="flex flex-col gap-3 rounded-2xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3"
                >
                  {editId === item.id ? (
                    <>
                      <input
                        className="rounded-xl border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                        value={editForm.name}
                        onChange={(event) => setEditForm((prev) => ({ ...prev, name: event.target.value }))}
                      />
                      <div className="grid grid-cols-2 gap-2">
                        <input
                          className="rounded-xl border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                          value={editForm.unit}
                          onChange={(event) => setEditForm((prev) => ({ ...prev, unit: event.target.value }))}
                        />
                        <input
                          type="number"
                          step="0.01"
                          className="rounded-xl border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                          value={editForm.stockQuantity}
                          onChange={(event) =>
                            setEditForm((prev) => ({ ...prev, stockQuantity: Number(event.target.value) }))
                          }
                        />
                      </div>
                      <input
                        type="number"
                        step="0.01"
                        className="rounded-xl border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                        value={editForm.reorderLevel}
                        onChange={(event) =>
                          setEditForm((prev) => ({ ...prev, reorderLevel: Number(event.target.value) }))
                        }
                      />
                      <input
                        type="number"
                        step="0.01"
                        className="rounded-xl border border-[var(--border)] bg-[var(--card)] px-3 py-2 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                        value={editForm.costPerUnit}
                        onChange={(event) =>
                          setEditForm((prev) => ({ ...prev, costPerUnit: Number(event.target.value) }))
                        }
                      />
                      <label className="flex items-center gap-2 text-xs text-stone-500">
                        <input
                          type="checkbox"
                          checked={editForm.isActive}
                          onChange={(event) => setEditForm((prev) => ({ ...prev, isActive: event.target.checked }))}
                        />
                        Active
                      </label>
                      <div className="flex gap-2">
                        <button
                          className="flex-1 btn btn-ghost btn-pill btn-xs"
                          onClick={handleUpdate}
                          data-loading={isSubmitting}
                        >
                          Save
                        </button>
                        <button
                          className="btn btn-ghost btn-pill btn-xs flex-1"
                          onClick={() => setEditId(null)}
                        >
                          Cancel
                        </button>
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-sm font-semibold text-stone-900">{item.name}</p>
                          <p className="text-xs text-stone-500">
                            {item.stockQuantity.toFixed(2)} {item.unit}
                          </p>
                          <p className="text-xs text-stone-500">
                            Cost: MMK {item.costPerUnit.toFixed(2)} / {item.unit}
                          </p>
                        </div>
                        <span
                          className={`rounded-full px-3 py-1 text-xs ${
                            item.stockQuantity <= item.reorderLevel
                              ? 'bg-rose-100 text-rose-700'
                              : 'bg-emerald-100 text-emerald-700'
                          }`}
                        >
                          {item.stockQuantity <= item.reorderLevel ? 'Low' : 'Good'}
                        </span>
                      </div>
                      <div className="flex gap-2">
                        <button
                          className="flex-1 btn btn-ghost btn-pill btn-xs"
                          onClick={() => startEdit(item)}
                        >
                          Edit
                        </button>
                        <button
                          className="btn btn-ghost btn-pill btn-xs flex-1 text-[var(--danger)] border-[var(--danger-border)]"
                          onClick={() => handleDelete(item)}
                        >
                          Delete
                        </button>
                      </div>
                    </>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">Add Ingredient</h4>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Name</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={newIngredient.name}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, name: event.target.value }))}
                placeholder="Tomatoes"
              />
              <p className="mt-1 text-xs text-stone-500">Ingredient name as shown in recipes.</p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Unit</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={newIngredient.unit}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, unit: event.target.value }))}
                placeholder="kg / L / each"
              />
              <p className="mt-1 text-xs text-stone-500">Unit for stock tracking (kg, L, each).</p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Stock</label>
              <input
                type="number"
                step="0.01"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={newIngredient.stockQuantity}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, stockQuantity: Number(event.target.value) }))}
              />
              <p className="mt-1 text-xs text-stone-500">Current quantity on hand.</p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Reorder Level</label>
              <input
                type="number"
                step="0.01"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={newIngredient.reorderLevel}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, reorderLevel: Number(event.target.value) }))}
              />
              <p className="mt-1 text-xs text-stone-500">If stock drops to this level, it’s flagged as Low.</p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Cost Per Unit</label>
              <input
                type="number"
                step="0.01"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={newIngredient.costPerUnit}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, costPerUnit: Number(event.target.value) }))}
              />
              <p className="mt-1 text-xs text-stone-500">Used to calculate ingredient cost and profit.</p>
            </div>
            <label className="flex items-center gap-2 text-sm text-stone-600">
              <input
                type="checkbox"
                checked={newIngredient.isActive}
                onChange={(event) => setNewIngredient((prev) => ({ ...prev, isActive: event.target.checked }))}
              />
              Active
            </label>
            <button
              className="btn btn-primary btn-soft btn-md"
              onClick={handleCreateIngredient}
              disabled={isSubmitting}
              data-loading={isSubmitting}
            >
              {isSubmitting ? 'Saving...' : 'Add Ingredient'}
            </button>
          </div>
        </div>
      </section>
    </div>
  )
}


