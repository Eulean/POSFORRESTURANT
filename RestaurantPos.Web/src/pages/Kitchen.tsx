import { useCallback, useEffect, useMemo, useState } from 'react'
import { fetchOrders, updateOrderStatus } from '../api/orders'
import type { Order } from '../types/orders'

const stages = [
  { status: 'Open', title: '1 · New orders', action: 'Start preparing', next: 'InProgress' },
  { status: 'InProgress', title: '2 · Preparing', action: 'Mark ready', next: 'Ready' },
  { status: 'Ready', title: '3 · Ready to serve', action: 'Mark served', next: 'Served' }
] as const

export default function Kitchen() {
  const [orders, setOrders] = useState<Order[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [busyId, setBusyId] = useState<number | null>(null)

  const refresh = useCallback(async () => {
    try {
      setOrders(await fetchOrders())
      setError('')
    } catch {
      setError('Could not refresh orders. Check the connection to the host laptop.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
    const timer = setInterval(() => void refresh(), 5000)
    return () => clearInterval(timer)
  }, [refresh])

  const grouped = useMemo(() => Object.fromEntries(stages.map(stage => [stage.status,
    orders.filter(order => order.status === stage.status)
      .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime())
  ])) as Record<(typeof stages)[number]['status'], Order[]>, [orders])

  const moveOrder = async (order: Order, nextStatus: string) => {
    if (busyId !== null) return
    setBusyId(order.id)
    try {
      await updateOrderStatus(order.id, { status: nextStatus })
      await refresh()
    } catch {
      setError(`Order #${order.id} could not be updated. Another staff member may have changed it; refresh and try again.`)
      try { setOrders(await fetchOrders()) } catch { /* Keep visible error. */ }
    } finally {
      setBusyId(null)
    }
  }

  return (
    <div className="flex flex-col gap-5 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Kitchen orders</h3>
          <p className="text-sm text-stone-500 dark:text-stone-400">New → Preparing → Ready. Orders refresh every 5 seconds.</p>
        </div>
        <button onClick={() => void refresh()} className="rounded-xl border border-[var(--border)] px-4 py-2 text-sm font-semibold">Refresh</button>
      </div>
      {error && <p role="alert" className="rounded-xl bg-[var(--danger-bg)] p-3 text-sm text-[var(--danger)]">{error}</p>}
      {isLoading ? <p className="text-sm text-stone-500">Loading orders…</p> : (
        <div className="grid gap-4 lg:grid-cols-3">
          {stages.map(stage => (
            <section key={stage.status} className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-4 shadow-sm">
              <div className="flex items-center justify-between gap-2">
                <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">{stage.title}</h4>
                <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-800 dark:bg-stone-800 dark:text-amber-200">{grouped[stage.status].length}</span>
              </div>
              <div className="mt-4 space-y-3">
                {grouped[stage.status].length === 0 && <p className="rounded-2xl border border-dashed border-[var(--border)] p-5 text-center text-sm text-[var(--muted)]">No orders in this step</p>}
                {grouped[stage.status].map(order => (
                  <article key={order.id} className="rounded-2xl border border-amber-100 bg-amber-50/40 p-4 dark:border-stone-700 dark:bg-stone-950">
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-lg font-semibold text-stone-900 dark:text-stone-100">#{order.id} · {order.tableName}</p>
                      <time className="whitespace-nowrap text-xs text-stone-500">{new Date(order.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</time>
                    </div>
                    <ul className="my-4 space-y-2 text-sm text-stone-800 dark:text-stone-200">
                      {order.items.map(item => <li key={item.id} className="flex gap-2"><span className="font-bold">{item.quantity}×</span><span>{item.menuItemName}</span></li>)}
                    </ul>
                    <button disabled={busyId !== null} className="w-full btn btn-primary btn-soft btn-md disabled:opacity-50" onClick={() => void moveOrder(order, stage.next)}>
                      {busyId === order.id ? 'Updating…' : stage.action}
                    </button>
                  </article>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  )
}
