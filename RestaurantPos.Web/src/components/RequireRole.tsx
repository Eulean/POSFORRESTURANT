import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

type Props = {
  allowed: string[]
}

export default function RequireRole({ allowed }: Props) {
  const { roles, token } = useAuth()
  const hasAccess = roles.some((role) => allowed.includes(role))

  if (!token) {
    return <Navigate to="/login" replace />
  }

  if (!hasAccess) {
    return <Navigate to="/not-authorized" replace />
  }

  return <Outlet />
}
