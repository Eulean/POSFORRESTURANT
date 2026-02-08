import { useEffect, useMemo, useState } from 'react'
import { fetchOrders, updateOrderStatus } from '../api/orders'
import type { Order } from '../types/orders'

const columns = ['InProgress', 'Ready']

export default function Kitchen() {
  const [orders, setOrders] = useState<Order[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let active = true
    const load = async () => {
      const data = await fetchOrders()
      if (!active) return
      setOrders(data)
      setIsLoading(false)
    }

    load()
    const timer = setInterval(load, 15000)
    return () => {
      active = false
      clearInterval(timer)
    }
  }, [])

  const grouped = useMemo(() => {
    return columns.reduce<Record<string, Order[]>>((acc, status) => {
      acc[status] = orders.filter((order) => order.status === status)
      return acc
    }, {})
  }, [orders])

  const moveOrder = async (order: Order, nextStatus: string) => {
    await updateOrderStatus(order.id, { status: nextStatus })
    setOrders((prev) => prev.map((item) => (item.id === order.id ? { ...item, status: nextStatus } : item)))
  }

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Kitchen Screen</h3>
          <p className="text-sm text-stone-500 dark:text-stone-400">Focused view for in-progress and ready orders.</p>
        </div>
      </div>

      {isLoading ? (
        <p className="text-sm text-stone-500 dark:text-stone-400">Loading...</p>
      ) : (
        <div className="grid gap-6 lg:grid-cols-2">
          {columns.map((status) => (
            <div key={status} className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
              <div className="flex items-center justify-between">
                <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">{status}</h4>
                <span className="rounded-full bg-amber-100 px-3 py-1 text-xs text-amber-800 dark:bg-stone-800 dark:text-amber-200">
                  {grouped[status]?.length ?? 0}
                </span>
              </div>
              <div className="mt-4 space-y-4">
                {(grouped[status] ?? []).map((order) => (
                  <div key={order.id} className="rounded-2xl border border-amber-100 bg-amber-50/40 p-4 dark:border-stone-700 dark:bg-stone-950">
                    <div className="flex items-center justify-between">
                      <p className="text-lg font-semibold text-stone-900 dark:text-stone-100">Order #{order.id}</p>
                      <span className="text-sm text-stone-500 dark:text-stone-400">{order.tableName}</span>
                    </div>
                    <p className="mt-2 text-sm text-stone-500 dark:text-stone-400">{order.items.length} items</p>
                    <div className="mt-4 flex gap-3">
                      {status === 'InProgress' && (
                        <button
                          className="flex-1 btn btn-primary btn-soft btn-md"
                          onClick={() => moveOrder(order, 'Ready')}
                        >
                          Mark Ready
                        </button>
                      )}
                      {status === 'Ready' && (
                        <button
                          className="flex-1 rounded-2xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)] dark:bg-amber-500 dark:text-stone-900"
                          onClick={() => moveOrder(order, 'Served')}
                        >
                          Mark Served
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
