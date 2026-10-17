import { useEffect, useState } from 'react'
import QRCode from 'qrcode'
import { api } from '../api/client'

type HostInfo = { addresses: string[] }

function AddressCard({ address }: { address: string }) {
  const [qr, setQr] = useState('')
  useEffect(() => {
    QRCode.toDataURL(address, { width: 220, margin: 2 })
      .then(setQr)
      .catch(() => setQr(''))
  }, [address])

  return (
    <div className="flex flex-col items-center gap-4 rounded-2xl border border-[var(--border)] bg-[var(--card)] p-6 text-center shadow-sm">
      {qr && <img src={qr} alt={`QR code for ${address}`} className="h-44 w-44" />}
      <div>
        <p className="text-xs uppercase tracking-widest text-[var(--muted)]">Open on another device</p>
        <p className="mt-2 break-all text-lg font-semibold text-[var(--ink)]">{address}</p>
      </div>
      <button className="rounded-xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white" onClick={() => navigator.clipboard.writeText(address)}>
        Copy address
      </button>
    </div>
  )
}

export default function Host() {
  const [addresses, setAddresses] = useState<string[]>([])
  const [error, setError] = useState('')
  const [backupPath, setBackupPath] = useState('')
  const [backingUp, setBackingUp] = useState(false)

  const refresh = () => {
    api.get<HostInfo>('/api/host/info')
      .then(({ data }) => { setAddresses(data.addresses); setError('') })
      .catch(() => setError('Could not read this laptop’s network addresses.'))
  }

  useEffect(refresh, [])

  const createBackup = async () => {
    setBackingUp(true)
    try {
      const { data } = await api.post<{ path: string }>('/api/host/backup')
      setBackupPath(data.path)
    } catch {
      setError('Backup failed. Check the host laptop and try again.')
    } finally {
      setBackingUp(false)
    }
  }

  return (
    <section className="space-y-6 text-[var(--ink)]">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.25em] text-[var(--accent)]">Local network</p>
        <h2 className="mt-2 text-3xl font-semibold">Connect phones and tablets</h2>
        <p className="mt-2 max-w-2xl text-[var(--muted)]">Keep this laptop on and connect every device to the same private Wi-Fi. Scan a code or type an address below, then sign in with a staff account.</p>
      </div>
      {error && <p className="rounded-xl bg-[var(--danger-bg)] p-4 text-[var(--danger)]">{error}</p>}
      {!error && addresses.length === 0 && <p className="rounded-xl bg-[var(--surface)] p-4">No private network address found. Connect this laptop to the restaurant Wi-Fi, then refresh.</p>}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {addresses.map(address => <AddressCard key={address} address={address} />)}
      </div>
      <button onClick={refresh} className="rounded-xl border border-[var(--border)] px-4 py-2 text-sm font-semibold">Refresh addresses</button>
      <div className="rounded-2xl border border-[var(--border)] bg-[var(--card)] p-5">
        <h3 className="text-lg font-semibold">Protect your data</h3>
        <p className="mt-1 text-sm text-[var(--muted)]">A database and photo backup is created on the laptop when hosting starts and every 24 hours. Copy backup files to a USB drive regularly.</p>
        <button onClick={createBackup} disabled={backingUp} className="mt-4 rounded-xl bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60">{backingUp ? 'Creating backup…' : 'Create backup now'}</button>
        {backupPath && <p className="mt-3 break-all text-sm text-[var(--muted)]">Saved on host laptop: {backupPath}</p>}
      </div>
      <p className="text-sm text-[var(--muted)]">No internet connection is required. Do not use guest Wi-Fi with device isolation; devices on that network may be unable to reach the laptop.</p>
    </section>
  )
}
