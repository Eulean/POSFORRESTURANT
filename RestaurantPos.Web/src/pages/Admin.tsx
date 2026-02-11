import { useEffect, useState } from 'react'
import { fetchUsers, resetUserPassword, updateUserRole } from '../api/admin'
import { register } from '../api/auth'
import { fetchShopProfile, updateShopProfile, uploadShopLogo } from '../api/shopProfile'
import { useToast } from '../context/ToastContext'
import type { AppUser } from '../types/admin'
import type { RegisterRequest, ShopProfileUpdateRequest } from '../types/api'

const roles = ['Admin', 'Waiter', 'Cashier']

const emptyUser: RegisterRequest = {
  userName: '',
  password: '',
  displayName: '',
  role: 'Waiter'
}

export default function Admin() {
  const { addToast } = useToast()
  const [users, setUsers] = useState<AppUser[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [newUser, setNewUser] = useState<RegisterRequest>(emptyUser)
  const [isCreating, setIsCreating] = useState(false)
  const [resetUserId, setResetUserId] = useState<string>('')
  const [resetPassword, setResetPassword] = useState('')
  const [isResetting, setIsResetting] = useState(false)
  const [shopProfile, setShopProfile] = useState<ShopProfileUpdateRequest>({
    name: 'GALA taste',
    address: '',
    phone: ''
  })
  const [isSavingProfile, setIsSavingProfile] = useState(false)
  const [logoUrl, setLogoUrl] = useState<string | null>(null)
  const [isUploadingLogo, setIsUploadingLogo] = useState(false)

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const [userData, profileData] = await Promise.all([
          fetchUsers(),
          fetchShopProfile()
        ])
        if (active) {
          setUsers(userData)
          setShopProfile({
            name: profileData.name ?? 'GALA taste',
            address: profileData.address ?? '',
            phone: profileData.phone ?? ''
          })
          setLogoUrl(profileData.logoUrl ?? '/logo.png')
        }
      } catch {
        if (active) setError('Unable to load users.')
      } finally {
        if (active) setIsLoading(false)
      }
    }

    load()
    return () => {
      active = false
    }
  }, [])

  const handleRoleChange = async (user: AppUser, role: string) => {
    await updateUserRole(user.id, role)
    setUsers((prev) =>
      prev.map((item) => (item.id === user.id ? { ...item, roles: [role] } : item))
    )
  }

  const handleCreateUser = async () => {
    if (!newUser.userName.trim() || !newUser.password.trim()) {
      setError('Username and password are required.')
      return
    }

    if (newUser.password.length < 6) {
      setError('Password must be at least 6 characters.')
      return
    }

    setIsCreating(true)
    try {
      await register({
        userName: newUser.userName.trim(),
        password: newUser.password,
        displayName: newUser.displayName?.trim() || null,
        role: newUser.role
      })
      const data = await fetchUsers()
      setUsers(data)
      setNewUser(emptyUser)
      setError(null)
      addToast('User created successfully.')
    } catch {
      setError('Unable to create user. Ensure you are Admin.')
    } finally {
      setIsCreating(false)
    }
  }

  const handleResetPassword = async () => {
    if (!resetUserId || !resetPassword.trim()) {
      setError('Select user and enter a new password.')
      return
    }

    if (resetPassword.length < 6) {
      setError('Password must be at least 6 characters.')
      return
    }

    setIsResetting(true)
    try {
      await resetUserPassword(resetUserId, resetPassword)
      setResetUserId('')
      setResetPassword('')
      setError(null)
      addToast('Password reset successfully.')
    } catch {
      setError('Unable to reset password.')
    } finally {
      setIsResetting(false)
    }
  }

  const handleSaveProfile = async () => {
    if (!shopProfile.name.trim()) {
      setError('Shop name is required.')
      return
    }

    setIsSavingProfile(true)
    try {
      const saved = await updateShopProfile({
        name: shopProfile.name.trim(),
        address: shopProfile.address.trim(),
        phone: shopProfile.phone.trim()
      })
      setShopProfile({
        name: saved.name,
        address: saved.address ?? '',
        phone: saved.phone ?? ''
      })
      setError(null)
      addToast('Shop location updated.')
    } catch {
      setError('Unable to update shop location.')
    } finally {
      setIsSavingProfile(false)
    }
  }

  const handleLogoUpload = async (file?: File | null) => {
    if (!file) return
    if (file.type !== 'image/png') {
      setError('Logo must be a PNG file.')
      return
    }

    setIsUploadingLogo(true)
    try {
      const saved = await uploadShopLogo(file)
      setLogoUrl(saved.logoUrl ?? `/logo.png?v=${Date.now()}`)
      setError(null)
      addToast('Logo updated.')
    } catch {
      setError('Unable to upload logo.')
    } finally {
      setIsUploadingLogo(false)
    }
  }

  return (
    <div className="flex flex-col gap-6 rise-in">
      <div>
        <h3 className="text-2xl font-semibold text-[var(--ink)]">Admin Console</h3>
        <p className="text-sm text-[var(--muted)]">Manage users and roles.</p>
      </div>

      {error && (
        <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-4 text-sm text-rose-600">
          {error}
        </div>
      )}

      <section className="grid gap-6 lg:grid-cols-[1.1fr_1fr]">
        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-[var(--ink)]">Create User</h4>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Username</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={newUser.userName}
                onChange={(event) => setNewUser((prev) => ({ ...prev, userName: event.target.value }))}
                placeholder="cashier1"
              />
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Display Name</label>
              <input
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={newUser.displayName ?? ''}
                onChange={(event) => setNewUser((prev) => ({ ...prev, displayName: event.target.value }))}
                placeholder="Cashier One"
              />
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Password</label>
              <input
                type="password"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={newUser.password}
                onChange={(event) => setNewUser((prev) => ({ ...prev, password: event.target.value }))}
                placeholder="********"
              />
              <p className="mt-2 text-xs text-[var(--muted)]">
                Password must be at least 6 characters. Use a mix of letters and numbers for best security.
              </p>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Role</label>
              <select
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={newUser.role}
                onChange={(event) => setNewUser((prev) => ({ ...prev, role: event.target.value }))}
              >
                {roles.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </div>
            <button
              className="btn btn-primary btn-soft btn-md"
              onClick={handleCreateUser}
              disabled={isCreating}
            >
              {isCreating ? 'Creating...' : 'Create User'}
            </button>
          </div>
        </div>

        <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
          <h4 className="text-lg font-semibold text-[var(--ink)]">Reset Password</h4>
          <div className="mt-4 space-y-4">
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">User</label>
              <select
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={resetUserId}
                onChange={(event) => setResetUserId(event.target.value)}
              >
                <option value="">Select user</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.displayName ?? user.userName}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">New Password</label>
              <input
                type="password"
                className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
                value={resetPassword}
                onChange={(event) => setResetPassword(event.target.value)}
                placeholder="********"
              />
              <p className="mt-2 text-xs text-[var(--muted)]">Minimum 6 characters.</p>
            </div>
            <button
              className="btn btn-primary btn-soft btn-md"
              onClick={handleResetPassword}
              disabled={isResetting}
            >
              {isResetting ? 'Resetting...' : 'Reset Password'}
            </button>
          </div>
        </div>
      </section>

      <section className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
        <div>
          <h4 className="text-lg font-semibold text-[var(--ink)]">Shop Location</h4>
          <p className="text-sm text-[var(--muted)]">Used on receipts and printouts.</p>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Shop Name</label>
            <input
              className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
              value={shopProfile.name}
              onChange={(event) => setShopProfile((prev) => ({ ...prev, name: event.target.value }))}
              placeholder="GALA taste"
            />
          </div>
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Logo (PNG)</label>
            <div className="mt-2 flex items-center gap-3">
              <div className="h-14 w-14 overflow-hidden rounded-2xl border border-[var(--border)] bg-[var(--surface)]">
                <img
                  src={logoUrl ?? '/logo.png'}
                  alt="Logo"
                  className="h-full w-full object-cover"
                />
              </div>
              <label className="btn btn-ghost btn-pill btn-xs cursor-pointer">
                {isUploadingLogo ? 'Uploading...' : 'Upload Logo'}
                <input
                  type="file"
                  accept="image/png"
                  className="hidden"
                  onChange={(event) => handleLogoUpload(event.target.files?.[0])}
                  disabled={isUploadingLogo}
                />
              </label>
            </div>
          </div>
          <div>
            <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Phone</label>
            <input
              className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
              value={shopProfile.phone}
              onChange={(event) => setShopProfile((prev) => ({ ...prev, phone: event.target.value }))}
              placeholder="(000) 000-0000"
            />
          </div>
          <div className="md:col-span-2">
            <label className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">Address</label>
            <textarea
              className="mt-2 w-full rounded-2xl border border-[var(--border)] bg-[var(--card)] px-4 py-3 text-sm text-[var(--ink)] focus:border-[var(--accent)] focus:outline-none "
              rows={2}
              value={shopProfile.address}
              onChange={(event) => setShopProfile((prev) => ({ ...prev, address: event.target.value }))}
              placeholder="123 Main Street, City"
            />
          </div>
        </div>
        <button
          className="mt-4 btn btn-primary btn-soft btn-md"
          onClick={handleSaveProfile}
          disabled={isSavingProfile}
        >
          {isSavingProfile ? 'Saving...' : 'Save Location'}
        </button>
      </section>

      <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
        <h4 className="text-lg font-semibold text-[var(--ink)]">User Roles</h4>
        {isLoading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Loading...</p>
        ) : (
          <div className="mt-4 space-y-3">
            {users.map((user) => (
              <div key={user.id} className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-[var(--border)] bg-[var(--surface)] px-4 py-3">
                <div>
                  <p className="text-sm font-semibold text-[var(--ink)]">{user.displayName ?? user.userName}</p>
                  <p className="text-xs text-[var(--muted)]">@{user.userName}</p>
                  <div className="mt-2 flex flex-wrap gap-2">
                    {user.roles.map((role) => (
                      <span
                        key={role}
                        className="btn btn-ghost btn-pill btn-xs"
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                  {roles.map((role) => (
                    <button
                      key={role}
                      className={`btn btn-ghost btn-pill btn-xs ${
                        user.roles.includes(role)
                          ? 'border-[var(--accent-strong)] bg-[var(--accent)] text-white'
                          : 'border-[var(--border)] text-[var(--accent)]'
                      }`}
                      onClick={() => handleRoleChange(user, role)}
                    >
                      {role}
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

