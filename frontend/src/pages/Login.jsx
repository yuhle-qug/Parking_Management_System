import { useState } from 'react'
import axios from 'axios'
import { API_BASE } from '../config/api'

export default function Login({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async () => {
    const uname = username.trim()
    const pwd = password.trim()

    if (!uname || !pwd) {
      setError('Vui lòng nhập đủ thông tin')
      return
    }
    setError('')
    setLoading(true)
    try {
      const res = await axios.post(`${API_BASE}/UserAccount/login`, { username: uname, password: pwd })
      localStorage.setItem('user', JSON.stringify(res.data))
      onLogin(res.data)
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
          <div className="text-3xl">🅿️</div>
          <h2 className="text-2xl font-bold text-gray-800">Đăng nhập</h2>
          <p className="text-sm text-gray-500">Truy cập hệ thống Parking Pro</p>
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
