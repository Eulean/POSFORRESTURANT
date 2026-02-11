import { useEffect, useMemo, useState } from 'react'
import { createOrder, fetchOrderReceipt, fetchOrders, updateOrderStatus } from '../api/orders'
import { fetchTables } from '../api/tables'
import { fetchMenuItems } from '../api/menu'
import { addPayment } from '../api/payments'
import type { MenuItem } from '../types/api'
import type { DiningTable, Order, OrderCreateItem } from '../types/orders'
import type { PaymentCreateRequest } from '../types/payments'
import { useAuth } from '../context/AuthContext'
import { API_BASE_URL } from '../api/config'

const statusFlow = ['Open', 'InProgress', 'Ready', 'Served', 'Paid', 'Closed']

export default function Orders() {
  const { roles } = useAuth()
  const isCashier = roles.includes('Cashier') || roles.includes('Admin')
  const [orders, setOrders] = useState<Order[]>([])
  const [tables, setTables] = useState<DiningTable[]>([])
  const [menuItems, setMenuItems] = useState<MenuItem[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [tableId, setTableId] = useState<number | ''>('')
  const [selectedItems, setSelectedItems] = useState<OrderCreateItem[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [paymentOrderId, setPaymentOrderId] = useState<number | null>(null)
  const [paymentForm, setPaymentForm] = useState<PaymentCreateRequest>({
    amount: 0,
    method: 'Cash',
    reference: ''
  })
  const imageUrlFor = (item: MenuItem) => {
    if (!item.imageUrl) return ''
    return item.imageUrl.startsWith('http') ? item.imageUrl : `${API_BASE_URL}/${item.imageUrl}`
  }

  useEffect(() => {
    let active = true
    const load = async (showLoader: boolean) => {
      if (showLoader) setIsLoading(true)
      try {
        const [ordersData, tablesData, menuData] = await Promise.all([
          fetchOrders(),
          fetchTables(),
          fetchMenuItems()
        ])
        if (!active) return
        setOrders(ordersData)
        setTables(tablesData)
        setMenuItems(menuData)
        setError(null)
      } catch {
        if (active) setError('Unable to load orders.')
      } finally {
        if (active && showLoader) setIsLoading(false)
      }
    }

    load(true)
    const timer = setInterval(() => {
      load(false)
    }, 10000)

    return () => {
      active = false
      clearInterval(timer)
    }
  }, [])

  const grouped = useMemo(() => {
    const groups: Record<string, Order[]> = {
      Open: [],
      InProgress: [],
      Ready: [],
      Served: [],
      Paid: [],
      Closed: [],
      Cancelled: []
    }

    orders.forEach((order) => {
      const key = groups[order.status] ? order.status : 'Open'
      groups[key].push(order)
    })

    return groups
  }, [orders])

  const handleAddItem = (menuItemId: number) => {
    setSelectedItems((prev) => {
      const existing = prev.find((item) => item.menuItemId === menuItemId)
      if (existing) {
        return prev.map((item) =>
          item.menuItemId === menuItemId ? { ...item, quantity: item.quantity + 1 } : item
        )
      }
      return [...prev, { menuItemId, quantity: 1 }]
    })
  }

  const handleRemoveItem = (menuItemId: number) => {
    setSelectedItems((prev) =>
      prev
        .map((item) =>
          item.menuItemId === menuItemId ? { ...item, quantity: item.quantity - 1 } : item
        )
        .filter((item) => item.quantity > 0)
    )
  }

  const handleCreateOrder = async () => {
    if (!tableId || selectedItems.length === 0) return
    setIsSubmitting(true)
    try {
      const order = await createOrder({
        diningTableId: Number(tableId),
        items: selectedItems
      })
      setOrders((prev) => [order, ...prev])
      setSelectedItems([])
      setTableId('')
    } catch {
      setError('Unable to create order. Check table availability.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleAdvanceStatus = async (order: Order) => {
    const currentIndex = statusFlow.indexOf(order.status)
    if (currentIndex === -1 || currentIndex === statusFlow.length - 1) return
    const nextStatus = statusFlow[currentIndex + 1]
    await updateOrderStatus(order.id, { status: nextStatus })
    setOrders((prev) => prev.map((item) => (item.id === order.id ? { ...item, status: nextStatus } : item)))
  }

  const handleCancelOrder = async (order: Order) => {
    await updateOrderStatus(order.id, { status: 'Cancelled' })
    setOrders((prev) => prev.map((item) => (item.id === order.id ? { ...item, status: 'Cancelled' } : item)))
  }

  const handleOpenPayment = (order: Order) => {
    if (!isCashier) return
    setPaymentOrderId(order.id)
    setPaymentForm({ amount: order.totalAmount, method: 'Cash', reference: '' })
  }

  const handleReceipt = async (order: Order) => {
    try {
      const blob = await fetchOrderReceipt(order.id)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `receipt-${order.id}.pdf`
      link.click()
      URL.revokeObjectURL(url)
    } catch {
      setError('Unable to download receipt.')
    }
  }

  const handleSubmitPayment = async () => {
    if (!paymentOrderId || !isCashier) return
    setIsSubmitting(true)
    try {
      await addPayment(paymentOrderId, {
        amount: paymentForm.amount,
        method: paymentForm.method,
        reference: paymentForm.reference?.trim() || null
      })
      await updateOrderStatus(paymentOrderId, { status: 'Closed' })
      setOrders((prev) =>
        prev.map((order) =>
          order.id === paymentOrderId
            ? { ...order, status: 'Closed' }
            : order
        )
      )
      setPaymentOrderId(null)
      setPaymentForm({ amount: 0, method: 'Cash', reference: '' })
    } catch {
      setError('Payment failed. Try again.')
    } finally {
      setIsSubmitting(false)
    }
  }

  const selectedSummary = useMemo(() => {
    return selectedItems
      .map((item) => {
        const menuItem = menuItems.find((menu) => menu.id === item.menuItemId)
        return {
          ...item,
          name: menuItem?.name ?? 'Item'
        }
      })
      .filter((item) => item.quantity > 0)
  }, [menuItems, selectedItems])

  const selectedTotal = useMemo(() => {
    return selectedItems.reduce((sum, item) => {
      const menuItem = menuItems.find((menu) => menu.id === item.menuItemId)
      return sum + (menuItem?.price ?? 0) * item.quantity
    }, 0)
  }, [menuItems, selectedItems])

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900">Order Flow</h3>
          <p className="text-sm text-stone-500">Create and track orders in real time.</p>
        </div>
        <button
          className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)]"
          onClick={handleCreateOrder}
          disabled={isSubmitting || !tableId || selectedItems.length === 0}          data-loading={isSubmitting}
        >
          {isSubmitting ? 'Creating...' : 'Create Order'}
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

      <section className="grid gap-6 lg:grid-cols-[1.1fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">Create Order</h4>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Table</label>
              <select
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                value={tableId}
                onChange={(event) => setTableId(event.target.value ? Number(event.target.value) : '')}
              >
                <option value="">Select table</option>
                {tables.map((table) => (
                  <option key={table.id} value={table.id}>
                    {table.name} · {table.capacity} seats {table.isAvailable ? '' : '(busy)'}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Add Items</label>
              <div className="mt-2 grid gap-2 md:grid-cols-2">
                {menuItems.map((menu) => (
                  <button
                    key={menu.id}
                    className="flex items-center justify-between gap-3 rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-2 text-sm text-[var(--ink)] hover:border-[var(--accent)]"
                    onClick={() => handleAddItem(menu.id)}
                  >
                    <div className="flex items-center gap-3">
                      {imageUrlFor(menu) ? (
                        <img
                          src={imageUrlFor(menu)}
                          alt={menu.name}
                          className="h-10 w-10 rounded-xl object-cover"
                          loading="lazy"
                        />
                      ) : (
                        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-[var(--surface)] text-[10px] text-[var(--muted)]">
                          No image
                        </div>
                      )}
                      <span>{menu.name}</span>
                    </div>
                    <span className="text-xs text-amber-700">+ Add</span>
                  </button>
                ))}
              </div>
            </div>

            <div className="rounded-2xl border border-amber-100 bg-amber-50/40 p-4">
              <p className="text-xs uppercase tracking-[0.2em] text-amber-600/80">Selected</p>
              {selectedSummary.length === 0 ? (
                <p className="mt-2 text-sm text-stone-500">No items selected.</p>
              ) : (
                <div className="mt-3 space-y-2">
                  {selectedSummary.map((item) => (
                    <div key={item.menuItemId} className="flex items-center justify-between text-sm">
                      <span>{item.name}</span>
                      <div className="flex items-center gap-2">
                        <button
                          className="rounded-full border border-amber-200 px-2"
                          onClick={() => handleRemoveItem(item.menuItemId)}
                        >
                          -
                        </button>
                        <span className="w-6 text-center">{item.quantity}</span>
                        <button
                          className="rounded-full border border-amber-200 px-2"
                          onClick={() => handleAddItem(item.menuItemId)}
                        >
                          +
                        </button>
                      </div>
                    </div>
                  ))}
                  <div className="mt-3 flex items-center justify-between border-t border-amber-100 pt-3 text-sm font-semibold text-stone-900">
                    <span>Estimated total</span>
                    <span>MMK {selectedTotal.toFixed(2)}</span>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900">Current Orders</h4>
          {isLoading ? (
            <p className="mt-4 text-sm text-stone-500">Loading…</p>
          ) : (
            <div className="mt-4 space-y-3">
              {orders.slice(0, 6).map((order) => (
                <div key={order.id} className="rounded-2xl border border-amber-100 bg-white px-4 py-3">
                  <p className="text-sm font-semibold text-stone-900">Order #{order.id}</p>
                  <p className="text-xs text-stone-500">{order.tableName} · {order.items.length} items</p>
                  <div className="mt-2 flex flex-wrap items-center gap-2 text-xs">
                    <span className="rounded-full bg-amber-100 px-3 py-1 text-amber-800">{order.status}</span>
                    {order.status !== 'Cancelled' && order.status !== 'Closed' && (
                      <button
                        className="rounded-full border border-amber-200 px-3 py-1 text-amber-700"
                        onClick={() => handleAdvanceStatus(order)}
                      >
                        Advance
                      </button>
                    )}
                    {order.status !== 'Cancelled' && order.status !== 'Closed' && (
                      <button
                        className="rounded-full border border-rose-200 px-3 py-1 text-rose-700"
                        onClick={() => handleCancelOrder(order)}
                      >
                        Cancel
                      </button>
                    )}
                    {['Paid', 'Closed'].includes(order.status) && (
                      <div className="flex gap-2">
                        <button
                          className="btn btn-ghost btn-pill btn-xs"
                          onClick={() => handleReceipt(order)}
                        >
                          Receipt
                        </button>
                        <button
                          className="btn btn-ghost btn-pill btn-xs"
                          onClick={async () => {
                            try {
                              const blob = await fetchOrderReceipt(order.id)
                              const url = URL.createObjectURL(blob)
                              const win = window.open(url)
                              if (win) {
                                win.addEventListener('load', () => {
                                  win.focus()
                                  win.print()
                                })
                              }
                            } catch {
                              setError('Unable to open print preview.')
                            }
                          }}
                        >
                          Print
                        </button>
                      </div>
                    )}
                    {order.status === 'Served' && isCashier && (
                      <button
                        className="btn btn-ghost btn-pill btn-xs"
                        onClick={() => handleOpenPayment(order)}
                      >
                        Pay
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      <div className="grid gap-4 lg:grid-cols-4">
        {Object.entries(grouped).map(([status, list]) => (
          <div key={status} className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-4 shadow-sm">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-semibold text-stone-800">{status}</h4>
              <span className="rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-800">
                {list.length}
              </span>
            </div>
            <div className="mt-4 space-y-3">
              {list.map((order) => (
                <div key={order.id} className="rounded-2xl border border-amber-100 bg-white px-4 py-3">
                  <p className="text-sm font-semibold text-stone-900">#{order.id}</p>
                  <p className="text-xs text-stone-500">{order.tableName}</p>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>

      {paymentOrderId && isCashier && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h4 className="text-lg font-semibold text-stone-900">Capture Payment</h4>
            <p className="mt-1 text-sm text-stone-500">Order #{paymentOrderId}</p>
            <div className="mt-4 space-y-3">
              <div>
                <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Amount</label>
                <input
                  type="number"
                  step="0.01"
                  className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                  value={paymentForm.amount}
                  onChange={(event) =>
                    setPaymentForm((prev) => ({ ...prev, amount: Number(event.target.value) }))
                  }
                />
              </div>
              <div>
                <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Method</label>
                <select
                  className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                  value={paymentForm.method}
                  onChange={(event) =>
                    setPaymentForm((prev) => ({ ...prev, method: event.target.value }))
                  }
                >
                  <option value="Cash">Cash</option>
                  <option value="Card">Card</option>
                  <option value="Mobile">Mobile</option>
                </select>
              </div>
              <div>
                <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Reference</label>
                <input
                  className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none"
                  value={paymentForm.reference ?? ''}
                  onChange={(event) =>
                    setPaymentForm((prev) => ({ ...prev, reference: event.target.value }))
                  }
                  placeholder="Receipt or last 4"
                />
                <p className="mt-1 text-xs text-stone-500">
                  Optional. Use card last 4 digits, mobile transfer ID, or receipt number.
                </p>
              </div>
            </div>
            <div className="mt-6 flex gap-2">
              <button
                className="flex-1 btn btn-ghost btn-soft btn-sm"
                onClick={() => setPaymentOrderId(null)}
              >
                Cancel
              </button>
              <button
                className="flex-1 rounded-2xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)]"
                onClick={handleSubmitPayment}
                disabled={isSubmitting}                data-loading={isSubmitting}              >
                {isSubmitting ? 'Processing...' : 'Confirm Payment'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}







