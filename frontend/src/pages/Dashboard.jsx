import { useEffect, useMemo, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import axios from 'axios'
import { Car, Ticket, Clock, LogIn, LogOut as LogOutIcon, QrCode, X, AlertTriangle, Search } from 'lucide-react'
import { API_BASE } from '../config/api'
import { EXIT_GATES, VEHICLE_OPTIONS } from '../config/gates'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')
const formatTime = (v) => new Date(v).toLocaleTimeString('vi-VN')

export default function Dashboard() {
  const { user } = useOutletContext()
  const [sessions, setSessions] = useState([])
  const [plateIn, setPlateIn] = useState('')
  const [cardIn, setCardIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR')
  const [plateOut, setPlateOut] = useState('')
  const [cardOut, setCardOut] = useState('')
  const [ticketIdOut, setTicketIdOut] = useState('')
  const [checkoutInfo, setCheckoutInfo] = useState(null)
  const [zoneStatus, setZoneStatus] = useState([])
  const [loadingSessions, setLoadingSessions] = useState(false)
  const [checkingOut, setCheckingOut] = useState(false)
  const [requestingQr, setRequestingQr] = useState(false)
  const [confirmingGateway, setConfirmingGateway] = useState(false)
  const [paymentQr, setPaymentQr] = useState(null)
  // [REMOVED] const [exitGate, setExitGate] = useState(...) -> Using currentGate from context
  const [isLostTicket, setIsLostTicket] = useState(false)
  const [isMonthlyCheckIn, setIsMonthlyCheckIn] = useState(false)
  const [isMonthlyCheckout, setIsMonthlyCheckout] = useState(false)
  const [qrModal, setQrModal] = useState(null)
  const [sessionSearch, setSessionSearch] = useState('')

  // Filter Logic
  const vehicleGroup = user?.gateVehicleGroup || 'CAR'
  const allowedOptions = VEHICLE_OPTIONS[vehicleGroup] || VEHICLE_OPTIONS.CAR
  const allowedTypes = useMemo(() => allowedOptions.map(c => c.value), [allowedOptions])
  const normalizedAllowed = useMemo(() => allowedTypes.map(t => t.toUpperCase().replace(/_/g, '')), [allowedTypes])

  // ... (useEffect etc)

  // ... (In render)
  const triggerPrint = (html, fileName = 'ticket.html') => {
    if (!html) return
    const blob = new Blob([html], { type: 'text/html' })
    const url = URL.createObjectURL(blob)
    const win = window.open(url, '_blank')

    if (!win) {
      alert('Trình duyệt đã chặn cửa sổ in. Hãy cho phép popup để in vé.')
      URL.revokeObjectURL(url)
      return
    }

    win.onload = () => {
      try {
        win.focus()
        win.print()
      } finally {
        setTimeout(() => {
          win.close()
          URL.revokeObjectURL(url)
        }, 1500)
      }
    }
  }

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

  const fetchZoneStatus = async () => {
    const gateToCheck = user?.gateId || 'GATE-IN-CAR-01'
    try {
      console.log('Fetching Zones status for gate:', gateToCheck)
      const res = await axios.get(`${API_BASE}/Zones/status?gateId=${gateToCheck}`)
      console.log('Zones Data:', res.data)
      setZoneStatus(res.data)
    } catch (err) {
      console.error('Error fetching zone status:', err)
      // silent
    }
  }

  useEffect(() => {
    fetchSessions()
    fetchZoneStatus()
    const interval = setInterval(() => {
      fetchSessions()
      fetchZoneStatus()
    }, 3000)
    return () => clearInterval(interval)
  }, [user?.gateId])

  const currentGate = user?.gateId || 'GATE-IN-CAR-01'

  const handleCheckIn = async () => {
    if (!plateIn) return alert('Nhập biển số trước')
    const relevantZones = zoneStatus.filter(z => z.vehicleCategory === typeIn || (typeIn.includes(z.name.split(' ')[0].toUpperCase()))) // Simple filter heuristic if needed, or just trust backend
    // Actually, backend finds suitable zone. We just check if *all* potentially relevant zones are full?
    // For simplicity: If ALL returned zones for this gate are full, block.
    // Or closer refinement: Check if any zone allows this vehicle type and has space.
    
    // Better logic: trusted from backend check-in, but UI warning is good.
    const isFull = zoneStatus.length > 0 && zoneStatus.every(z => z.isFull)
    if (isFull) return alert('Bãi đã đầy, không thể check-in!')

    if (isMonthlyCheckIn && !cardIn) return alert('Vé tháng cần quẹt thẻ (cardId)')
    if (!currentGate) return alert('Chưa chọn cổng làm việc')
    // Logic ẩn/hiện CardId: nếu !isMonthlyCheckIn thì gửi null
    const finalCardId = isMonthlyCheckIn ? (cardIn || null) : null

    try {
      const res = await axios.post(`${API_BASE}/CheckIn`, { plateNumber: plateIn, vehicleType: typeIn, gateId: currentGate, cardId: finalCardId })
      addLog(`✅ Check-in (${currentGate}): ${plateIn} - Vé: ${res.data.ticketId}`)
      const shouldPrint = res.data.shouldPrintTicket ?? res.data.ShouldPrintTicket
      if (shouldPrint) {
        const html = res.data.printHtml || res.data.PrintHtml
        const fileName = res.data.printFileName || res.data.PrintFileName || 'ticket.html'
        triggerPrint(html, fileName)
      }
      setPlateIn('')
      setCardIn('')
      fetchSessions()
    } catch (err) {
      addLog('❌ ' + (err.response?.data?.error || 'Check-in lỗi'))
    }
  }

  const handleCheckOut = async () => {
    if (!plateOut) return alert('Nhập biển số xe')
    if (isMonthlyCheckout && !cardOut) return alert('Vé tháng cần thẻ (cardId) khi ra')
    if (!isMonthlyCheckout && !isLostTicket && !ticketIdOut) return alert('Vé lượt cần nhập mã vé giấy')
    setCheckingOut(true)
    let reportWindow = null
    try {
      let url = `${API_BASE}/CheckOut`
      let payload = {};

      if (isLostTicket) {
        url = `${API_BASE}/CheckOut/lost-ticket`
        payload = { plateNumber: plateOut, vehicleType: typeIn, gateId: currentGate, printReport: true }
        reportWindow = window.open('', '_blank')
      } else if (isMonthlyCheckout) {
        payload = { ticketIdOrPlate: cardOut, gateId: currentGate, plateNumber: plateOut, cardId: cardOut }
      } else {
        payload = { ticketIdOrPlate: ticketIdOut, gateId: currentGate, plateNumber: plateOut }
      }

      const res = await axios.post(url, payload)
      setCheckoutInfo(res.data)
      setPaymentQr(null)
      addLog(`ℹ️ Xe ra (${currentGate}${isLostTicket ? ' - mất vé' : ''}): ${res.data.licensePlate} - Phí: ${formatCurrency(res.data.amount)}đ`)

      if (isLostTicket) {
        const baseAmount = res.data.baseAmount ?? res.data.BaseAmount ?? 0
        const lostPenalty = res.data.lostPenalty ?? res.data.LostPenalty ?? 0
        addLog(`💸 Chi tiết phí: Gửi xe ${formatCurrency(baseAmount)}đ + Mất vé ${formatCurrency(lostPenalty)}đ`)
      }

      if (isLostTicket && res.data.reportUrl) {
        if (reportWindow) {
          reportWindow.location.href = res.data.reportUrl
        } else {
          window.open(res.data.reportUrl, '_blank')
        }
      } else if (reportWindow) {
        reportWindow.close()
      }
    } catch (err) {
      if (reportWindow) {
        try { reportWindow.close() } catch { /* no-op */ }
      }
      addLog('❌ ' + (err.response?.data?.error || 'Không tìm thấy xe'))
    } finally {
      setCheckingOut(false)
    }
  }

  const handleRequestQr = async () => {
    if (!checkoutInfo) return alert('Cần kiểm tra xe ra trước')
    if (checkoutInfo.amount <= 0) return handleFreePayment()
    setRequestingQr(true)
    try {
      const res = await axios.post(`${API_BASE}/Payment`, {
        sessionId: checkoutInfo.sessionId,
        amount: checkoutInfo.amount,
        exitGateId: currentGate,
        method: 'ExternalQR'
      })

      const qrContent = res.data.qrContent || res.data.QrContent
      const transactionCode = res.data.transactionCode || res.data.TransactionCode
      setPaymentQr({
        qrContent,
        transactionCode,
        providerLog: res.data.providerLog || res.data.ProviderLog,
        status: res.data.status || res.data.Status
      })

      setQrModal({
        content: qrContent,
        title: 'Quét QR thanh toán',
        subtitle: `Mã giao dịch: ${transactionCode || 'N/A'}`
      })

      addLog(`📨 Nhận QR thanh toán (${transactionCode || 'chưa có mã'}).`)
    } catch (err) {
      const apiMessage = err.response?.data?.message || err.response?.data?.Message
      addLog('❌ Lấy QR thất bại: ' + (apiMessage || err.message))
    } finally {
      setRequestingQr(false)
    }
  }

  const handleFreePayment = async () => {
    if (!checkoutInfo) return
    setConfirmingGateway(true)
    try {
      await axios.post(`${API_BASE}/Payment/callback`, {
        sessionId: checkoutInfo.sessionId,
        transactionCode: 'FREE-0',
        status: 'SUCCESS',
        providerLog: 'Miễn phí 0đ',
        exitGateId: currentGate
      })

      addLog('✅ Phiên miễn phí 0đ - đã đóng phiên và mở cổng')
      setCheckoutInfo(null)
      setPaymentQr(null)
      setPlateOut('')
      setCardOut('')
      fetchSessions()
    } catch (err) {
      const apiMessage = err.response?.data?.message || err.response?.data?.Message
      addLog('❌ Xử lý miễn phí thất bại: ' + (apiMessage || err.message))
    } finally {
      setConfirmingGateway(false)
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

  const handleGatewayCallback = async (status = 'SUCCESS') => {
    if (!checkoutInfo || !paymentQr?.transactionCode) return alert('Chưa có phiên thanh toán cần ghi nhận log')
    setConfirmingGateway(true)
    try {
      const res = await axios.post(`${API_BASE}/Payment/callback`, {
        sessionId: checkoutInfo.sessionId,
        transactionCode: paymentQr.transactionCode,
        status,
        providerLog: `Gateway ${status === 'SUCCESS' ? 'đã thanh toán' : 'không thành công'} lúc ${new Date().toLocaleTimeString('vi-VN')}`,
        exitGateId: currentGate
      })

      addLog(`📥 Log từ gateway: ${res.data.providerLog || res.data.ProviderLog || status}`)

      if (status === 'SUCCESS') {
        setCheckoutInfo(null)
        setPaymentQr(null)
        setPlateOut('')
        setCardOut('')
        fetchSessions()
      }
    } catch (err) {
      const apiMessage = err.response?.data?.message || err.response?.data?.Message
      addLog('❌ Ghi nhận log thất bại: ' + (apiMessage || err.message))
    } finally {
      setConfirmingGateway(false)
    }
  }

  const filteredSessions = useMemo(() => {
    return sessions.filter(s => {
      // Filter by vehicle type
      const rawType = s.vehicle?.vehicleType || s.vehicleType || s.VehicleType || ''
      const type = rawType.toUpperCase().replace(/_/g, '')
      if (!normalizedAllowed.includes(type)) return false
      
      // Filter out completed sessions (safety check)
      const status = s.status || s.Status || ''
      return status !== 'Completed'
    })
  }, [sessions, normalizedAllowed])

  return (
    <div className="grid lg:grid-cols-3 gap-6 relative">
      {renderQrModal()}
      
      {/* Sessions Table - Full Width */}
      <div className="lg:col-span-3">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <div className="w-10 h-10 rounded-lg bg-purple-100 flex items-center justify-center">
                <Car className="text-purple-600" size={20} />
              </div>
              <div>
                <h3 className="font-semibold text-gray-800">Xe trong bãi</h3>
                <p className="text-xs text-gray-500">{filteredSessions.length} xe đang gửi</p>
              </div>
            </div>
            
            <div className="flex items-center gap-2">
                 <div className="relative">
                    <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                    <input 
                        type="text" 
                        placeholder="Tìm biển số..." 
                        className="pl-8 pr-3 py-1.5 text-xs border border-gray-200 rounded-lg outline-none focus:border-purple-500 transition"
                        value={sessionSearch}
                        onChange={(e) => setSessionSearch(e.target.value)}
                    />
                 </div>
                 {loadingSessions && <span className="text-xs text-gray-400">Đang tải...</span>}
            </div>
          </div>
          
          
          <div className="overflow-auto min-h-[400px]">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left text-gray-500 border-b">
                  <th className="pb-3 font-medium">Biển số</th>
                  <th className="pb-3 font-medium">Loại xe</th>
                  <th className="pb-3 font-medium">Mã vé</th>
                  <th className="pb-3 font-medium">Giờ vào</th>
                  <th className="pb-3 font-medium">Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {filteredSessions
                    .filter(s => !sessionSearch || (s.vehicle?.licensePlate || s.plateNumber || '').toUpperCase().includes(sessionSearch.toUpperCase()))
                    .map((s) => (
                  <tr 
                    key={s.sessionId} 
                    className="border-b border-gray-50 hover:bg-blue-50 transition-colors"
                  >
                    <td className="py-3 font-semibold text-gray-800">{s.vehicle?.licensePlate || s.plateNumber}</td>
                    <td className="py-3 text-gray-600">{s.vehicle?.vehicleType || s.vehicleType}</td>
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
