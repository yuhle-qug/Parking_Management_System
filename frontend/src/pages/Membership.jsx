import { useEffect, useState, useMemo } from 'react'
import axios from 'axios'
import { CreditCard, User, Car, Calendar, CheckCircle, Clock, Trash2, Search, X, History, XCircle, CalendarPlus, Phone } from 'lucide-react'
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
  const [search, setSearch] = useState('')
  const [form, setForm] = useState({
    ownerName: '',
    phone: '',
    licensePlate: '',
    vehicleType: 'CAR',
    months: 1,
    policyId: '',
    identityNumber: ''
  })
  const [qrModal, setQrModal] = useState(null)
  const [renewModal, setRenewModal] = useState(null)
  const [historyModal, setHistoryModal] = useState(null)
  const [confirmModal, setConfirmModal] = useState(null)
  const [detailModal, setDetailModal] = useState(null)
  const [history, setHistory] = useState([])

  const defaultPolicies = [
    { policyId: 'P-CAR', policyName: 'Vé tháng Ô tô', monthlyPrice: 1500000, vehicleType: 'CAR' },
    { policyId: 'P-MOTO', policyName: 'Vé tháng Xe máy', monthlyPrice: 120000, vehicleType: 'MOTORBIKE' },
    { policyId: 'P-ELEC', policyName: 'Vé tháng Xe điện', monthlyPrice: 1000000, vehicleType: 'ELECTRIC_CAR' },
    { policyId: 'P-BIKE', policyName: 'Vé tháng Xe đạp', monthlyPrice: 80000, vehicleType: 'BICYCLE' }
  ]

  const filteredPolicies = useMemo(() => {
    return policies.filter(p => (p.vehicleType || '').toUpperCase() === form.vehicleType.toUpperCase())
  }, [policies, form.vehicleType])

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
        vehicleType: (p.vehicleType ?? p.VehicleType ?? '').toString().toUpperCase()
      })).filter(p => !!p.policyId)

      const finalPolicies = mappedPolicies.length ? mappedPolicies : defaultPolicies
      setPolicies(finalPolicies)
      if (!form.policyId) {
        const firstForType = finalPolicies.find(p => p.vehicleType === form.vehicleType) || finalPolicies[0]
        if (firstForType?.policyId) {
          setForm(prev => ({ ...prev, policyId: firstForType.policyId }))
        }
      }

      const rawTickets = Array.isArray(ticketRes.data) ? ticketRes.data : []
      const now = new Date()
      const mappedTickets = rawTickets.map(t => {
        const ticketId = t.ticketId ?? t.TicketId
        const startDate = t.startDate ?? t.StartDate
        const endDate = t.endDate ?? t.ExpiryDate ?? t.expiryDate
        const status = (t.status ?? t.Status ?? '').toString()
        const paymentStatus = (t.paymentStatus ?? t.PaymentStatus ?? '').toString()
        const transactionCode = t.transactionCode ?? t.TransactionCode
        const qrContent = t.qrContent ?? t.QrContent
        const providerLog = t.providerLog ?? t.ProviderLog
        const end = parseDate(endDate)
        const statusLower = status.trim().toLowerCase()
        // Nếu parseDate thất bại, vẫn giữ vé Active để tránh lọc nhầm.
        const isActive = statusLower === 'active' && (!end || end >= now)
        const daysLeft = end ? Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)) : null

        return {
          ticketId,
          customerId: t.customerId ?? t.CustomerId ?? '',
          ownerName: t.ownerName ?? t.OwnerName ?? t.customerName ?? t.CustomerName ?? (t.customerId ?? t.CustomerId ?? '').toString(),
          phone: t.phone ?? t.Phone ?? '',
          identityNumber: t.identityNumber ?? t.IdentityNumber ?? '',
          licensePlate: t.licensePlate ?? t.VehiclePlate ?? t.vehiclePlate ?? '',
          startDate,
          endDate,
          isActive,
          vehicleType: t.vehicleType ?? t.VehicleType ?? '',
          monthlyFee: t.monthlyFee ?? t.MonthlyFee ?? 0,
          status,
          paymentStatus,
          transactionCode,
          qrContent,
          providerLog,
          daysLeft
        }
      }).filter(t => !!t.ticketId)

      // Hiển thị cả vé đang chờ thanh toán để tiện theo dõi
      setTickets(mappedTickets)
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
    // Set confirm modal instead of API call
    setConfirmModal({ ...form, displayPrice: estimatedPrice })
  }

  const handleConfirmSubmit = async () => {
    if (!confirmModal) return
    try {
      // Gọi API backend
      const res = await axios.post(`${API_BASE}/Membership/register`, {
        name: confirmModal.ownerName,
        phone: confirmModal.phone,
        identityNumber: confirmModal.identityNumber,
        plateNumber: confirmModal.licensePlate.toUpperCase(),
        vehicleType: confirmModal.vehicleType,
        planId: confirmModal.policyId,
        months: confirmModal.months
      })

      const payment = res.data?.payment || res.data?.Payment
      const ticket = res.data?.ticket || res.data?.Ticket || res.data

      if (payment?.qrContent || payment?.QrContent) {
        const qrContent = payment.qrContent || payment.QrContent
        setQrModal({
          content: qrContent,
          title: `Thanh toán vé ${ticket?.ticketId || ''}`,
          subtitle: payment.transactionCode || payment.TransactionCode || 'Quét mã để thanh toán'
        })
      } else {
        alert('Đăng ký thành công, chờ xác nhận thanh toán.')
      }

      setForm({ ownerName: '', phone: '', licensePlate: '', vehicleType: 'CAR', months: 1, policyId: policies[0]?.policyId || '', identityNumber: '' })
      setConfirmModal(null)
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Lỗi đăng ký')
    }
  }

  const filteredTickets = useMemo(() => {
    const q = search.trim().toUpperCase()
    if (!q) return tickets
    return tickets.filter(t =>
      (t.licensePlate || '').toUpperCase().includes(q) ||
      (t.ticketId || '').toUpperCase().includes(q) ||
      (t.phone || '').toUpperCase().includes(q) ||
      (t.ownerName || '').toUpperCase().includes(q)
    )
  }, [search, tickets])

  const handleCancel = async (ticketId) => {
    const note = window.prompt('Lý do hủy (không bắt buộc):')
    if (note === null) return // User clicked Cancel

    try {
      await axios.post(`${API_BASE}/Membership/tickets/${ticketId}/cancel`, {
        performedBy: 'staff',
        note: note || 'Hủy bởi quản lý'
      })
      alert('Đã hủy vé tháng thành công')
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Hủy vé thất bại')
    }
  }

  const handleConfirmPayment = async (ticketId) => {
    try {
      await axios.post(`${API_BASE}/Membership/confirm-payment`, {
        ticketId,
        status: 'SUCCESS',
        transactionCode: `MANUAL-${Date.now()}`
      })
      alert('Đã xác nhận thanh toán cho vé tháng')
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Xác nhận thanh toán thất bại')
    }
  }

  const handleRenew = (ticket) => {
    setRenewModal({ ticket, months: 1 })
  }

  const handleRenewSubmit = async () => {
    if (!renewModal?.ticket) return
    const { ticket, months } = renewModal
    try {
      const res = await axios.post(`${API_BASE}/Membership/tickets/${ticket.ticketId}/extend`, {
        months,
        performedBy: 'staff',
        note: `Gia hạn ${months} tháng`
      })

      const payment = res.data?.payment || res.data?.Payment
      const updatedTicket = res.data?.ticket || res.data?.Ticket || res.data

      if (payment?.qrContent || payment?.QrContent) {
        const qrContent = payment.qrContent || payment.QrContent
        setQrModal({
          content: qrContent,
          title: `Thanh toán gia hạn vé ${updatedTicket?.ticketId || ticket.ticketId}`,
          subtitle: payment.transactionCode || payment.TransactionCode || 'Quét mã để thanh toán'
        })
      } else {
        alert('Gia hạn thành công, chờ xác nhận thanh toán.')
      }

      setRenewModal(null)
      await fetchData()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Gia hạn thất bại')
    }
  }

  const handleViewHistory = async (ticketId) => {
    try {
      const res = await axios.get(`${API_BASE}/Membership/tickets/${ticketId}/history`)
      const rawHistory = Array.isArray(res.data) ? res.data : []
      const mappedHistory = rawHistory.map(h => ({
        historyId: h.historyId || h.HistoryId,
        action: h.action || h.Action,
        months: h.months || h.Months || 0,
        amount: h.amount || h.Amount || 0,
        performedBy: h.performedBy || h.PerformedBy,
        time: h.time || h.Time,
        note: h.note || h.Note
      }))
      setHistory(mappedHistory)
      setHistoryModal(ticketId)
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Tải lịch sử thất bại')
    }
  }

  const renderQrModal = () => {
    if (!qrModal?.content) return null
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-[360px] space-y-4 border border-gray-100">
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-xs text-gray-500">{qrModal.subtitle || 'Quét mã để thanh toán'}</div>
              <div className="text-base font-semibold text-gray-800">{qrModal.title || 'Mã QR'}</div>
            </div>
            <button
              type="button"
              onClick={() => setQrModal(null)}
              className="text-gray-500 hover:text-gray-800"
            >
              <X size={18} />
            </button>
          </div>
          <div className="flex flex-col items-center gap-2">
            <img
              className="w-64 h-64 rounded-lg border"
              alt="QR"
              src={`https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=${encodeURIComponent(qrModal.content)}`}
            />
            <div className="text-[11px] text-gray-500 break-all text-center leading-tight">{qrModal.content}</div>
          </div>
        </div>
      </div>
    )
  }

  const renderRenewModal = () => {
    if (!renewModal?.ticket) return null
    const { ticket, months } = renewModal
    const policy = policies.find(p => p.vehicleType === ticket.vehicleType)
    const estimatedFee = policy ? policy.monthlyPrice * months * (months >= 12 ? 0.85 : months >= 6 ? 0.9 : months >= 3 ? 0.95 : 1) : 0
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-[400px] space-y-4 border border-gray-100">
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-base font-semibold text-gray-800">Gia hạn vé tháng</div>
              <div className="text-xs text-gray-500">Vé: {ticket.ticketId} - {ticket.licensePlate}</div>
            </div>
            <button
              type="button"
              onClick={() => setRenewModal(null)}
              className="text-gray-500 hover:text-gray-800"
            >
              <X size={18} />
            </button>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-600 mb-1">Số tháng gia hạn</label>
            <select
              className="w-full bg-gray-50 rounded-lg px-3 py-2.5 text-sm outline-none"
              value={months}
              onChange={(e) => setRenewModal({ ...renewModal, months: parseInt(e.target.value) })}
            >
              <option value={1}>1 tháng</option>
              <option value={3}>3 tháng (tiết kiệm 5%)</option>
              <option value={6}>6 tháng (tiết kiệm 10%)</option>
              <option value={12}>12 tháng (tiết kiệm 15%)</option>
            </select>
          </div>
          <div className="bg-indigo-50 rounded-lg p-4">
            <div className="flex justify-between items-center">
              <span className="text-sm text-gray-600">Phí gia hạn:</span>
              <span className="text-xl font-bold text-indigo-600">{formatCurrency(estimatedFee)} đ</span>
            </div>
          </div>
          <button
            onClick={handleRenewSubmit}
            className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-3 rounded-lg transition"
          >
            Xác nhận gia hạn
          </button>
        </div>
      </div>
    )
  }

  const renderHistoryModal = () => {
    if (!historyModal) return null
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-[600px] max-h-[80vh] overflow-auto space-y-4 border border-gray-100">
          <div className="flex items-start justify-between gap-3 sticky top-0 bg-white pb-3">
            <div>
              <div className="text-base font-semibold text-gray-800">Lịch sử vé tháng</div>
              <div className="text-xs text-gray-500">Vé: {historyModal}</div>
            </div>
            <button
              type="button"
              onClick={() => setHistoryModal(null)}
              className="text-gray-500 hover:text-gray-800"
            >
              <X size={18} />
            </button>
          </div>
          {history.length === 0 ? (
            <div className="text-center py-12 text-gray-400">
              <History size={48} className="mx-auto mb-3 opacity-30" />
              <p>Chưa có lịch sử giao dịch</p>
            </div>
          ) : (
            <div className="space-y-2">
              {history.map(h => (
                <div key={h.historyId} className="border border-gray-100 rounded-lg p-4 hover:bg-gray-50">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <span className={`text-xs px-2 py-1 rounded-full font-semibold ${h.action === 'Register' ? 'bg-green-100 text-green-700' : h.action === 'Extend' ? 'bg-blue-100 text-blue-700' : 'bg-red-100 text-red-700'}`}>
                          {h.action === 'Register' ? '📝 Đăng ký' : h.action === 'Extend' ? '🔄 Gia hạn' : '❌ Hủy'}
                        </span>
                        <span className="text-xs text-gray-500">{formatDate(h.time)}</span>
                      </div>
                      <div className="text-sm text-gray-700">
                        {h.action === 'Extend' && `Gia hạn ${h.months} tháng`}
                        {h.action === 'Register' && `Đăng ký ${h.months} tháng`}
                        {h.action === 'Cancel' && 'Hủy vé tháng'}
                      </div>
                      {h.note && <div className="text-xs text-gray-500 mt-1">Ghi chú: {h.note}</div>}
                    </div>
                    <div className="text-right">
                      <div className="text-sm font-semibold text-gray-800">{formatCurrency(h.amount)} đ</div>
                      <div className="text-xs text-gray-500">Bởi: {h.performedBy}</div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    )
  }

  const renderConfirmModal = () => {
    if (!confirmModal) return null
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-[400px] space-y-4 border border-gray-100">
           <h3 className="text-lg font-bold text-gray-800">Xác nhận thông tin</h3>
           <div className="space-y-2 text-sm bg-gray-50 p-4 rounded-lg">
              <div className="flex justify-between"><span className="text-gray-500">Chủ xe:</span><span className="font-semibold">{confirmModal.ownerName}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Biển số:</span><span className="font-bold">{confirmModal.licensePlate}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Loại xe:</span><span>{confirmModal.vehicleType}</span></div>
              <div className="flex justify-between"><span className="text-gray-500">Gói đăng ký:</span><span>{confirmModal.months} tháng</span></div>
              <div className="border-t border-gray-200 mt-2 pt-2 flex justify-between"><span className="text-gray-600 font-medium">Tổng phí:</span><span className="text-indigo-600 font-bold text-lg">{formatCurrency(confirmModal.displayPrice)} đ</span></div>
           </div>
           <div className="flex gap-3">
             <button onClick={() => setConfirmModal(null)} className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold py-2.5 rounded-lg transition">Sửa lại</button>
             <button onClick={handleConfirmSubmit} className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-2.5 rounded-lg transition">Xác nhận</button>
           </div>
        </div>
      </div>
    )
  }

  const renderDetailModal = () => {
    if (!detailModal) return null
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-[400px] space-y-4 border border-gray-100">
           <div className="flex items-start justify-between gap-3 border-b border-gray-100 pb-3">
             <div>
                <h3 className="text-lg font-bold text-gray-800">Thông tin khách hàng</h3>
                <div className="flex gap-3 text-xs text-gray-500">
                    <span>Mã vé: {detailModal.ticketId}</span>
                    <span className="text-gray-300">|</span>
                    <span>Mã KH: {detailModal.customerId}</span>
                </div>
             </div>
             <button onClick={() => setDetailModal(null)} className="text-gray-500 hover:text-gray-800"><X size={18} /></button>
           </div>
           
           <div className="space-y-4">
              <div className="flex items-center gap-3">
                 <div className="w-10 h-10 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600">
                    <User size={20} />
                 </div>
                 <div>
                    <div className="text-sm text-gray-500">Họ và tên</div>
                    <div className="font-semibold text-gray-800 text-lg">{detailModal.ownerName}</div>
                 </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                 <div className="bg-gray-50 p-3 rounded-lg">
                    <div className="text-xs text-gray-500 mb-1">Số điện thoại</div>
                    <div className="font-mono font-medium text-gray-800">{detailModal.phone || '---'}</div>
                 </div>
                 <div className="bg-gray-50 p-3 rounded-lg">
                    <div className="text-xs text-gray-500 mb-1">CMND/CCCD</div>
                    <div className="font-mono font-medium text-gray-800">{detailModal.identityNumber || '---'}</div>
                 </div>
              </div>

              <div className="bg-blue-50 p-3 rounded-lg flex justify-between items-center">
                 <div>
                    <div className="text-xs text-blue-600 mb-1">Xe đăng ký</div>
                    <div className="font-bold text-blue-800">{detailModal.licensePlate}</div>
                 </div>
                 <div className="text-xs font-mono bg-white px-2 py-1 rounded text-blue-600 border border-blue-100">
                    {detailModal.vehicleType}
                 </div>
              </div>
           </div>

           <button onClick={() => setDetailModal(null)} className="w-full bg-gray-100 hover:bg-gray-200 text-gray-700 font-semibold py-2.5 rounded-lg transition mt-2">
              Đóng
           </button>
        </div>
      </div>
    )
  }

  return (
    <div className="grid lg:grid-cols-3 gap-6 relative">
      {renderQrModal()}
      {renderRenewModal()}
      {renderHistoryModal()}
      {renderConfirmModal()}
      {renderDetailModal()}
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
              <label className="block text-sm font-medium text-gray-600 mb-1">Số CMND/CCCD</label>
              <input
                type="text"
                className="w-full bg-gray-50 rounded-lg px-3 py-2 text-sm outline-none"
                placeholder="0123456789"
                value={form.identityNumber}
                onChange={(e) => setForm({ ...form, identityNumber: e.target.value })}
              />
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
                <option value="ELECTRIC_CAR">⚡ Ô tô điện</option>
                <option value="ELECTRIC_MOTORBIKE">🔋 Xe máy điện</option>
                <option value="BICYCLE">🚲 Xe đạp</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-600 mb-2">Gói đăng ký</label>
              <div className="grid grid-cols-2 gap-2">
                {filteredPolicies.map(p => (
                  <button
                    key={p.policyId}
                    type="button"
                    onClick={() => setForm({ ...form, policyId: p.policyId })}
                    className={`rounded-lg border px-3 py-3 text-left text-sm font-semibold transition ${form.policyId === p.policyId ? 'border-indigo-500 bg-indigo-50 text-indigo-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                  >
                    <div className="flex items-center justify-between">
                      <span>{p.policyName}</span>
                      <span className="text-xs font-mono">{p.vehicleType}</span>
                    </div>
                    <div className="text-xs text-gray-500 mt-1">{formatCurrency(p.monthlyPrice)} đ/tháng</div>
                  </button>
                ))}
              </div>
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
            <div className="flex items-center gap-3">
              <div className="relative">
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="pl-8 pr-3 py-2 text-sm border border-gray-200 rounded-lg bg-gray-50 focus:bg-white focus:border-indigo-200 outline-none"
                  placeholder="Tìm mã vé / biển số"
                />
              </div>
              {loading && <span className="text-xs text-gray-400">Đang tải...</span>}
            </div>
          </div>

          {tickets.length === 0 ? (
            <div className="text-center py-12 text-gray-400">
              <CreditCard size={48} className="mx-auto mb-3 opacity-30" />
              <p>Chưa có vé tháng nào được đăng ký</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4 h-[600px] overflow-y-auto pr-2">
              {filteredTickets.map((t) => (
                <div key={t.ticketId} className="bg-white rounded-xl border border-gray-100 p-4 shadow-sm hover:shadow-md transition-shadow flex flex-col justify-between">
                   <div>
                      <div className="flex justify-between items-start mb-3">
                         <span className="font-mono text-xs text-gray-400 bg-gray-50 px-2 py-1 rounded">{t.ticketId}</span>
                         {(() => {
                            const s = (t.status || '').toString().toLowerCase()
                            const pay = (t.paymentStatus || '').toString().toLowerCase()
                            const cls = pay === 'completed' || s === 'active'
                              ? 'bg-green-100 text-green-700'
                              : pay === 'failed'
                                ? 'bg-red-100 text-red-700'
                                : pay === 'pendingexternal'
                                  ? 'bg-amber-100 text-amber-700'
                                  : 'bg-gray-100 text-gray-500'
                            const label = pay === 'pendingexternal' ? 'Chờ thanh toán' : pay === 'failed' ? 'Lỗi thanh toán' : t.status
                            return <span className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full ${cls}`}>{label}</span>
                         })()}
                      </div>
                      
                      <div className="text-center py-2">
                         <div className="text-2xl font-bold text-gray-800 tracking-tight">{t.licensePlate || '---'}</div>
                         <div className="text-sm font-semibold text-gray-800 truncate mt-1">{t.ownerName}</div>
                         <div className="text-xs text-gray-500 flex items-center justify-center gap-1 mt-0.5">
                             <Phone size={10} /> {t.phone || '---'}
                         </div>
                      </div>

                      <div className="mt-2 space-y-2 text-xs text-gray-600 bg-gray-50 p-2 rounded-lg">
                          <div className="flex justify-between">
                             <span>Hạn dùng:</span>
                             <span className="font-medium">{formatDate(t.endDate)}</span>
                          </div>
                          <div className="flex justify-between items-center">
                             <span>Còn lại:</span>
                             {t.daysLeft != null ? (
                                <span className={`font-bold ${t.daysLeft <= 7 ? 'text-amber-600' : 'text-emerald-600'}`}>
                                   {t.daysLeft} ngày
                                </span>
                             ) : <span>--</span>}
                          </div>
                      </div>
                   </div>

                   <div className="mt-4 pt-3 border-t border-gray-100 grid grid-cols-2 gap-2">
                      {t.isActive ? (
                         <>
                            <button onClick={() => handleRenew(t)} className="flex items-center justify-center gap-1 text-xs font-semibold py-2 bg-green-50 text-green-600 rounded-lg hover:bg-green-100 transition">
                               <CalendarPlus size={14} /> Gia hạn
                            </button>
                            <button onClick={() => handleCancel(t.ticketId)} className="flex items-center justify-center gap-1 text-xs font-semibold py-2 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 transition">
                               <XCircle size={14} /> Hủy vé
                            </button>
                         </>
                      ) : (
                         <div className="col-span-2 text-center text-xs text-gray-400 italic py-2">Vé không khả dụng</div>
                      )}
                      
                      {(t.paymentStatus?.toLowerCase() === 'pendingexternal' || t.paymentStatus?.toLowerCase() === 'failed') && (
                          <button onClick={() => handleConfirmPayment(t.ticketId)} className="col-span-2 flex items-center justify-center gap-1 text-xs font-semibold py-2 bg-indigo-50 text-indigo-600 rounded-lg hover:bg-indigo-100 transition">
                             <CheckCircle size={14} /> Xác nhận thanh toán
                          </button>
                      )}

                      <button onClick={() => handleViewHistory(t.ticketId)} className="flex items-center justify-center gap-1 text-xs font-semibold py-1.5 bg-gray-50 text-gray-600 rounded-lg hover:bg-gray-100 transition">
                         <History size={14} /> Lịch sử
                      </button>
                      <button onClick={() => setDetailModal(t)} className="flex items-center justify-center gap-1 text-xs font-semibold py-1.5 bg-blue-50 text-blue-600 rounded-lg hover:bg-blue-100 transition">
                         <User size={14} /> Chi tiết
                      </button>
                   </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
