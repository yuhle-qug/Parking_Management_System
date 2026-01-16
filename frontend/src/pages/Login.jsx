import { useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import { API_BASE } from '../config/api'
import { ENTRY_GATES, VEHICLE_GROUPS } from '../config/gates'

export default function Login({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [gateId, setGateId] = useState(ENTRY_GATES[0]?.id || '')
  const [gateVehicleGroup, setGateVehicleGroup] = useState(ENTRY_GATES[0]?.vehicleGroup || VEHICLE_GROUPS[0]?.key || 'CAR')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const gatesByGroup = useMemo(
    () => ENTRY_GATES.filter((g) => !gateVehicleGroup || g.vehicleGroup === gateVehicleGroup),
    [gateVehicleGroup]
  )

  useEffect(() => {
    if (!gatesByGroup.find((g) => g.id === gateId)) {
      setGateId(gatesByGroup[0]?.id || '')
    }
  }, [gateVehicleGroup, gatesByGroup, gateId])

  const handleSubmit = async () => {
    const uname = username.trim()
    const pwd = password.trim()

    if (!uname || !pwd || !gateId) {
      setError('Vui lòng nhập đủ thông tin và chọn cổng làm việc')
      return
    }
    setError('')
    setLoading(true)
    try {
      const res = await axios.post(`${API_BASE}/UserAccount/login`, { username: uname, password: pwd })
      const userWithGate = { ...res.data, gateId, gateVehicleGroup }
      localStorage.setItem('user', JSON.stringify(userWithGate))
      onLogin(userWithGate)
    } catch (err) {
      const apiMessage = err.response?.data?.message || err.response?.data?.Message
      setError('Đăng nhập thất bại: ' + (apiMessage || err.message))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4">
      <div className="bg-white w-full max-w-md shadow-lg rounded-xl p-8 space-y-6">
        <div className="text-center space-y-2">
          <div className="flex justify-center">
            <img src="/logo.png" alt="SmartPark" className="h-16 w-auto" />
          </div>
          <h2 className="text-2xl font-bold text-gray-800">Đăng nhập</h2>
          <p className="text-sm text-gray-500">Truy cập hệ thống SmartPark</p>
        </div>
        {error && <div className="text-red-600 text-sm bg-red-50 border border-red-100 rounded p-2">{error}</div>}
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-1">Tài khoản</label>
            <input
              className="w-full border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="admin"
            />
          </div>
          <div>
            <label className="block text-sm font-semibold text-gray-700 mb-1">Mật khẩu</label>
            <input
              type="password"
              className="w-full border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="123"
            />
          </div>
          <div className="space-y-2">
            <label className="block text-sm font-semibold text-gray-700">Luồng cổng đang vận hành</label>
            <div className="grid grid-cols-2 gap-2">
              {VEHICLE_GROUPS.map((group) => (
                <button
                  key={group.key}
                  type="button"
                  onClick={() => setGateVehicleGroup(group.key)}
                  className={`flex items-center justify-center gap-2 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                    gateVehicleGroup === group.key ? 'border-blue-500 bg-blue-50 text-blue-700' : 'border-gray-200 bg-gray-50 text-gray-700'
                  }`}
                >
                  {group.label}
                </button>
              ))}
            </div>
            <p className="text-xs text-gray-500">Cố định luồng ngay từ đăng nhập: cổng dành cho ô tô hoặc xe máy.</p>
          </div>

          <div className="space-y-2">
            <label className="block text-sm font-semibold text-gray-700">Chọn cổng đang vận hành</label>
            <div className="grid grid-cols-2 gap-2">
              {gatesByGroup.map((g) => (
                <button
                  key={g.id}
                  type="button"
                  onClick={() => setGateId(g.id)}
                  className={`rounded-lg border px-3 py-2 text-sm font-semibold transition text-left ${
                    gateId === g.id ? 'border-blue-500 bg-blue-50 text-blue-700 shadow-sm' : 'border-gray-200 bg-gray-50 text-gray-700'
                  }`}
                >
                  <div>{g.label}</div>
                  <div className="text-xs font-normal text-gray-500">ID: {g.id}</div>
                </button>
              ))}
            </div>
          </div>
          <button
            onClick={handleSubmit}
            disabled={loading}
            className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 rounded-lg transition disabled:opacity-60"
          >
            {loading ? 'Đang xử lý...' : 'Đăng nhập'}
          </button>
        </div>
      </div>
    </div>
  )
}
