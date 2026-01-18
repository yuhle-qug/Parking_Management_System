import { useState, useEffect } from 'react'
import { useOutletContext } from 'react-router-dom'
import axios from 'axios'
import { LogOut, CreditCard, Search, Banknote, CheckCircle, AlertTriangle, Clock, QrCode, Ticket, Car, X } from 'lucide-react'
import { API_BASE } from '../config/api'
import { VEHICLE_OPTIONS } from '../config/gates'

const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')

export default function CheckOut() {
  const { user, logs, addLog } = useOutletContext()
  
  // Checkout State
  const [plateOut, setPlateOut] = useState('')
  const [ticketIdOut, setTicketIdOut] = useState('')
  const [cardOut, setCardOut] = useState('')
  const [isMonthlyCheckout, setIsMonthlyCheckout] = useState(false)
  const [isLostTicket, setIsLostTicket] = useState(false)
  
  const [checkingOut, setCheckingOut] = useState(false)
  const [checkoutInfo, setCheckoutInfo] = useState(null)
  
  // Payment State
  const [requestingQr, setRequestingQr] = useState(false)
  const [paymentQr, setPaymentQr] = useState(null)
  const [confirmingGateway, setConfirmingGateway] = useState(false)
  const [qrModal, setQrModal] = useState(null)

  const currentGate = user?.gateId || 'GATE-OUT-CAR-01'
  const baseAmount = checkoutInfo?.baseAmount ?? checkoutInfo?.BaseAmount ?? 0
  const lostPenalty = checkoutInfo?.lostPenalty ?? checkoutInfo?.LostPenalty ?? 0
  const isLostTicketResult = checkoutInfo?.isLostTicket ?? checkoutInfo?.IsLostTicket ?? false

  const handleCheckOut = async () => {
    if (!plateOut) return alert('Nhập biển số xe')
    if (isMonthlyCheckout && !cardOut) return alert('Vé tháng cần thẻ (cardId) khi ra')
    if (!isMonthlyCheckout && !isLostTicket && !ticketIdOut) return alert('Vé lượt cần nhập mã vé giấy')
    
    setCheckingOut(true)
    let reportWindow = null
    try {
      reportWindow = isLostTicket ? window.open('', '_blank') : null
      let url = `${API_BASE}/CheckOut`
      let payload = {};

      if (isLostTicket) {
        url = `${API_BASE}/CheckOut/lost-ticket`
        payload = { plateNumber: plateOut, gateId: currentGate, printReport: true }
      } else if (isMonthlyCheckout) {
        payload = { ticketIdOrPlate: cardOut, gateId: currentGate, plateNumber: plateOut, cardId: cardOut }
      } else {
        payload = { ticketIdOrPlate: ticketIdOut, gateId: currentGate, plateNumber: plateOut }
      }

      const res = await axios.post(url, payload)
      
      // --- VEHICLE TYPE VALIDATION ---
      const vehicleGroup = user?.gateVehicleGroup || 'CAR'
      const allowedOptions = VEHICLE_OPTIONS[vehicleGroup] || []
      const allowedTypes = allowedOptions.map(o => o.value)
      
      // If we strictly want to block BICYCLE at CAR gate:
      if (res.data.vehicleType && !allowedTypes.includes(res.data.vehicleType)) {
          const confirmMsg = `❌ CẢNH BÁO SAI LUỒNG:\n\nXe này là: ${res.data.vehicleType}\nCổng hiện tại dành cho: ${vehicleGroup}\n\nBạn có chắc chắn muốn tiếp tục cho xe ra không?`
          if (!window.confirm(confirmMsg)) {
              addLog(`⚠️ Đã chặn xe ${res.data.vehicleType} tại cổng ${vehicleGroup} (${res.data.licensePlate})`)
              setCheckingOut(false)
              return
          }
           addLog(`⚠️ Cảnh báo: Cho phép xe ${res.data.vehicleType} ra sai luồng tại cổng ${vehicleGroup}`)
      }
      // -------------------------------

      setCheckoutInfo(res.data)
      setPaymentQr(null)
      addLog(`🔍 Kiểm tra (${currentGate}): ${res.data.licensePlate} - Phí: ${formatCurrency(res.data.amount)}đ`)

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
      addLog('❌ Lỗi: ' + (err.response?.data?.error || 'Không tìm thấy xe'))
    } finally {
      setCheckingOut(false)
    }
  }

  const handleRequestQr = async () => {
    if (!checkoutInfo) return
    if (checkoutInfo.amount <= 0) return handleFreePayment() // Should handle this case
    
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
      
      const newQrData = {
        qrContent,
        transactionCode,
        providerLog: res.data.providerLog || res.data.ProviderLog,
        status: res.data.status || res.data.Status
      }
      setPaymentQr(newQrData)
      setQrModal({ content: qrContent, title: 'Quét QR thanh toán', subtitle: `Mã GD: ${transactionCode}` })

      addLog(`💳 Tạo QR thành công (${transactionCode})`)
    } catch (err) {
      addLog('❌ Tạo QR thất bại: ' + (err.response?.data?.message || err.message))
    } finally {
      setRequestingQr(false)
    }
  }

  const handleGatewayCallback = async (status = 'SUCCESS') => {
    if (!checkoutInfo || !paymentQr?.transactionCode) return
    setConfirmingGateway(true)
    try {
      await axios.post(`${API_BASE}/Payment/callback`, {
        sessionId: checkoutInfo.sessionId,
        transactionCode: paymentQr.transactionCode,
        status,
        providerLog: `Gateway ${status} confirmed by Attendant`,
        exitGateId: currentGate
      })

      addLog(`✅ Thanh toán ${status}: Đã mở cổng cho xe.`)
      resetFlow()
    } catch (err) {
       addLog('❌ Xác nhận thanh toán lỗi: ' + (err.response?.data?.message || err.message))
    } finally {
      setConfirmingGateway(false)
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

      addLog('✅ Xe ra miễn phí: Đã mở cổng.')
      resetFlow()
    } catch (err) {
      addLog('❌ Lỗi xử lý miễn phí: ' + (err.response?.data?.message || err.message))
    } finally {
      setConfirmingGateway(false)
    }
  }

  const resetFlow = () => {
    setCheckoutInfo(null)
    setPaymentQr(null)
    setPlateOut('')
    setCardOut('')
    setTicketIdOut('')
    setQrModal(null)
  }

  const renderQrModal = () => {
    if (!qrModal?.content) return null
    return (
      <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4">
        <div className="bg-white rounded-xl shadow-2xl p-6 w-full max-w-sm space-y-4 border border-gray-100 animate-in fade-in zoom-in duration-200">
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-xs text-gray-500">{qrModal.subtitle}</div>
              <div className="text-lg font-bold text-gray-800">{qrModal.title}</div>
            </div>
            <button onClick={() => setQrModal(null)} className="text-gray-400 hover:text-gray-800 transition-colors">
              <X size={24} />
            </button>
          </div>
          <div className="flex flex-col items-center gap-4 py-4 bg-gray-50 rounded-lg">
            <img
              className="w-64 h-64 rounded-lg border-2 border-white shadow-sm mix-blend-multiply"
              alt="QR"
              src={`https://api.qrserver.com/v1/create-qr-code/?size=400x400&data=${encodeURIComponent(qrModal.content)}`}
            />
          </div>
          <button onClick={() => setQrModal(null)} className="w-full py-3 bg-gray-100 font-semibold rounded-lg hover:bg-gray-200 transition-colors text-sm">
            Đóng
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="grid lg:grid-cols-2 gap-6 h-[calc(100vh-100px)]">
        {renderQrModal()}
        
        {/* Left Column: Interaction */}
        <div className="space-y-6 flex flex-col h-full">
            <div className="bg-white rounded-xl shadow-lg border border-gray-100 p-6 flex-1 flex flex-col">
                <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-3">
                        <div className="w-12 h-12 rounded-xl bg-orange-100 flex items-center justify-center text-orange-600 shadow-sm">
                            <LogOut size={24} />
                        </div>
                        <div>
                            <h2 className="text-xl font-bold text-gray-800">Cổng Ra</h2>
                            <p className="text-sm text-gray-500 font-medium">{currentGate}</p>
                        </div>
                    </div>
                </div>

                {!checkoutInfo ? (
                    /* Step 1: Lookup Form */
                    <div className="space-y-5 animate-in fade-in slide-in-from-left-4 duration-300">
                        <div className="p-4 bg-blue-50 rounded-xl border border-blue-100 text-blue-800 text-sm mb-4">
                            Sẵn sàng kiểm tra xe ra. Vui lòng nhập thông tin.
                        </div>

                        <div className="space-y-4">
                            <div>
                                <label className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-1 block">Biển số xe</label>
                                <div className="flex items-center gap-3 bg-gray-50 rounded-xl px-4 py-3 border focus-within:border-orange-500 transition-all">
                                    <Car size={20} className="text-gray-400" />
                                    <input
                                        className="flex-1 bg-transparent outline-none text-lg font-bold uppercase tracking-widest text-gray-800 placeholder-gray-300"
                                        placeholder="30A-..."
                                        value={plateOut}
                                        onChange={(e) => setPlateOut(e.target.value.toUpperCase())}
                                        autoFocus
                                    />
                                </div>
                            </div>

                            <div className="flex bg-gray-100 p-1 rounded-lg">
                                <button
                                    onClick={() => { setIsMonthlyCheckout(false); setIsLostTicket(false); }}
                                    className={`flex-1 py-1.5 text-xs font-bold rounded-md transition-all ${(!isMonthlyCheckout && !isLostTicket) ? 'bg-white text-orange-700 shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
                                >
                                    Vé Lượt
                                </button>
                                <button
                                    onClick={() => { setIsMonthlyCheckout(true); setIsLostTicket(false); }}
                                    className={`flex-1 py-1.5 text-xs font-bold rounded-md transition-all ${isMonthlyCheckout ? 'bg-indigo-600 text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
                                >
                                    Vé Tháng
                                </button>
                                <button
                                    onClick={() => { setIsLostTicket(true); setIsMonthlyCheckout(false); }}
                                    className={`flex-1 py-1.5 text-xs font-bold rounded-md transition-all ${isLostTicket ? 'bg-amber-500 text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
                                >
                                    Mất Vé
                                </button>
                            </div>

                            {/* Conditional Inputs */}
                            {!isMonthlyCheckout && !isLostTicket && (
                                <div className="animate-in fade-in zoom-in duration-200">
                                   <label className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-1 block">Mã vé giấy</label>
                                    <div className="flex items-center gap-3 bg-gray-50 rounded-xl px-4 py-3 border focus-within:border-orange-500 transition-all">
                                        <Ticket size={20} className="text-gray-400" />
                                        <input
                                            className="flex-1 bg-transparent outline-none text-sm font-medium"
                                            placeholder="Nhập mã vé..."
                                            value={ticketIdOut}
                                            onChange={(e) => setTicketIdOut(e.target.value.toUpperCase())}
                                            onKeyDown={(e) => e.key === 'Enter' && handleCheckOut()}
                                        />
                                    </div>
                                </div>
                            )}

                            {isMonthlyCheckout && (
                                 <div className="animate-in fade-in zoom-in duration-200">
                                   <label className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-1 block">Thẻ Thành Viên</label>
                                    <div className="flex items-center gap-3 bg-indigo-50 rounded-xl px-4 py-3 border border-indigo-100 focus-within:border-indigo-500 transition-all">
                                        <QrCode size={20} className="text-indigo-400" />
                                        <input
                                            className="flex-1 bg-transparent outline-none text-sm font-medium text-indigo-900"
                                            placeholder="Quẹt thẻ..."
                                            value={cardOut}
                                            onChange={(e) => setCardOut(e.target.value)}
                                            onKeyDown={(e) => e.key === 'Enter' && handleCheckOut()}
                                        />
                                    </div>
                                </div>
                            )}
                        </div>

                        <button
                            onClick={handleCheckOut}
                            disabled={checkingOut}
                            className="w-full mt-4 bg-orange-600 hover:bg-orange-700 text-white font-bold py-4 rounded-xl shadow-lg transition-transform active:scale-95 flex items-center justify-center gap-2"
                        >
                            {checkingOut ? 'Đang kiểm tra...' : <><Search size={20} /> KIỂM TRA XE RA</>}
                        </button>
                    </div>
                ) : (
                    /* Step 2: Payment & Result */
                    <div className="flex-1 flex flex-col animate-in fade-in slide-in-from-right-4 duration-300">
                        <div className="p-4 bg-gray-50 rounded-xl border border-gray-200 mb-6 relative overflow-hidden">
                             <div className="absolute top-0 right-0 p-2 opacity-10">
                                <Car size={100} />
                             </div>
                             <div className="relative z-10 space-y-2">
                                <div className="flex justify-between items-end border-b border-gray-200 pb-2">
                                     <span className="text-gray-500 text-sm">Biển số xe</span>
                                     <span className="text-2xl font-black text-gray-800">{checkoutInfo.licensePlate}</span>
                                </div>
                                <div className="flex justify-between items-end pt-2">
                                     <span className="text-gray-500 text-sm">Tổng thanh toán</span>
                                     <span className="text-3xl font-black text-red-600 tracking-tight">{formatCurrency(checkoutInfo.amount)} đ</span>
                                </div>
                                {isLostTicketResult && (
                                  <div className="mt-3 space-y-1 text-sm text-gray-600">
                                    <div className="flex justify-between">
                                      <span>Phí gửi xe</span>
                                      <span className="font-semibold text-gray-800">{formatCurrency(baseAmount)} đ</span>
                                    </div>
                                    <div className="flex justify-between">
                                      <span>Phí mất vé</span>
                                      <span className="font-semibold text-gray-800">{formatCurrency(lostPenalty)} đ</span>
                                    </div>
                                  </div>
                                )}
                             </div>
                        </div>

                        {/* Actions */}
                        <div className="flex-1 space-y-3">
                             {checkoutInfo.amount <= 0 ? (
                                 <div className="flex flex-col items-center justify-center h-40 bg-green-50 rounded-xl border border-green-200 text-center p-4">
                                     <CheckCircle size={48} className="text-green-500 mb-2" />
                                     <h3 className="font-bold text-green-800">Miễn Phí / Vé Tháng</h3>
                                     <p className="text-sm text-green-600 mb-4">Xe đủ điều kiện ra miễn phí</p>
                                     <button 
                                        onClick={handleFreePayment}
                                        disabled={confirmingGateway}
                                        className="bg-green-600 text-white px-6 py-2 rounded-lg font-bold hover:bg-green-700 transition shadow-md"
                                     >
                                         {confirmingGateway ? 'Đang xử lý...' : 'MỞ CỔNG NGAY'}
                                     </button>
                                 </div>
                             ) : (
                                 <div className="space-y-3">
                                     {/* QR Button */}
                                     {(!paymentQr || paymentQr.status === 'PENDING') && (
                                         <button
                                            onClick={handleRequestQr}
                                            disabled={requestingQr}
                                            className="w-full py-4 bg-blue-600 text-white rounded-xl font-bold text-lg shadow-lg hover:bg-blue-700 transition flex items-center justify-center gap-2"
                                         >
                                             {requestingQr ? 'Đang tạo mã...' : <><QrCode size={20} /> THANH TOÁN QR</>}
                                         </button>
                                     )}

                                     {/* Confirm Manual Logic */}
                                     {paymentQr?.transactionCode && (
                                         <div className="grid grid-cols-2 gap-3 mt-4">
                                             <button
                                                onClick={() => handleGatewayCallback('SUCCESS')}
                                                disabled={confirmingGateway}
                                                className="py-3 bg-green-600 text-white rounded-xl font-bold shadow-md hover:bg-green-700"
                                             >
                                                Đã nhận tiền
                                             </button>
                                             <button
                                                onClick={() => handleGatewayCallback('FAILED')}
                                                disabled={confirmingGateway}
                                                className="py-3 bg-red-500 text-white rounded-xl font-bold shadow-md hover:bg-red-600"
                                             >
                                                Hủy / Thất bại
                                             </button>
                                         </div>
                                     )}
                                 </div>
                             )}
                        </div>

                        <button 
                            onClick={resetFlow}
                            className="mt-6 py-3 border-2 border-gray-200 text-gray-500 font-bold rounded-xl hover:border-gray-400 hover:text-gray-700 transition"
                        >
                            Quay lại (Hủy)
                        </button>
                    </div>
                )}
            </div>
        </div>

        {/* Right Column: Logs */}
        <div className="bg-gray-900 rounded-xl p-0 flex flex-col overflow-hidden shadow-inner border border-gray-800 h-full">
            <div className="p-4 border-b border-gray-800 flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <Clock size={16} className="text-orange-500" />
                    <span className="text-orange-500 font-mono font-bold text-sm">CHECKOUT LOGS</span>
                </div>
                <div className="text-xs text-gray-600 font-mono">LIVE</div>
            </div>
            <div className="flex-1 overflow-auto p-4 font-mono text-xs space-y-2">
                {logs.length === 0 && <div className="text-gray-600 italic">Chờ lệnh kiểm tra xe...</div>}
                {logs.map((log, idx) => (
                    <div key={idx} className="text-gray-300 border-l-2 border-orange-700 pl-3 py-1 hover:bg-gray-800 transition-colors">
                        {log}
                    </div>
                ))}
            </div>
        </div>
    </div>
  )
}
