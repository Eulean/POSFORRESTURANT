import { useEffect, useState } from 'react'
import { createTable, fetchTables, updateTable } from '../api/tables'
import type { DiningTable } from '../types/orders'

const emptyForm = { name: '', capacity: 2, isAvailable: true }

export default function Tables() {
  const [tables, setTables] = useState<DiningTable[]>([])
  const [form, setForm] = useState(emptyForm)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const data = await fetchTables()
        if (active) setTables(data)
      } catch {
        if (active) setError('Unable to load tables.')
      } finally {
        if (active) setIsLoading(false)
      }
    }

    load()
    return () => {
      active = false
    }
  }, [])

  const handleCreate = async () => {
    if (!form.name.trim()) {
      setError('Table name is required.')
      return
    }

    setIsSubmitting(true)
    try {
      const newTable = await createTable({
        name: form.name.trim(),
        capacity: form.capacity,
        isAvailable: form.isAvailable
      })
      setTables((prev) => [...prev, newTable])
      setForm(emptyForm)
      setError(null)
    } catch {
      setError('Unable to create table.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const toggleAvailability = async (table: DiningTable) => {
    const updated = { ...table, isAvailable: !table.isAvailable }
    await updateTable(table.id, {
      name: updated.name,
      capacity: updated.capacity,
      isAvailable: updated.isAvailable
    })
    setTables((prev) => prev.map((item) => (item.id === table.id ? updated : item)))
  }

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900">Tables</h3>
          <p className="text-sm text-stone-500">Manage seating and availability.</p>
        </div>
        <button
          className="btn btn-primary btn-pill btn-md"
          onClick={handleCreate}
          disabled={isSubmitting}
        >
          {isSubmitting ? 'Saving...' : 'Add Table'}
        </button>
      </div>

      {error && (
        <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-4 text-sm text-rose-600">
          {error}
        </div>
      )}

      <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
        <h4 className="text-lg font-semibold text-stone-900">New Table</h4>
        <div className="mt-4 grid gap-4 md:grid-cols-3">
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Name</label>
            <input
              className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
              value={form.name}
              onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
              placeholder="Table 1"
            />
          </div>
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Seats</label>
            <input
              type="number"
              min={1}
              className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
              value={form.capacity}
              onChange={(event) => setForm((prev) => ({ ...prev, capacity: Number(event.target.value) }))}
            />
          </div>
          <div className="flex items-center gap-3">
            <input
              id="available"
              type="checkbox"
              checked={form.isAvailable}
              onChange={(event) => setForm((prev) => ({ ...prev, isAvailable: event.target.checked }))}
              className="h-4 w-4 rounded border-amber-200"
            />
            <label htmlFor="available" className="text-sm text-stone-600">Available</label>
          </div>
        </div>
      </div>

      <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
        <h4 className="text-lg font-semibold text-stone-900">Current Tables</h4>
        {isLoading ? (
          <p className="mt-4 text-sm text-stone-500">Loading</p>
        ) : (
          <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {tables.map((table) => (
              <div key={table.id} className="rounded-3xl border border-amber-100 bg-white p-5">
                <div className="flex items-center justify-between">
                  <h4 className="text-lg font-semibold text-stone-900">{table.name}</h4>
                  <span className={`rounded-full px-3 py-1 text-xs ${table.isAvailable ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'}`}>
                    {table.isAvailable ? 'Available' : 'Busy'}
                  </span>
                </div>
                <p className="mt-2 text-sm text-stone-500">Seats {table.capacity}</p>
                <div className="mt-4 flex gap-2">
                  <button
                    className="flex-1 btn btn-ghost btn-pill btn-xs"
                    onClick={() => toggleAvailability(table)}
                  >
                    Toggle
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
