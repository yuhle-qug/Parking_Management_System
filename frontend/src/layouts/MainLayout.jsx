import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { LayoutDashboard, CreditCard, BarChart3, Shield, LogOut } from 'lucide-react'

export default function MainLayout({ user, onLogout }) {
  const location = useLocation()
  const navigate = useNavigate()

  const handleLogout = () => {
    onLogout()
    navigate('/')
  }

  const isAdmin = user?.role?.toUpperCase() === 'ADMIN'
  const menuItems = [
    { path: '/dashboard', label: 'Dashboard', icon: <LayoutDashboard size={20} /> },
    { path: '/membership', label: 'Vé Tháng', icon: <CreditCard size={20} /> },
    { path: '/report', label: 'Báo Cáo', icon: <BarChart3 size={20} /> },
    ...(isAdmin
      ? [{ path: '/admin', label: 'Quản Trị', icon: <Shield size={20} /> }]
      : []),
  ]

  return (
    <div className="min-h-screen flex flex-col bg-gray-100 text-gray-900">
      <header className="bg-white shadow flex items-center justify-between px-6 py-4">
        <div className="flex items-center gap-2 text-xl font-bold">
          <span className="text-blue-600">🅿️</span> Parking Pro
        </div>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-gray-600">Xin chào, <b>{user?.username}</b> ({user?.role})</span>
          <button
            onClick={handleLogout}
            className="flex items-center gap-1 text-red-600 hover:bg-red-50 px-3 py-2 rounded-md"
          >
            <LogOut size={18} /> Thoát
          </button>
        </div>
      </header>

      <div className="flex flex-1">
        <aside className="w-64 bg-white border-r hidden md:block">
          <nav className="p-4 space-y-2">
            {menuItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 p-3 rounded-lg transition-colors ${
                  location.pathname === item.path
                    ? 'bg-blue-50 text-blue-600 font-semibold'
                    : 'text-gray-600 hover:bg-gray-50'
                }`}
              >
                {item.icon} {item.label}
              </Link>
            ))}
          </nav>
        </aside>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
