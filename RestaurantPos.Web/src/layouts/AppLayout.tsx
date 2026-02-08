import { Outlet } from 'react-router-dom'
import Sidebar from '../components/Sidebar'
import TopBar from '../components/TopBar'

export default function AppLayout() {
  return (
    <div className="min-h-screen bg-[var(--canvas)]">
      <div className="grid min-h-screen grid-cols-1 lg:grid-cols-[280px_1fr]">
        <Sidebar />
        <main className="flex flex-col gap-6 px-6 py-8">
          <TopBar />
          <div className="flex-1">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
