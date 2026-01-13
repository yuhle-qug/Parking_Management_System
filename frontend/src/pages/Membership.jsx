import { useEffect, useState } from 'react'
import axios from 'axios'
import { CreditCard, User, Car, Calendar, CheckCircle, Clock, Trash2 } from 'lucide-react'
import { API_BASE } from '../config/api'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')

const normalizeDotNetIso = (value) => {
  if (!value || typeof value !== 'string') return value
  // .NET can emit 7 fractional digits: 2026-01-27T... .3340684+07:00
  // Some browsers parse only up to milliseconds reliably.
  return value.replace(/\.(\d{3})\d+(?=([zZ]|[+-]\d{2}:\d{2})$)/, '.$1')
}

const parseDate = (value) => {
  if (!value) return null
  if (value instanceof Date) return isNaN(value.getTime()) ? null : value
  const normalized = normalizeDotNetIso(value)
  const d = new Date(normalized)
  return isNaN(d.getTime()) ? null : d
}

const formatDate = (v) => {
  const d = parseDate(v)
  return d ? d.toLocaleDateString('vi-VN') : ''
}

export default function Membership() {
  const [tickets, setTickets] = useState([])
  const [policies, setPolicies] = useState([])
  const [loading, setLoading] = useState(false)
  const [form, setForm] = useState({
    ownerName: '',
    phone: '',
    licensePlate: '',
    vehicleType: 'CAR',
    months: 1,
    policyId: ''
  })

  const defaultPolicies = [
    { policyId: 'P-CAR', policyName: 'Vé tháng Ô tô', monthlyPrice: 1500000, vehicleType: 'CAR' },
    { policyId: 'P-MOTO', policyName: 'Vé tháng Xe máy', monthlyPrice: 120000, vehicleType: 'MOTORBIKE' },
    { policyId: 'P-ELEC', policyName: 'Vé tháng Xe điện', monthlyPrice: 1000000, vehicleType: 'ELECTRIC_CAR' }
  ]

  const fetchData = async () => {
    setLoading(true)

    try {
      const [polRes, ticketRes] = await Promise.all([
        axios.get(`${API_BASE}/Membership/policies`),
        axios.get(`${API_BASE}/Membership/tickets`)
      ])

      const pol = Array.isArray(polRes.data) ? polRes.data : []
      const mappedPolicies = pol.map(p => ({
        policyId: p.policyId ?? p.PolicyId,
        policyName: p.policyName ?? p.Name,
        monthlyPrice: p.monthlyPrice ?? p.MonthlyPrice ?? 0,
        vehicleType: (p.vehicleType ?? p.VehicleType ?? '').toString()
      })).filter(p => !!p.policyId)

      const finalPolicies = mappedPolicies.length ? mappedPolicies : defaultPolicies
      setPolicies(finalPolicies)
      if (!form.policyId && finalPolicies[0]?.policyId) {
        setForm(prev => ({ ...prev, policyId: finalPolicies[0].policyId }))
      }

      const rawTickets = Array.isArray(ticketRes.data) ? ticketRes.data : []
      const now = new Date()
      const mappedTickets = rawTickets.map(t => {
        const ticketId = t.ticketId ?? t.TicketId
        const startDate = t.startDate ?? t.StartDate
        const endDate = t.endDate ?? t.ExpiryDate
        const status = (t.status ?? t.Status ?? '').toString()
        const end = parseDate(endDate)
        const isActive = status.trim().toLowerCase() === 'active' && !!end && end >= now

        return {
          ticketId,
          ownerName: t.ownerName ?? t.OwnerName ?? t.customerName ?? t.CustomerName ?? (t.customerId ?? t.CustomerId ?? '').toString(),
          phone: t.phone ?? t.Phone ?? '',
          licensePlate: t.licensePlate ?? t.VehiclePlate ?? '',
          startDate,
          endDate,
          isActive,
          vehicleType: t.vehicleType ?? t.VehicleType ?? '',
          monthlyFee: t.monthlyFee ?? t.MonthlyFee ?? 0,
          status
        }
      }).filter(t => !!t.ticketId)

      // Chỉ giữ các vé còn hiệu lực để hiển thị
      setTickets(mappedTickets.filter(t => t.isActive))
    } catch (err) {
      // fallback: at least show something (no localStorage anymore)
      setPolicies(defaultPolicies)
      if (!form.policyId) {
        setForm(prev => ({ ...prev, policyId: defaultPolicies[0].policyId }))
      }
      setTickets([])
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchData() }, [])

  const selectedPolicy = policies.find(p => p.policyId === form.policyId)
  const estimatedPrice = selectedPolicy ? selectedPolicy.monthlyPrice * form.months : 0

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.ownerName || !form.licensePlate || !form.policyId) {
      return alert('Vui lòng điền đầy đủ thông tin')
    }
    try {
      // Gọi API backend: name, phone, identityNumber, plateNumber, vehicleType
      const res = await axios.post(`${API_BASE}/Membership/register`, {
        name: form.ownerName,
        phone: form.phone,
        identityNumber: '',
        plateNumber: form.licensePlate.toUpperCase(),
        vehicleType: form.vehicleType
      })

      alert('Đăng ký thành công!')
      setForm({ ownerName: '', phone: '', licensePlate: '', vehicleType: 'CAR', months: 1, policyId: policies[0]?.policyId || '' })
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Lỗi đăng ký')
    }
  }

  const handleCancel = async (ticketId) => {
    if (!window.confirm('Bạn có chắc muốn hủy vé này?')) return

    try {
      await axios.delete(`${API_BASE}/Membership/tickets/${ticketId}`)
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Backend chưa hỗ trợ hủy vé tháng')
    }
  }

  return (
    <div className="grid lg:grid-cols-3 gap-6">
      {/* Registration Form */}
      <div className="lg:col-span-1">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-12 h-12 rounded-xl bg-indigo-100 flex items-center justify-center">
              <CreditCard className="text-indigo-600" size={24} />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-800">Đăng ký vé tháng</h2>
              <p className="text-xs text-gray-500">Tiết kiệm chi phí gửi xe hàng ngày</p>
            </div>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Họ tên chủ xe</label>
              <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
                <User size={16} className="text-gray-400" />
                <input
                  type="text"
                  className="flex-1 bg-transparent outline-none text-sm"
                  placeholder="Nguyễn Văn A"
                  value={form.ownerName}
                  onChange={(e) => setForm({ ...form, ownerName: e.target.value })}
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Số điện thoại</label>
              <input
                type="tel"
                className="w-full bg-gray-50 rounded-lg px-3 py-2 text-sm outline-none"
                placeholder="0901234567"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Biển số xe</label>
              <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
                <Car size={16} className="text-gray-400" />
                <input
                  type="text"
                  className="flex-1 bg-transparent outline-none text-sm uppercase"
                  placeholder="30A-12345"
                  value={form.licensePlate}
                  onChange={(e) => setForm({ ...form, licensePlate: e.target.value.toUpperCase() })}
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Loại xe</label>
              <select
                className="w-full bg-gray-50 rounded-lg px-3 py-2.5 text-sm outline-none"
                value={form.vehicleType}
                onChange={(e) => setForm({ ...form, vehicleType: e.target.value })}
              >
                <option value="CAR">🚗 Ô tô</option>
                <option value="MOTORBIKE">🛵 Xe máy</option>
                <option value="ELECTRIC_CAR">⚡ Xe điện</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Gói đăng ký</label>
              <select
                className="w-full bg-gray-50 rounded-lg px-3 py-2.5 text-sm outline-none"
                value={form.policyId}
                onChange={(e) => setForm({ ...form, policyId: e.target.value })}
              >
                {policies.map(p => (
                  <option key={p.policyId} value={p.policyId}>
                    {p.policyName} - {formatCurrency(p.monthlyPrice)}đ/tháng
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-1">Số tháng</label>
              <select
                className="w-full bg-gray-50 rounded-lg px-3 py-2.5 text-sm outline-none"
                value={form.months}
                onChange={(e) => setForm({ ...form, months: parseInt(e.target.value) })}
              >
                <option value={1}>1 tháng</option>
                <option value={3}>3 tháng (tiết kiệm 5%)</option>
                <option value={6}>6 tháng (tiết kiệm 10%)</option>
                <option value={12}>12 tháng (tiết kiệm 15%)</option>
              </select>
            </div>

            <div className="bg-indigo-50 rounded-lg p-4">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Tổng thanh toán:</span>
                <span className="text-xl font-bold text-indigo-600">{formatCurrency(estimatedPrice)} đ</span>
              </div>
            </div>

            <button
              type="submit"
              className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-3 rounded-lg transition"
            >
              Đăng ký ngay
            </button>
          </form>
        </div>
      </div>

      {/* Active Tickets */}
      <div className="lg:col-span-2">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <div className="flex items-center justify-between mb-6">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center">
                <CheckCircle className="text-green-600" size={24} />
              </div>
              <div>
                <h2 className="text-lg font-bold text-gray-800">Vé tháng đang hoạt động</h2>
                <p className="text-xs text-gray-500">{tickets.filter(t => t.isActive).length} vé còn hiệu lực</p>
              </div>
            </div>
            {loading && <span className="text-xs text-gray-400">Đang tải...</span>}
          </div>

          {tickets.length === 0 ? (
            <div className="text-center py-12 text-gray-400">
              <CreditCard size={48} className="mx-auto mb-3 opacity-30" />
              <p>Chưa có vé tháng nào được đăng ký</p>
            </div>
          ) : (
            <div className="grid md:grid-cols-2 gap-4">
              {tickets.map(ticket => (
                <div
                  key={ticket.ticketId}
                  className={`p-4 rounded-xl border ${ticket.isActive ? 'bg-green-50 border-green-200' : 'bg-gray-50 border-gray-200 opacity-60'}`}
                >
                  <div className="flex items-start justify-between mb-3">
                    <div>
                      <p className="font-bold text-gray-800">{ticket.ownerName}</p>
                      <p className="text-sm text-gray-500">{ticket.phone}</p>
                    </div>
                    <span className={`text-xs px-2 py-1 rounded-full font-medium ${ticket.isActive ? 'bg-green-200 text-green-700' : 'bg-gray-300 text-gray-600'}`}>
                      {ticket.isActive ? 'Hoạt động' : 'Hết hạn'}
                    </span>
                  </div>
                  <div className="space-y-2 text-sm">
                    <div className="flex items-center gap-2 text-gray-600">
                      <Car size={14} />
                      <span className="font-mono font-semibold">{ticket.licensePlate}</span>
                    </div>
                    <div className="flex items-center gap-2 text-gray-600">
                      <Calendar size={14} />
                      <span>{formatDate(ticket.startDate)} - {formatDate(ticket.endDate)}</span>
                    </div>
                    <div className="flex items-center gap-2 text-gray-600">
                      <Clock size={14} />

                      <span>
                        Còn {Math.max(0, Math.ceil(((parseDate(ticket.endDate)?.getTime() ?? 0) - Date.now()) / (1000 * 60 * 60 * 24)))} ngày
                      </span>
                    </div>
                      <div className="flex items-center justify-between text-gray-600">
                        <span className="text-xs">Phí/tháng:</span>
                        <span className="text-xs font-semibold">{formatCurrency(ticket.monthlyFee)} đ</span>
                      </div>
                  </div>
                  {ticket.isActive && (
                    <button
                      onClick={() => handleCancel(ticket.ticketId)}
                      className="mt-3 flex items-center gap-1 text-xs text-red-500 hover:text-red-700"
                    >
                      <Trash2 size={14} /> Hủy vé
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
