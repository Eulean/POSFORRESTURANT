import { NavLink } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const allLinks = [
  { to: '/', label: 'Overview', roles: ['Admin', 'Waiter', 'Cashier'] },
  { to: '/orders', label: 'Orders', roles: ['Admin', 'Waiter', 'Cashier'] },
  { to: '/kitchen', label: 'Kitchen', roles: ['Admin', 'Waiter', 'Cashier'] },
  { to: '/menu', label: 'Menu', roles: ['Admin'] },
  { to: '/tables', label: 'Tables', roles: ['Admin', 'Waiter'] },
  { to: '/inventory', label: 'Inventory', roles: ['Admin'] },
  { to: '/reports', label: 'Reports', roles: ['Admin'] },
  { to: '/admin', label: 'Admin', roles: ['Admin'] }
]

export default function Sidebar() {
  const { roles } = useAuth()
  const links = allLinks.filter((link) => link.roles.some((role) => roles.includes(role)))

  return (
    <aside className="flex h-full flex-col gap-6 border-r border-amber-200/60 bg-white/70 p-6 backdrop-blur dark:border-stone-800 dark:bg-stone-950/70">
      <div className="space-y-2">
        <p className="text-xs uppercase tracking-[0.3em] text-amber-600/80 dark:text-amber-300/80">Restaurant POS</p>
        <h1 className="text-2xl font-semibold text-stone-900 dark:text-stone-100">Golden Oak</h1>
        <p className="text-sm text-stone-500 dark:text-stone-400">Single-location setup</p>
      </div>

      <nav className="flex flex-1 flex-col gap-2">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) =>
              `rounded-2xl px-4 py-3 text-sm font-medium transition ${
                isActive
                  ? 'bg-amber-100 text-amber-900 shadow-sm dark:bg-stone-800 dark:text-amber-200'
                  : 'text-stone-600 hover:bg-amber-50 hover:text-stone-800 dark:text-stone-300 dark:hover:bg-stone-900'
              }`
            }
          >
            {link.label}
          </NavLink>
        ))}
      </nav>

      <div className="rounded-2xl bg-gradient-to-br from-amber-600 to-orange-500 p-4 text-white shadow-lg dark:from-amber-500 dark:to-orange-400">
        <p className="text-xs uppercase tracking-[0.3em] text-white/70">Shift</p>
        <p className="text-lg font-semibold">Morning Flow</p>
        <div className="mt-3 flex items-center justify-between text-xs text-white/80">
          <span>Open tables: 8</span>
          <span>Orders: 14</span>
        </div>
      </div>
    </aside>
  )
}
