import { useToast } from '../context/ToastContext'

export default function Toasts() {
  const { toasts, removeToast } = useToast()

  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col gap-3">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className="flex items-center justify-between gap-3 rounded-2xl border border-amber-200 bg-white px-4 py-3 text-sm text-stone-700 shadow-lg"
        >
          <span>{toast.message}</span>
          <button
            className="text-xs uppercase tracking-[0.2em] text-amber-600"
            onClick={() => removeToast(toast.id)}
          >
            Close
          </button>
        </div>
      ))}
    </div>
  )
}
