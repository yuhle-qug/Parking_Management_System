import { useEffect, useMemo, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import axios from 'axios'
import { Car, Ticket, Clock, LogIn, LogOut as LogOutIcon, QrCode, X, AlertTriangle } from 'lucide-react'
import { API_BASE } from '../config/api'
import { EXIT_GATES, VEHICLE_OPTIONS } from '../config/gates'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')
const formatTime = (v) => new Date(v).toLocaleTimeString('vi-VN')

export default function Dashboard() {
  const { user } = useOutletContext()
  const [sessions, setSessions] = useState([])
  const [logs, setLogs] = useState([])
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
  const [exitGate, setExitGate] = useState(EXIT_GATES[0]?.id || 'GATE-OUT-01')
  const [isLostTicket, setIsLostTicket] = useState(false)
  const [isMonthlyCheckIn, setIsMonthlyCheckIn] = useState(false)
  const [isMonthlyCheckout, setIsMonthlyCheckout] = useState(false)
  const [qrModal, setQrModal] = useState(null)

  const vehicleGroup = user?.gateVehicleGroup || 'CAR'
  const vehicleChoices = useMemo(
    () => VEHICLE_OPTIONS[vehicleGroup] || VEHICLE_OPTIONS.CAR,
    [vehicleGroup]
  )

  useEffect(() => {
    setTypeIn(vehicleChoices[0]?.value || 'CAR')
  }, [vehicleChoices])

  const addLog = (msg) => setLogs((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev].slice(0, 60))

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
    const gateToCheck = user?.gateId || 'GATE-IN-01'
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

  const currentGate = user?.gateId || 'GATE-IN-01'

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
    try {
      let url = `${API_BASE}/CheckOut`
      let payload = {};

      if (isLostTicket) {
        url = `${API_BASE}/CheckOut/lost-ticket`
        payload = { plateNumber: plateOut, vehicleType: typeIn, gateId: exitGate, printReport: true }
      } else if (isMonthlyCheckout) {
        payload = { ticketIdOrPlate: cardOut, gateId: exitGate, plateNumber: plateOut, cardId: cardOut }
      } else {
        payload = { ticketIdOrPlate: ticketIdOut, gateId: exitGate, plateNumber: plateOut }
      }

      const res = await axios.post(url, payload)
      setCheckoutInfo(res.data)
      setPaymentQr(null)
      addLog(`ℹ️ Xe ra (${exitGate}${isLostTicket ? ' - mất vé' : ''}): ${res.data.licensePlate} - Phí: ${formatCurrency(res.data.amount)}đ`)

      if (isLostTicket && res.data.reportUrl) {
        window.open(res.data.reportUrl, '_blank')
      }
    } catch (err) {
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
        exitGateId: exitGate,
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
        exitGateId: exitGate
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
        exitGateId: exitGate
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

  return (
    <div className="grid lg:grid-cols-3 gap-6 relative">
      {renderQrModal()}
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

          
          {/* Capacity Status */}
          <div className="mb-4">
             <div className="flex items-center justify-between mb-3">
                <span className="text-sm font-semibold text-gray-700">Tình trạng bãi</span>
                <span className="text-xs text-gray-500">
                  Tổng chỗ: {zoneStatus.reduce((acc, z) => acc + (z.capacity || 0), 0)}
                </span>
             </div>
             {(() => {
                // Filter zones based on gate vehicle group
                const visibleZones = zoneStatus.filter(z => {
                  if (vehicleGroup === 'CAR') return z.vehicleCategory === 'CAR'
                  if (vehicleGroup === 'MOTORBIKE') return ['MOTORBIKE', 'BICYCLE'].includes(z.vehicleCategory)
                  return true // Default show all if group unknown
                })

                return visibleZones.length > 0 ? (
                  <div className="grid grid-cols-2 gap-3">
                    {visibleZones.map(z => {
                       const occupancy = z.capacity > 0 ? ((z.active / z.capacity) * 100) : 0
                       const colorClass = z.isFull ? 'text-red-600' : occupancy > 80 ? 'text-amber-600' : 'text-emerald-600'
                       const barColor = z.isFull ? 'bg-red-500' : occupancy > 80 ? 'bg-amber-500' : 'bg-emerald-500'

                       return (
                        <div key={z.zoneId} className={`rounded-xl border p-3 flex flex-col justify-between shadow-sm transition hover:shadow-md ${z.isFull ? 'bg-red-50 border-red-100' : 'bg-white border-gray-100'}`}>
                          <div className="flex justify-between items-start mb-2">
                            <span className="text-xs font-medium text-gray-500 uppercase tracking-tight">{z.name}</span>
                            {z.isFull && <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-red-200 text-red-700">FULL</span>}
                          </div>
                          
                          <div className="flex items-baseline gap-1 mb-2">
                             <span className={`text-2xl font-bold ${colorClass}`}>{z.available}</span>
                             <span className="text-xs text-gray-400">/ {z.capacity}</span>
                          </div>

                          <div className="w-full bg-gray-100 rounded-full h-1.5 overflow-hidden">
                             <div 
                                className={`h-full rounded-full transition-all duration-500 ${barColor}`} 
                                style={{ width: `${occupancy}%` }}
                             ></div>
                          </div>
                          <div className="mt-1 text-[10px] text-end text-gray-400">
                            {Math.round(occupancy)}% sử dụng
                          </div>
                        </div>
                       )
                    })}
                  </div>
               ) : (
                  <div className="text-center py-4 text-xs text-gray-400 bg-gray-50 rounded-xl border border-dashed animate-pulse">
                     Đang tải số liệu...
                  </div>
               )
             })()} 
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
            {isMonthlyCheckIn && (
              <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
                <Ticket size={18} className="text-gray-400" />
                <input
                  className="flex-1 bg-transparent outline-none text-sm"
                  placeholder="Mã thẻ (cardId)" 
                  value={cardIn}
                  onChange={(e) => setCardIn(e.target.value)}
                />
              </div>
            )}
              <div className="grid grid-cols-2 gap-2 text-sm font-semibold">
                <button
                  type="button"
                  onClick={() => setIsMonthlyCheckIn(false)}
                  className={`rounded-lg px-3 py-2 border transition ${!isMonthlyCheckIn ? 'border-green-500 bg-green-50 text-green-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                >
                  Vé lượt (in vé)
                </button>
                <button
                  type="button"
                  onClick={() => setIsMonthlyCheckIn(true)}
                  className={`rounded-lg px-3 py-2 border transition ${isMonthlyCheckIn ? 'border-indigo-500 bg-indigo-50 text-indigo-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                >
                  Vé tháng (thẻ)
                </button>
              </div>
            <div className="text-xs text-gray-500 font-semibold">Luồng đã chọn: {vehicleGroup === 'CAR' ? 'Ô tô' : 'Xe máy / xe đạp'}</div>
            <div className="grid grid-cols-2 gap-2">
              {vehicleChoices.map((opt) => (
                <button
                  key={opt.value}
                  type="button"
                  onClick={() => setTypeIn(opt.value)}
                  className={`flex items-center gap-2 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                    typeIn === opt.value ? 'border-green-500 bg-green-50 text-green-700' : 'border-gray-200 bg-gray-50 text-gray-700'
                  }`}
                >
                  <span className="text-lg">{opt.icon}</span>
                  {opt.label}
                </button>
              ))}
            </div>
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
                <Car size={18} className="text-gray-400" />
                <input
                  className="flex-1 bg-transparent outline-none text-sm"
                  placeholder="Biển số xe..."
                  value={plateOut}
                  onChange={(e) => setPlateOut(e.target.value.toUpperCase())}
                />
              </div>

              {!isMonthlyCheckout && !isLostTicket && (
                <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
                  <Ticket size={18} className="text-gray-400" />
                  <input
                    className="flex-1 bg-transparent outline-none text-sm"
                    placeholder="Mã vé giấy (ticketId)"
                    value={ticketIdOut}
                    onChange={(e) => setTicketIdOut(e.target.value.toUpperCase())}
                  />
                </div>
              )}

              {isMonthlyCheckout && (
                <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2">
                  <QrCode size={18} className="text-gray-400" />
                  <input
                    className="flex-1 bg-transparent outline-none text-sm"
                    placeholder="CardId (mã thẻ vé tháng)"
                    value={cardOut}
                    onChange={(e) => setCardOut(e.target.value)}
                  />
                </div>
              )}

              <div className="grid grid-cols-3 gap-2 text-sm font-semibold">
                <button
                  type="button"
                  onClick={() => { setIsMonthlyCheckout(false); setIsLostTicket(false); }}
                  className={`rounded-lg px-3 py-2 border transition ${(!isMonthlyCheckout && !isLostTicket) ? 'border-blue-500 bg-blue-50 text-blue-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                >
                  Vé lượt
                </button>
                <button
                  type="button"
                  onClick={() => { setIsMonthlyCheckout(true); setIsLostTicket(false); }}
                  className={`rounded-lg px-3 py-2 border transition ${isMonthlyCheckout ? 'border-indigo-500 bg-indigo-50 text-indigo-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                >
                  Vé tháng
                </button>
                <button
                  type="button"
                  onClick={() => { setIsLostTicket(true); setIsMonthlyCheckout(false); }}
                  className={`rounded-lg px-3 py-2 border transition ${isLostTicket ? 'border-amber-500 bg-amber-50 text-amber-700' : 'border-gray-200 bg-gray-50 text-gray-700'}`}
                >
                  Mất vé
                </button>
              </div>
            <div className="grid grid-cols-3 gap-2">
              {EXIT_GATES.map((g) => (
                <button
                  key={g.id}
                  type="button"
                  onClick={() => setExitGate(g.id)}
                  className={`rounded-lg border px-2 py-2 text-xs font-semibold transition ${
                    exitGate === g.id ? 'border-blue-500 bg-blue-50 text-blue-700' : 'border-gray-200 bg-gray-50 text-gray-700'
                  }`}
                >
                  <div>{g.label}</div>
                  <div className="text-[10px] text-gray-500">{g.id}</div>
                </button>
              ))}
            </div>
            <button
              onClick={handleCheckOut}
              disabled={checkingOut}
              className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2.5 rounded-lg transition disabled:opacity-60"
            >
              {checkingOut ? 'Đang kiểm tra...' : 'Kiểm tra'}
            </button>
            {checkoutInfo && (
              <div className="mt-3 p-4 bg-blue-50 rounded-lg space-y-3">
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Biển số:</span>
                  <span className="font-bold text-gray-800">{checkoutInfo.licensePlate}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Phí gửi xe:</span>
                  <span className="font-bold text-red-600 text-lg">{formatCurrency(checkoutInfo.amount)} đ</span>
                </div>
                {(checkoutInfo.lostPenalty ?? checkoutInfo.LostPenalty ?? 0) > 0 && (
                  <div className="text-xs bg-amber-50 border border-amber-200 text-amber-800 rounded-lg p-2 space-y-1">
                    <div className="flex justify-between"><span>Phí cơ bản</span><span className="font-semibold">{formatCurrency(checkoutInfo.baseAmount ?? checkoutInfo.BaseAmount ?? 0)} đ</span></div>
                    <div className="flex justify-between"><span>Phụ thu mất vé</span><span className="font-semibold">{formatCurrency(checkoutInfo.lostPenalty ?? checkoutInfo.LostPenalty ?? 0)} đ</span></div>
                    <div className="flex justify-between border-t border-amber-200 pt-1"><span>Tổng thanh toán</span><span className="font-bold">{formatCurrency(checkoutInfo.amount)} đ</span></div>
                  </div>
                )}
                {checkoutInfo.amount <= 0 && (
                  <div className="text-xs text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-lg p-2">
                    Vé miễn phí / 0đ: không cần QR thanh toán.
                  </div>
                )}
                {paymentQr?.qrContent && (
                  <div className="flex items-center justify-between bg-white rounded-lg p-3 border border-blue-100">
                    <div className="text-xs text-gray-700 space-y-1">
                      <div className="font-semibold">Đã sẵn sàng mã QR</div>
                      <div>Mã giao dịch: <span className="font-mono">{paymentQr.transactionCode}</span></div>
                      {paymentQr.providerLog && <div className="text-gray-500">{paymentQr.providerLog}</div>}
                    </div>
                    <button
                      type="button"
                      onClick={() => setQrModal({ content: paymentQr.qrContent, title: 'Quét QR thanh toán', subtitle: `Mã giao dịch: ${paymentQr.transactionCode}` })}
                      className="text-xs font-semibold text-blue-600 hover:text-blue-800 underline"
                    >
                      Mở QR
                    </button>
                  </div>
                )}
                <button
                  onClick={handleRequestQr}
                  disabled={requestingQr}
                  className="w-full bg-amber-500 hover:bg-amber-600 text-white font-semibold py-2.5 rounded-lg transition mt-2 disabled:opacity-60 flex items-center justify-center gap-2"
                >
                  {requestingQr ? 'Đang lấy mã QR...' : (<><QrCode size={18} /> Hiển thị mã QR thanh toán</>)}
                </button>
                {paymentQr?.transactionCode && (
                  <div className="grid grid-cols-2 gap-2">
                    <button
                      onClick={() => handleGatewayCallback('SUCCESS')}
                      disabled={confirmingGateway}
                      className="bg-green-600 hover:bg-green-700 text-white font-semibold py-2 rounded-lg transition disabled:opacity-60"
                    >
                      {confirmingGateway ? 'Đang ghi nhận...' : 'Đã nhận log thành công'}
                    </button>
                    <button
                      onClick={() => handleGatewayCallback('FAILED')}
                      disabled={confirmingGateway}
                      className="bg-red-500 hover:bg-red-600 text-white font-semibold py-2 rounded-lg transition disabled:opacity-60"
                    >
                      Ghi nhận log thất bại
                    </button>
                  </div>
                )}
                {checkoutInfo.amount <= 0 && (
                  <button
                    onClick={handleFreePayment}
                    disabled={confirmingGateway}
                    className="w-full bg-green-600 hover:bg-green-700 text-white font-semibold py-2.5 rounded-lg transition disabled:opacity-60"
                  >
                    {confirmingGateway ? 'Đang xử lý...' : 'Hoàn tất miễn phí & mở cổng'}
                  </button>
                )}
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
                  <tr 
                    key={s.sessionId} 
                    className="border-b border-gray-50 hover:bg-blue-50 cursor-pointer transition-colors"
                    onClick={() => {
                        setPlateOut(s.vehicle?.licensePlate || '')
                        const tId = s.ticket?.ticketId || ''
                        const card = s.ticket?.cardId || s.cardId || ''
                        
                        // Smart auto-detect mode
                        const isMonthly = s.ticket?.ticketType === 'Monthly' || tId.startsWith('M-') || (tId.length === 16 && !tId.includes('-')) // Hex RFID
                        
                        if (isMonthly) {
                            setIsMonthlyCheckout(true)
                            setCardOut(card || tId) // Use ticketId as card if card is missing (for RFID simulation)
                            setTicketIdOut('')
                        } else {
                            setIsMonthlyCheckout(false)
                            setTicketIdOut(tId)
                            setCardOut('')
                        }
                        setIsLostTicket(false)
                        
                        // Highlight UI feedback
                        addLog(`👉 Đã chọn xe: ${s.vehicle?.licensePlate}`)
                    }}
                    title="Click để điền nhanh vào form Checkout"
                  >
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
