export default function NotAuthorized() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
      <p className="text-xs uppercase tracking-[0.3em] text-amber-600/80">Restricted</p>
      <h2 className="text-3xl font-semibold text-stone-900">Not authorized</h2>
      <p className="max-w-md text-sm text-stone-500">
        You do not have permission to access this area. Please contact an administrator if you believe this is a mistake.
      </p>
    </div>
  )
}
