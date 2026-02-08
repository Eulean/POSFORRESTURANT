import { useNavigate } from 'react-router-dom'
import { useSession } from '../context/SessionContext'

export default function SessionExpiredModal() {
  const { sessionExpired, setSessionExpired } = useSession()
  const navigate = useNavigate()

  if (!sessionExpired) return null

  const handleRelog = () => {
    setSessionExpired(false)
    navigate('/login')
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
        <p className="text-xs uppercase tracking-[0.3em] text-amber-600/80">Session Ended</p>
        <h4 className="mt-2 text-2xl font-semibold text-stone-900">Session expired</h4>
        <p className="mt-2 text-sm text-stone-500">
          Your session has expired. Please sign in again to continue.
        </p>
        <div className="mt-6 flex gap-2">
          <button
            className="flex-1 rounded-2xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-[var(--accent-strong)]"
            onClick={handleRelog}
          >
            Re-login
          </button>
        </div>
      </div>
    </div>
  )
}
