import { useEffect, useState } from 'react'
import axios from 'axios'
import { Car, Ticket, Clock, LogIn, LogOut as LogOutIcon } from 'lucide-react'

const API_BASE = 'http://localhost:5166/api'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')
const formatTime = (v) => new Date(v).toLocaleTimeString('vi-VN')

export default function Dashboard() {
  const [sessions, setSessions] = useState([])
  const [logs, setLogs] = useState([])
  const [plateIn, setPlateIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR')
  const [plateOut, setPlateOut] = useState('')
  const [checkoutInfo, setCheckoutInfo] = useState(null)
  const [loadingSessions, setLoadingSessions] = useState(false)
  const [checkingOut, setCheckingOut] = useState(false)
  const [paying, setPaying] = useState(false)

  const addLog = (msg) => setLogs((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev].slice(0, 60))

  const fetchSessions = async () => {
    setLoadingSessions(true)
    try {
      const res = await axios.get(`${API_BASE}/Report/active-sessions`)
      setSessions(res.data)
    } catch {
      // silent
    } finally {
      setLoadingSessions(false)
    }
  }

  useEffect(() => {
    fetchSessions()
    const interval = setInterval(fetchSessions, 3000)
    return () => clearInterval(interval)
  }, [])

  const handleCheckIn = async () => {
    if (!plateIn) return alert('Nhập biển số trước')
    try {
      const res = await axios.post(`${API_BASE}/CheckIn`, { plateNumber: plateIn, vehicleType: typeIn, gateId: 'GATE-01' })
      addLog(`✅ Check-in: ${plateIn} - Vé: ${res.data.ticketId}`)
      setPlateIn('')
      fetchSessions()
    } catch (err) {
      addLog('❌ ' + (err.response?.data?.error || 'Check-in lỗi'))
    }
  }

  const handleCheckOut = async () => {
    if (!plateOut) return alert('Nhập vé hoặc biển số')
    setCheckingOut(true)
    try {
      const res = await axios.post(`${API_BASE}/CheckOut`, { ticketIdOrPlate: plateOut, gateId: 'GATE-02' })
      setCheckoutInfo(res.data)
      addLog(`ℹ️ Xe ra: ${res.data.licensePlate} - Phí: ${formatCurrency(res.data.amount)}đ`)
    } catch (err) {
      addLog('❌ ' + (err.response?.data?.error || 'Không tìm thấy xe'))
    } finally {
      setCheckingOut(false)
    }
  }

  const handlePay = async () => {
    if (!checkoutInfo) return
    if (!window.confirm('Xác nhận thanh toán và mở cổng?')) return
    setPaying(true)
    try {
      await axios.post(`${API_BASE}/Payment`, { sessionId: checkoutInfo.sessionId, amount: checkoutInfo.amount })
      addLog('💰 Thanh toán thành công')
      setCheckoutInfo(null)
      setPlateOut('')
      fetchSessions()
    } catch {
      addLog('❌ Thanh toán thất bại')
    } finally {
      setPaying(false)
    }
  }

  return (
    <div className="grid lg:grid-cols-3 gap-6">
      {/* Left Column - Gates */}
      <div className="lg:col-span-1 space-y-6">
        {/* Check In Card */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div className="w-10 h-10 rounded-lg bg-green-100 flex items-center justify-center">
                <LogIn className="text-green-600" size={20} />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Cổng vào</h3>
                <p className="text-xs text-gray-500">Kiểm soát luồng xe vào</p>
              </div>
            </div>
            <span className="text-xs px-2 py-1 rounded-full bg-green-100 text-green-700 font-medium">Online</span>
          </div>
          <div className="space-y-3">
            <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
              <Car size={18} className="text-gray-400" />
              <input
                className="flex-1 bg-transparent outline-none text-sm"
                placeholder="Biển số xe..."
                value={plateIn}
                onChange={(e) => setPlateIn(e.target.value.toUpperCase())}
              />
            </div>
            <select
              className="w-full bg-gray-50 rounded-lg px-3 py-2 text-sm outline-none"
              value={typeIn}
              onChange={(e) => setTypeIn(e.target.value)}
            >
              <option value="CAR">🚗 Ô tô</option>
              <option value="MOTORBIKE">🛵 Xe máy</option>
              <option value="ELECTRIC_CAR">⚡ Xe điện</option>
            </select>
            <button
              onClick={handleCheckIn}
              className="w-full bg-green-600 hover:bg-green-700 text-white font-semibold py-2.5 rounded-lg transition"
            >
              Vào bến
            </button>
          </div>
        </div>

        {/* Check Out Card */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div className="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center">
                <LogOutIcon className="text-blue-600" size={20} />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Cổng ra</h3>
                <p className="text-xs text-gray-500">Kiểm tra & tính phí</p>
              </div>
            </div>
            <span className={`text-xs px-2 py-1 rounded-full font-medium ${checkingOut ? 'bg-amber-100 text-amber-700' : 'bg-blue-100 text-blue-700'}`}>
              {checkingOut ? 'Đang kiểm tra' : 'Sẵn sàng'}
            </span>
          </div>
          <div className="space-y-3">
            <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
              <Ticket size={18} className="text-gray-400" />
              <input
                className="flex-1 bg-transparent outline-none text-sm"
                placeholder="Nhập vé hoặc biển số..."
                value={plateOut}
                onChange={(e) => setPlateOut(e.target.value.toUpperCase())}
              />
            </div>
            <button
              onClick={handleCheckOut}
              disabled={checkingOut}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2.5 rounded-lg transition disabled:opacity-60"
            >
              {checkingOut ? 'Đang kiểm tra...' : 'Kiểm tra'}
            </button>
            {checkoutInfo && (
              <div className="mt-3 p-4 bg-blue-50 rounded-lg space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Biển số:</span>
                  <span className="font-bold text-gray-800">{checkoutInfo.licensePlate}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Phí gửi xe:</span>
                  <span className="font-bold text-red-600 text-lg">{formatCurrency(checkoutInfo.amount)} đ</span>
                </div>
                <button
                  onClick={handlePay}
                  disabled={paying}
                  className="w-full bg-amber-500 hover:bg-amber-600 text-white font-semibold py-2.5 rounded-lg transition mt-2 disabled:opacity-60"
                >
                  {paying ? 'Đang xử lý...' : '💳 Thanh toán & mở cổng'}
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Logs */}
        <div className="bg-gray-900 rounded-xl p-4 text-green-400 font-mono text-xs max-h-48 overflow-auto">
          <div className="flex items-center gap-2 mb-2 text-gray-400">
            <Clock size={14} /> Nhật ký hệ thống
          </div>
          {logs.length === 0 ? (
            <div className="text-gray-500">Chưa có log</div>
          ) : (
            logs.map((l, i) => <div key={i} className="py-0.5">{l}</div>)
          )}
        </div>
      </div>

      {/* Right Column - Sessions Table */}
      <div className="lg:col-span-2">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div className="w-10 h-10 rounded-lg bg-purple-100 flex items-center justify-center">
                <Car className="text-purple-600" size={20} />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Xe trong bãi</h3>
                <p className="text-xs text-gray-500">{sessions.length} xe đang gửi</p>
              </div>
            </div>
            {loadingSessions && <span className="text-xs text-gray-400">Đang tải...</span>}
          </div>
          <div className="overflow-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="pb-3 font-medium">Biển số</th>
                  <th className="pb-3 font-medium">Mã vé</th>
                  <th className="pb-3 font-medium">Giờ vào</th>
                  <th className="pb-3 font-medium">Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr key={s.sessionId} className="border-b border-gray-50 hover:bg-gray-50">
                    <td className="py-3 font-semibold text-gray-800">{s.vehicle?.licensePlate}</td>
                    <td className="py-3 font-mono text-gray-600">{s.ticket?.ticketId}</td>
                    <td className="py-3 text-gray-600">{formatTime(s.entryTime)}</td>
                    <td className="py-3">
                      <span className={`text-xs px-2 py-1 rounded-full font-medium ${s.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'}`}>
                        {s.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {sessions.length === 0 && (
                  <tr>
                    <td colSpan="4" className="py-8 text-center text-gray-400">
                      ✨ Bãi xe đang trống. Chờ xe vào để hiển thị.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}
