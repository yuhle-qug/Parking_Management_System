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
        <div className="flex items-center gap-3 text-xl font-bold">
          <img src="/logo.png" alt="SmartPark" className="h-10 w-auto" />
          <span className="text-gray-800">SmartPark</span>
        </div>
        <div className="flex items-center gap-4 text-sm">
          <span className="text-gray-600">Xin chào, <b>{user?.username}</b> ({user?.role})</span>
          {user?.gateId && (
            <span className="text-xs px-2 py-1 rounded-full bg-blue-50 text-blue-700 font-semibold border border-blue-100">Gate: {user.gateId}</span>
          )}
          {user?.gateVehicleGroup && (
            <span className="text-xs px-2 py-1 rounded-full bg-emerald-50 text-emerald-700 font-semibold border border-emerald-100">
              Luồng: {user.gateVehicleGroup === 'CAR' ? 'Ô tô' : 'Xe máy'}
            </span>
          )}
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
          <Outlet context={{ user }} />
        </main>
      </div>
    </div>
  )
}
