import { Navigate, Route, Routes } from 'react-router-dom'
import RequireAuth from './components/RequireAuth'
import RequireRole from './components/RequireRole'
import AppLayout from './layouts/AppLayout'
import Admin from './pages/Admin'
import Dashboard from './pages/Dashboard'
import Inventory from './pages/Inventory'
import Kitchen from './pages/Kitchen'
import Login from './pages/Login'
import Menu from './pages/Menu'
import Orders from './pages/Orders'
import Tables from './pages/Tables'
import NotAuthorized from './pages/NotAuthorized'
import Reports from './pages/Reports'
import { useLoading } from './context/LoadingContext'

export default function App() {
  const { activeCount } = useLoading()

  return (
    <>
      {activeCount > 0 && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-stone-900/20">
          <div className="rounded-full border-4 border-amber-300/70 border-t-transparent p-4 animate-spin" />
        </div>
      )}
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/not-authorized" element={<NotAuthorized />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route index element={<Dashboard />} />
            <Route path="/orders" element={<Orders />} />
            <Route path="/tables" element={<Tables />} />
            <Route path="/kitchen" element={<Kitchen />} />
            <Route element={<RequireRole allowed={['Admin']} />}>
              <Route path="/menu" element={<Menu />} />
              <Route path="/inventory" element={<Inventory />} />
              <Route path="/reports" element={<Reports />} />
              <Route path="/admin" element={<Admin />} />
            </Route>
          </Route>
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </>
  )
}
