import { NavLink } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const allLinks = [
  { to: "/", label: "Overview", roles: ["Admin", "Waiter", "Cashier"] },
  { to: "/orders", label: "Orders", roles: ["Admin", "Waiter", "Cashier"] },
  { to: "/kitchen", label: "Kitchen", roles: ["Admin", "Waiter", "Cashier"] },
  { to: "/menu", label: "Menu", roles: ["Admin"] },
  { to: "/tables", label: "Tables", roles: ["Admin", "Waiter"] },
  { to: "/inventory", label: "Inventory", roles: ["Admin"] },
  { to: "/reports", label: "Reports", roles: ["Admin"] },
  { to: "/admin", label: "Admin", roles: ["Admin"] },
];

export default function Sidebar() {
  const { roles } = useAuth();
  const links = allLinks.filter((link) =>
    link.roles.some((role) => roles.includes(role)),
  );

  return (
    <aside className="flex h-full flex-col gap-6 border-r p-6 backdrop-blur bg-[var(--card)]/95 border-[var(--border)]">
      <div className="flex items-center gap-3">
        <img
          src="/logo.png"
          alt="GALA taste"
          className="h-12 w-12 rounded-full object-cover"
        />
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-[var(--accent)]/85">
            Restaurant POS
          </p>
          <h1 className="text-2xl font-semibold text-[var(--ink)]">
            GALA taste
          </h1>
          {/* <p className="text-sm text-stone-500 dark:text-stone-400">Single-location setup</p> */}
        </div>
      </div>

      <nav className="flex flex-1 flex-col gap-2">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) =>
              `rounded-2xl border px-4 py-3 text-sm font-semibold transition ${
                isActive
                  ? "border-[var(--accent)] bg-[var(--accent)]/15 text-[var(--ink)] shadow-sm"
                  : "border-[var(--border)] bg-[var(--surface)] text-[var(--muted)] hover:border-[var(--accent)]/70 hover:text-[var(--ink)]"
              }`
            }
          >
            {link.label}
          </NavLink>
        ))}
      </nav>

      <div className="rounded-2xl bg-gradient-to-br from-[var(--accent)] to-[var(--accent-strong)] p-4 text-white shadow-lg">
        <p className="text-xs uppercase tracking-[0.3em] text-white/70">
          Shift
        </p>
        <p className="text-lg font-semibold">Morning Flow</p>
        <div className="mt-3 flex items-center justify-between text-xs text-white/80">
          <span>Open tables: 8</span>
          <span>Orders: 14</span>
        </div>
      </div>
    </aside>
  );
}
