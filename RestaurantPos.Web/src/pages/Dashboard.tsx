import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchDashboardSummary } from '../api/dashboard'
import { fetchOrders } from '../api/orders'
import { fetchTables } from '../api/tables'
import type { Order, DiningTable } from '../types/orders'

const statuses = ['All', 'Open', 'InProgress', 'Ready', 'Served', 'Paid', 'Closed', 'Cancelled']

export default function Dashboard() {
  const navigate = useNavigate()
  const [summary, setSummary] = useState({
    openOrders: 0,
    kitchenQueue: 0,
    occupiedTables: 0,
    totalTables: 0,
    lowStockCount: 0
  })
  const [orders, setOrders] = useState<Order[]>([])
  const [tables, setTables] = useState<DiningTable[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isRefreshing, setIsRefreshing] = useState(false)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [statusFilter, setStatusFilter] = useState('All')

  const load = useCallback(async () => {
    const [summaryData, orderData, tableData] = await Promise.all([
      fetchDashboardSummary(),
      fetchOrders(),
      fetchTables()
    ])
    setSummary(summaryData)
    setOrders(orderData)
    setTables(tableData)
    setLastUpdated(new Date())
  }, [])

  useEffect(() => {
    let active = true
    const init = async () => {
      try {
        await load()
      } finally {
        if (active) setIsLoading(false)
      }
    }

    init()
    const timer = setInterval(() => {
      load().catch(() => undefined)
    }, 30000)

    return () => {
      active = false
      clearInterval(timer)
    }
  }, [load])

  const handleRefresh = async () => {
    setIsRefreshing(true)
    try {
      await load()
    } finally {
      setIsRefreshing(false)
    }
  }

  const stats = [
    {
      label: 'Open Orders',
      value: summary.openOrders.toString(),
      trend: isLoading ? 'Loading...' : 'Live count'
    },
    {
      label: 'Tables Seated',
      value: `${summary.occupiedTables} / ${summary.totalTables}`,
      trend: isLoading ? 'Loading...' : 'Occupied right now'
    },
    {
      label: 'Kitchen Queue',
      value: summary.kitchenQueue.toString(),
      trend: isLoading ? 'Loading...' : 'In progress + ready'
    },
    {
      label: 'Inventory Alerts',
      value: summary.lowStockCount.toString(),
      trend: isLoading ? 'Loading...' : 'Below reorder level'
    }
  ]

  const filteredOrders = useMemo(() => {
    if (statusFilter === 'All') return orders
    return orders.filter((order) => order.status === statusFilter)
  }, [orders, statusFilter])

  const recentOrders = useMemo(() => filteredOrders.slice(0, 5), [filteredOrders])

  return (
    <div className="flex flex-col gap-6 fade-in">
      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => (
          <div
            key={stat.label}
            className="rounded-3xl border border-amber-200/60 bg-white/80 p-5 shadow-sm"
          >
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">{stat.label}</p>
            <p className="mt-2 text-3xl font-semibold text-stone-900">{stat.value}</p>
            <p className="mt-3 text-sm text-stone-500">{stat.trend}</p>
          </div>
        ))}
      </section>

      <section className="grid gap-6 lg:grid-cols-[2fr_1fr]">
        <div className="rounded-3xl border border-amber-200/60 bg-white/80 p-6 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-lg font-semibold text-stone-900">Live Orders</h3>
              <p className="text-xs text-stone-500">
                {lastUpdated ? `Updated ${lastUpdated.toLocaleTimeString()}` : 'Updating...'}
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <select
                className="rounded-full border border-amber-200 bg-white px-3 py-1 text-xs text-stone-600"
                value={statusFilter}
                onChange={(event) => setStatusFilter(event.target.value)}
              >
                {statuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
              <button
                className="btn btn-ghost btn-pill btn-xs"
                onClick={handleRefresh}
                disabled={isRefreshing}
              >
                {isRefreshing ? 'Refreshing...' : 'Refresh'}
              </button>
            </div>
          </div>
          <div className="mt-4 space-y-3">
            {recentOrders.length === 0 && !isLoading ? (
              <div className="rounded-2xl border border-amber-100 bg-amber-50/60 px-4 py-3 text-sm text-stone-500">
                No recent orders.
              </div>
            ) : (
              recentOrders.map((order) => {
                const minutes = Math.max(
                  0,
                  Math.floor((Date.now() - new Date(order.createdAt).getTime()) / 60000)
                )

                return (
                  <div
                    key={order.id}
                    className="flex items-center justify-between rounded-2xl border border-amber-100 bg-amber-50/60 px-4 py-3"
                  >
                    <div>
                      <p className="text-sm font-semibold text-stone-900">Order #{order.id}</p>
                      <p className="text-xs text-stone-500">{order.tableName} · {order.items.length} items</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-semibold text-amber-700">{order.status}</p>
                      <p className="text-xs text-stone-500">{minutes} min ago</p>
                    </div>
                  </div>
                )
              })
            )}
          </div>
        </div>

        <div className="rounded-3xl border border-amber-200/60 bg-white/80 p-6 shadow-sm">
          <h3 className="text-lg font-semibold text-stone-900">Table Pulse</h3>
          <div className="mt-4 grid grid-cols-3 gap-3">
            {tables.map((table) => (
              <div
                key={table.id}
                className={`rounded-2xl border px-3 py-4 text-center ${
                  table.isAvailable
                    ? 'border-amber-100 bg-amber-50/40'
                    : 'border-rose-200 bg-rose-50'
                }`}
              >
                <p className="text-sm font-semibold text-stone-900">{table.name}</p>
                <p className="text-[11px] uppercase tracking-[0.2em] text-stone-500">
                  {table.isAvailable ? 'Available' : 'Occupied'}
                </p>
              </div>
            ))}
          </div>
          <button
            className="mt-4 w-full rounded-2xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)]"
            onClick={() => navigate('/tables')}
          >
            Manage Tables
          </button>
        </div>
      </section>
    </div>
  )
}
