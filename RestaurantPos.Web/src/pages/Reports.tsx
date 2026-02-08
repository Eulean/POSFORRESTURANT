import { useEffect, useState } from 'react'
import {
  closeDay,
  exportDailyReport,
  exportMonthlyReport,
  fetchDailyReport,
  fetchMonthlyReport
} from '../api/reports'
import { useToast } from '../context/ToastContext'

type ReportState = {
  dateUtc: string
  ordersToday: number
  totalSales: number
  ingredientCost: number
  profit: number
  topItems: { name: string; quantity: number }[]
  revenueByMethod: { method: string; total: number }[]
  busiestHour: { hour: number; orders: number } | null
}

type MonthlyState = {
  startUtc: string
  endUtc: string
  ordersTotal: number
  totalSales: number
  ingredientCost: number
  profit: number
  topItems: { name: string; quantity: number }[]
  revenueByMethod: { method: string; total: number }[]
  dailyBreakdown: { dateUtc: string; orders: number; totalSales: number; ingredientCost: number; profit: number }[]
}

export default function Reports() {
  const { addToast } = useToast()
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [rangeStart, setRangeStart] = useState(() => {
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10)
  })
  const [rangeEnd, setRangeEnd] = useState(() => new Date().toISOString().slice(0, 10))
  const [report, setReport] = useState<ReportState>({
    dateUtc: new Date().toISOString(),
    ordersToday: 0,
    totalSales: 0,
    ingredientCost: 0,
    profit: 0,
    topItems: [],
    revenueByMethod: [],
    busiestHour: null
  })
  const [monthly, setMonthly] = useState<MonthlyState>({
    startUtc: new Date().toISOString(),
    endUtc: new Date().toISOString(),
    ordersTotal: 0,
    totalSales: 0,
    ingredientCost: 0,
    profit: 0,
    topItems: [],
    revenueByMethod: [],
    dailyBreakdown: []
  })
  const [isLoading, setIsLoading] = useState(true)
  const [isMonthlyLoading, setIsMonthlyLoading] = useState(true)
  const [isExporting, setIsExporting] = useState(false)
  const [isMonthlyExporting, setIsMonthlyExporting] = useState(false)
  const [isClosing, setIsClosing] = useState(false)

  const load = async (value?: string) => {
    setIsLoading(true)
    const data = await fetchDailyReport(value ? new Date(value).toISOString() : undefined)
    setReport({
      dateUtc: data.dateUtc,
      ordersToday: data.ordersToday,
      totalSales: data.totalSales,
      ingredientCost: data.ingredientCost,
      profit: data.profit,
      topItems: data.topItems,
      revenueByMethod: data.revenueByMethod,
      busiestHour: data.busiestHour ?? null
    })
    setIsLoading(false)
  }

  useEffect(() => {
    load(date)
  }, [date])

  const loadMonthly = async (start?: string, end?: string) => {
    setIsMonthlyLoading(true)
    const data = await fetchMonthlyReport(
      start ? new Date(start).toISOString() : undefined,
      end ? new Date(end).toISOString() : undefined
    )
    setMonthly({
      startUtc: data.startUtc,
      endUtc: data.endUtc,
      ordersTotal: data.ordersTotal,
      totalSales: data.totalSales,
      ingredientCost: data.ingredientCost,
      profit: data.profit,
      topItems: data.topItems,
      revenueByMethod: data.revenueByMethod,
      dailyBreakdown: data.dailyBreakdown
    })
    setIsMonthlyLoading(false)
  }

  useEffect(() => {
    loadMonthly(rangeStart, rangeEnd)
  }, [rangeStart, rangeEnd])

  const handleExport = async () => {
    setIsExporting(true)
    try {
      const blob = await exportDailyReport(date)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `daily-report-${date}.csv`
      link.click()
      URL.revokeObjectURL(url)
      addToast('Report exported.')
    } finally {
      setIsExporting(false)
    }
  }

  const handleMonthlyExport = async () => {
    setIsMonthlyExporting(true)
    try {
      const blob = await exportMonthlyReport(
        rangeStart ? new Date(rangeStart).toISOString() : undefined,
        rangeEnd ? new Date(rangeEnd).toISOString() : undefined
      )
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = `monthly-report-${rangeStart.replace(/-/g, '')}.xlsx`
      link.click()
      URL.revokeObjectURL(url)
      addToast('Monthly report exported.')
    } finally {
      setIsMonthlyExporting(false)
    }
  }

  const handleCloseDay = async () => {
    setIsClosing(true)
    try {
      await closeDay(date)
      addToast('Close day snapshot saved.')
    } finally {
      setIsClosing(false)
    }
  }

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h3 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Daily Summary</h3>
          <p className="text-sm text-stone-500 dark:text-stone-400">Track sales, orders, and top items.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <input
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            className="btn btn-ghost btn-pill btn-sm"
          />
          <button
            className="rounded-full border border-amber-200 px-4 py-2 text-sm text-amber-700"
            onClick={handleExport}
            disabled={isExporting}            data-loading={isExporting}          >
            {isExporting ? 'Exporting...' : 'Export CSV'}
          </button>
          <button
            className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)] dark:bg-amber-500 dark:text-stone-900"
            onClick={handleCloseDay}
            disabled={isClosing}            data-loading={isClosing}          >
            {isClosing ? 'Saving...' : 'Close Day'}
          </button>
        </div>
      </div>

      <section className="grid gap-4 md:grid-cols-5">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Orders Today</p>
          <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
            {isLoading ? '...' : report.ordersToday}
          </p>
        </div>
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Total Sales</p>
          <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
            {isLoading ? '...' : `$${report.totalSales.toFixed(2)}`}
          </p>
        </div>
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Ingredient Cost</p>
          <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
            {isLoading ? '...' : `$${report.ingredientCost.toFixed(2)}`}
          </p>
        </div>
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Profit</p>
          <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
            {isLoading ? '...' : `$${report.profit.toFixed(2)}`}
          </p>
        </div>
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Busiest Hour</p>
          <p className="mt-2 text-lg font-semibold text-stone-900 dark:text-stone-100">
            {isLoading || !report.busiestHour
              ? '...' : `${report.busiestHour.hour}:00 (${report.busiestHour.orders} orders)`}
          </p>
        </div>
      </section>

      <section className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">Top Items</h4>
          <div className="mt-4 space-y-3">
            {isLoading ? (
              <p className="text-sm text-stone-500 dark:text-stone-400">Loading...</p>
            ) : report.topItems.length === 0 ? (
              <p className="text-sm text-stone-500 dark:text-stone-400">No sales for this date.</p>
            ) : (
              report.topItems.map((item) => (
                <div
                  key={item.name}
                  className="flex items-center justify-between rounded-2xl border border-amber-100 bg-amber-50/50 px-4 py-3 dark:border-stone-700 dark:bg-stone-950"
                >
                  <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">{item.name}</span>
                  <span className="text-xs text-stone-600 dark:text-stone-300">{item.quantity} sold</span>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">Revenue by Method</h4>
          <div className="mt-4 space-y-3">
            {isLoading ? (
              <p className="text-sm text-stone-500 dark:text-stone-400">Loading...</p>
            ) : report.revenueByMethod.length === 0 ? (
              <p className="text-sm text-stone-500 dark:text-stone-400">No payments for this date.</p>
            ) : (
              report.revenueByMethod.map((item) => (
                <div
                  key={item.method}
                  className="flex items-center justify-between rounded-2xl border border-amber-100 bg-amber-50/50 px-4 py-3 dark:border-stone-700 dark:bg-stone-950"
                >
                  <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">{item.method}</span>
                  <span className="text-xs text-stone-600 dark:text-stone-300">${item.total.toFixed(2)}</span>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h3 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Monthly Report</h3>
            <p className="text-sm text-stone-500 dark:text-stone-400">Search by date range and export to Excel.</p>
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="date"
              value={rangeStart}
              onChange={(event) => setRangeStart(event.target.value)}
              className="btn btn-ghost btn-pill btn-sm"
            />
            <input
              type="date"
              value={rangeEnd}
              onChange={(event) => setRangeEnd(event.target.value)}
              className="btn btn-ghost btn-pill btn-sm"
            />
            <button
              className="rounded-full border border-amber-200 px-4 py-2 text-sm text-amber-700"
              onClick={handleMonthlyExport}
              disabled={isMonthlyExporting}              data-loading={isMonthlyExporting}            >
              {isMonthlyExporting ? 'Exporting...' : 'Export Excel'}
            </button>
          </div>
        </div>

        <section className="grid gap-4 md:grid-cols-5">
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Orders Total</p>
            <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
              {isMonthlyLoading ? '...' : monthly.ordersTotal}
            </p>
          </div>
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Total Sales</p>
            <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
              {isMonthlyLoading ? '...' : `$${monthly.totalSales.toFixed(2)}`}
            </p>
          </div>
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Ingredient Cost</p>
            <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
              {isMonthlyLoading ? '...' : `$${monthly.ingredientCost.toFixed(2)}`}
            </p>
          </div>
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Profit</p>
            <p className="mt-2 text-3xl font-semibold text-stone-900 dark:text-stone-100">
              {isMonthlyLoading ? '...' : `$${monthly.profit.toFixed(2)}`}
            </p>
          </div>
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <p className="text-xs uppercase tracking-[0.2em] text-amber-700/70">Range</p>
            <p className="mt-2 text-sm font-semibold text-stone-900 dark:text-stone-100">
              {isMonthlyLoading
                ? '...'
                : `${monthly.startUtc.slice(0, 10)} â†’ ${new Date(new Date(monthly.endUtc).getTime() - 86400000).toISOString().slice(0, 10)}`}
            </p>
          </div>
        </section>

        <section className="grid gap-6 lg:grid-cols-[1.2fr_1fr]">
          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">Top Items (Range)</h4>
            <div className="mt-4 space-y-3">
              {isMonthlyLoading ? (
                <p className="text-sm text-stone-500 dark:text-stone-400">Loading...</p>
              ) : monthly.topItems.length === 0 ? (
                <p className="text-sm text-stone-500 dark:text-stone-400">No sales in this range.</p>
              ) : (
                monthly.topItems.map((item) => (
                  <div
                    key={item.name}
                    className="flex items-center justify-between rounded-2xl border border-amber-100 bg-amber-50/50 px-4 py-3 dark:border-stone-700 dark:bg-stone-950"
                  >
                    <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">{item.name}</span>
                    <span className="text-xs text-stone-600 dark:text-stone-300">{item.quantity} sold</span>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
            <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">Revenue by Method</h4>
            <div className="mt-4 space-y-3">
              {isMonthlyLoading ? (
                <p className="text-sm text-stone-500 dark:text-stone-400">Loading...</p>
              ) : monthly.revenueByMethod.length === 0 ? (
                <p className="text-sm text-stone-500 dark:text-stone-400">No payments in this range.</p>
              ) : (
                monthly.revenueByMethod.map((item) => (
                  <div
                    key={item.method}
                    className="flex items-center justify-between rounded-2xl border border-amber-100 bg-amber-50/50 px-4 py-3 dark:border-stone-700 dark:bg-stone-950"
                  >
                    <span className="text-sm font-semibold text-stone-900 dark:text-stone-100">{item.method}</span>
                    <span className="text-xs text-stone-600 dark:text-stone-300">${item.total.toFixed(2)}</span>
                  </div>
                ))
              )}
            </div>
          </div>
        </section>

        <section className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-stone-900 dark:text-stone-100">Daily Breakdown</h4>
          <div className="mt-4 overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-xs uppercase tracking-[0.2em] text-amber-700/70">
                  <th className="pb-2">Date</th>
                  <th className="pb-2">Orders</th>
                  <th className="pb-2">Total Sales</th>
                  <th className="pb-2">Ingredient Cost</th>
                  <th className="pb-2">Profit</th>
                </tr>
              </thead>
              <tbody>
                {isMonthlyLoading ? (
                  <tr>
                    <td className="py-2 text-stone-500 dark:text-stone-400" colSpan={5}>
                      Loading...
                    </td>
                  </tr>
                ) : monthly.dailyBreakdown.length === 0 ? (
                  <tr>
                    <td className="py-2 text-stone-500 dark:text-stone-400" colSpan={5}>
                      No data in this range.
                    </td>
                  </tr>
                ) : (
                  monthly.dailyBreakdown.map((day) => (
                    <tr key={day.dateUtc} className="border-t border-amber-100/70 dark:border-stone-800">
                      <td className="py-2 text-stone-700 dark:text-stone-200">{day.dateUtc.slice(0, 10)}</td>
                      <td className="py-2 text-stone-700 dark:text-stone-200">{day.orders}</td>
                      <td className="py-2 text-stone-700 dark:text-stone-200">${day.totalSales.toFixed(2)}</td>
                      <td className="py-2 text-stone-700 dark:text-stone-200">${day.ingredientCost.toFixed(2)}</td>
                      <td className="py-2 text-stone-700 dark:text-stone-200">${day.profit.toFixed(2)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>
      </section>
    </div>
  )
}



