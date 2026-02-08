import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function TopBar() {
  const navigate = useNavigate()
  const { userName, roles, logout } = useAuth()
  const canCreateOrder = roles.includes('Waiter') || roles.includes('Admin')
  const [darkMode, setDarkMode] = useState(() => localStorage.getItem('pos_theme') === 'dark')
  const [tabletMode, setTabletMode] = useState(() => localStorage.getItem('pos_tablet') === 'true')

  useEffect(() => {
    document.documentElement.classList.toggle('dark', darkMode)
    localStorage.setItem('pos_theme', darkMode ? 'dark' : 'light')
  }, [darkMode])

  useEffect(() => {
    document.documentElement.classList.toggle('tablet', tabletMode)
    localStorage.setItem('pos_tablet', tabletMode ? 'true' : 'false')
  }, [tabletMode])

  return (
    <header className="flex flex-wrap items-center justify-between gap-4 rounded-3xl border border-amber-200/60 bg-white/80 px-6 py-4 shadow-sm backdrop-blur dark:border-stone-700/60 dark:bg-stone-900/80">
      <div>
        <p className="text-xs uppercase tracking-[0.3em] text-amber-600/80 dark:text-amber-300/80">Live</p>
        <h2 className="text-xl font-semibold text-stone-900 dark:text-stone-100">Service Control Center</h2>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex flex-wrap items-center gap-2">
          {roles.map((role) => (
            <span
              key={role}
              className="rounded-full border border-amber-200 bg-amber-50 px-3 py-1 text-xs uppercase tracking-[0.2em] text-amber-700 dark:border-stone-700 dark:bg-stone-800 dark:text-amber-200"
            >
              {role}
            </span>
          ))}
        </div>
        <span className="text-sm text-stone-500 dark:text-stone-300">{userName}</span>
        <button
          className="btn btn-ghost btn-pill btn-md"
          onClick={() => setDarkMode((prev) => !prev)}
        >
          {darkMode ? 'Light' : 'Dark'}
        </button>
        <button
          className="btn btn-ghost btn-pill btn-md"
          onClick={() => setTabletMode((prev) => !prev)}
        >
          {tabletMode ? 'Tablet On' : 'Tablet Off'}
        </button>
        <button
          className="btn btn-ghost btn-pill btn-md"
          onClick={logout}
        >
          Log out
        </button>
        {canCreateOrder && (
          <button
            className="btn btn-primary btn-pill btn-md"
            onClick={() => navigate('/orders')}
          >
            New Order
          </button>
        )}
      </div>
    </header>
  )
}
