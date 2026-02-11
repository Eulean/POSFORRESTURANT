import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Login() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setError(null)
    setIsLoading(true)

    try {
      const response = await login(userName, password)
      const state = location.state as { from?: Location } | null
      const requested = state?.from?.pathname ?? '/'
      const adminOnly = ['/menu', '/inventory', '/reports', '/admin']
      const canAccessRequested =
        !adminOnly.includes(requested) || response.roles.includes('Admin')
      const fallback = response.roles.includes('Admin') ? '/' : '/orders'
      navigate(canAccessRequested ? requested : fallback, { replace: true })
    } catch (err) {
      setError('Login failed. Check username and password.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--canvas)] px-6">
      <div className="w-full max-w-md rounded-[32px] border border-amber-200/60 bg-white/80 p-8 shadow-lg backdrop-blur">
        <div className="flex items-center gap-3">
          <img src="/logo.png" alt="GALA taste" className="h-12 w-12 rounded-full object-cover" />
          <div>
            <p className="text-xs uppercase tracking-[0.3em] text-amber-600/80">Welcome Back</p>
            <h1 className="mt-1 text-3xl font-semibold text-stone-900">GALA taste</h1>
          </div>
        </div>
        <p className="mt-2 text-sm text-stone-500">Sign in to start service.</p>

        <form className="mt-6 space-y-4" onSubmit={handleSubmit}>
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Username</label>
            <input
              className="mt-2 w-full rounded-2xl border border-amber-200 bg-white px-4 py-3 text-sm text-stone-800"
              value={userName}
              onChange={(event) => setUserName(event.target.value)}
              placeholder="admin"
              required
            />
          </div>
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-stone-500">Password</label>
            <input
              type="password"
              className="mt-2 w-full rounded-2xl border border-amber-200 bg-white px-4 py-3 text-sm text-stone-800"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="********"
              required
            />
          </div>
          {error && (
            <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-2 text-xs text-rose-600">
              {error}
            </div>
          )}
          <button
            className="w-full rounded-2xl bg-[var(--accent)] px-4 py-3 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-70"
            disabled={isLoading}
          >
            {isLoading ? 'Signing in...' : 'Enter POS'}
          </button>
        </form>

        <div className="mt-6 rounded-2xl border border-amber-100 bg-amber-50/40 px-4 py-3 text-xs text-stone-600">
          Tip: Use your staff credentials. Admins can add roles in the Admin Console.
        </div>
      </div>
    </div>
  )
}
