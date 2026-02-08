import { useEffect, useState } from 'react'
import { fetchUsers, resetUserPassword, updateUserRole } from '../api/admin'
import { register } from '../api/auth'
import { useToast } from '../context/ToastContext'
import type { AppUser } from '../types/admin'
import type { RegisterRequest } from '../types/api'

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

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const data = await fetchUsers()
        if (active) setUsers(data)
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
                placeholder=""
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
                placeholder=""
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

      <div className="rounded-3xl border border-[var(--border)] bg-[var(--card)] p-6 shadow-sm">
        <h4 className="text-lg font-semibold text-[var(--ink)]">User Roles</h4>
        {isLoading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Loading</p>
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
