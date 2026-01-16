import { useState, useEffect, useMemo } from 'react'
import { useOutletContext } from 'react-router-dom'
import axios from 'axios'
import { Car, Ticket, Clock, LogIn, Send, Printer, User } from 'lucide-react'
import { API_BASE } from '../config/api'
import { VEHICLE_OPTIONS } from '../config/gates'

const formatTime = (v) => new Date(v).toLocaleTimeString('vi-VN')

export default function CheckIn() {
  const { user, logs, addLog } = useOutletContext()
  const [zoneStatus, setZoneStatus] = useState([])
  
  // Form State
  const [plateIn, setPlateIn] = useState('')
  const [cardIn, setCardIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR') // Default, will sync with user group
  const [isMonthlyCheckIn, setIsMonthlyCheckIn] = useState(false)
  const [loading, setLoading] = useState(false)

  const currentGate = user?.gateId || 'GATE-IN-CAR-01'
  const vehicleGroup = user?.gateVehicleGroup || 'CAR'
  
  const vehicleChoices = useMemo(
    () => VEHICLE_OPTIONS[vehicleGroup] || VEHICLE_OPTIONS.CAR,
    [vehicleGroup]
  )

  useEffect(() => {
    // Set default vehicle type based on gate group
    setTypeIn(vehicleChoices[0]?.value || 'CAR')
  }, [vehicleChoices])

  // Removed local addLog definition as it comes from context now

  const fetchZoneStatus = async () => {
    try {
      const res = await axios.get(`${API_BASE}/Zones/status?gateId=${currentGate}`)
      setZoneStatus(res.data)
    } catch (err) {
      console.error('Error fetching zone status:', err)
    }
  }

  useEffect(() => {
    fetchZoneStatus()
    const interval = setInterval(fetchZoneStatus, 3000)
    return () => clearInterval(interval)
  }, [currentGate])

  const triggerPrint = (html, fileName = 'ticket.html') => {
    if (!html) return
    const blob = new Blob([html], { type: 'text/html' })
    const url = URL.createObjectURL(blob)
    const win = window.open(url, '_blank')
    if (!win) return alert('Trình duyệt đã chặn cửa sổ in. Hãy cho phép popup để in vé.')
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

  const handleCheckIn = async () => {
    if (!plateIn) return alert('Vui lòng nhập biển số xe')
    if (isMonthlyCheckIn && !cardIn) return alert('Vé tháng yêu cầu quét thẻ (CardId)')
    
    // Zone capacity check (Client-side heuristic, backend is authoritative)
    const isFull = zoneStatus.length > 0 && zoneStatus.every(z => z.isFull)
    if (isFull) {
        if (!confirm('Cảnh báo: Bãi có thể đã đầy. Bạn có muốn tiếp tục thử check-in không?')) return;
    }

    setLoading(true)
    try {
      const payload = {
        plateNumber: plateIn,
        vehicleType: typeIn,
        gateId: currentGate,
        cardId: isMonthlyCheckIn ? cardIn : null
      }
      
      const res = await axios.post(`${API_BASE}/CheckIn`, payload)
      
      addLog(`✅ Check-in THÀNH CÔNG: ${plateIn} (Vé: ${res.data.ticketId})`)
      const shouldPrint = res.data.shouldPrintTicket ?? res.data.ShouldPrintTicket
      if (shouldPrint) {
        triggerPrint(res.data.printHtml || res.data.PrintHtml, 'ticket.html')
      }
      
      // Reset form
      setPlateIn('')
      setCardIn('')
    } catch (err) {
      const msg = err.response?.data?.error || err.message || 'Lỗi không xác định'
      addLog(`❌ Check-in THẤT BẠI: ${msg}`)
    } finally {
      setLoading(false)
      fetchZoneStatus()
    }
  }

  return (
    <div className="grid lg:grid-cols-3 gap-6 h-[calc(100vh-100px)]">
        {/* Left Column: Interactions & Form */}
        <div className="lg:col-span-1 space-y-6">
            <div className="bg-white rounded-xl shadow-lg border border-gray-100 p-6 flex flex-col h-full">
                <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-3">
                        <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center text-green-600 shadow-sm">
                            <LogIn size={24} />
                        </div>
                        <div>
                            <h2 className="text-xl font-bold text-gray-800">Cổng Vào</h2>
                            <p className="text-sm text-gray-500 font-medium">{currentGate}</p>
                        </div>
                    </div>
                    <span className="px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-bold animate-pulse">
                        ONLINE
                    </span>
                </div>

                <div className="flex-1 space-y-5">
                    {/* Vehicle Type Selection */}
                    <div>
                        <label className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-2 block">Loại xe</label>
                        <div className="grid grid-cols-2 gap-3">
                            {vehicleChoices.map((opt) => (
                                <button
                                key={opt.value}
                                onClick={() => setTypeIn(opt.value)}
                                className={`flex flex-col items-center justify-center gap-1 p-3 rounded-lg border-2 transition-all ${
                                    typeIn === opt.value 
                                    ? 'border-green-500 bg-green-50 text-green-700 shadow-md transform scale-[1.02]' 
                                    : 'border-gray-100 bg-gray-50 text-gray-600 hover:border-green-200'
                                }`}
                                >
                                    <span className="text-2xl">{opt.icon}</span>
                                    <span className="text-xs font-bold">{opt.label}</span>
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Input Fields */}
                    <div className="space-y-4">
                        <div>
                             <label className="text-xs font-bold text-gray-400 uppercase tracking-wider mb-1 block">Biển số xe</label>
                            <div className="flex items-center gap-3 bg-gray-50 rounded-xl px-4 py-3 border focus-within:border-green-500 focus-within:ring-2 focus-within:ring-green-200 transition-all">
                                <Car size={20} className="text-gray-400" />
                                <input
                                    className="flex-1 bg-transparent outline-none text-lg font-bold uppercase tracking-widest text-gray-800 placeholder-gray-300"
                                    placeholder="30A-123.45"
                                    value={plateIn}
                                    onChange={(e) => setPlateIn(e.target.value.toUpperCase())}
                                    onKeyDown={(e) => e.key === 'Enter' && handleCheckIn()}
                                    autoFocus
                                />
                            </div>
                        </div>

                         {/* Mode Toggle */}
                        <div className="flex bg-gray-100 p-1 rounded-lg">
                            <button
                                onClick={() => setIsMonthlyCheckIn(false)}
                                className={`flex-1 py-1.5 text-xs font-bold rounded-md transition-all ${!isMonthlyCheckIn ? 'bg-white text-green-700 shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
                            >
                                Vé Lượt (Khách vãng lai)
                            </button>
                            <button
                                onClick={() => setIsMonthlyCheckIn(true)}
                                className={`flex-1 py-1.5 text-xs font-bold rounded-md transition-all ${isMonthlyCheckIn ? 'bg-indigo-600 text-white shadow-sm' : 'text-gray-500 hover:text-gray-700'}`}
                            >
                                Vé Tháng (Thành Viên)
                            </button>
                        </div>

                        {/* Card Input (Conditional) */}
                        <div className={`transition-all duration-300 overflow-hidden ${isMonthlyCheckIn ? 'max-h-20 opacity-100' : 'max-h-0 opacity-0'}`}>
                             <div className="flex items-center gap-3 bg-indigo-50 rounded-xl px-4 py-3 border border-indigo-100 focus-within:border-indigo-500 transition-all">
                                <Ticket size={20} className="text-indigo-400" />
                                <input
                                    className="flex-1 bg-transparent outline-none text-sm font-medium text-indigo-900 placeholder-indigo-300"
                                    placeholder="Quẹt thẻ thành viên..."
                                    value={cardIn}
                                    onChange={(e) => setCardIn(e.target.value)}
                                    // Make sure not to conflict with Enter key on plate input if focused here
                                    onKeyDown={(e) => e.key === 'Enter' && handleCheckIn()}
                                />
                            </div>
                        </div>
                    </div>
                </div>

                <button
                    onClick={handleCheckIn}
                    disabled={loading}
                    className={`mt-6 w-full py-4 rounded-xl font-bold text-white shadow-lg transition-transform active:scale-95 flex items-center justify-center gap-2
                        ${loading ? 'bg-gray-400 cursor-not-allowed' : 'bg-gradient-to-r from-green-600 to-green-500 hover:from-green-500 hover:to-green-400'}
                    `}
                >
                    {loading ? 'Đang xử lý...' : <><Send size={20} /> XÁC NHẬN VÀO BẾN</>}
                </button>
            </div>
        </div>

        {/* Right Column: Status & Logs */}
        <div className="lg:col-span-2 space-y-6 flex flex-col h-full">
             {/* 1. Zone Status Panel */}
            <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
                <div className="flex items-center justify-between mb-4">
                    <h3 className="font-bold text-gray-800 flex items-center gap-2">
                        <div className="w-2 h-6 bg-blue-500 rounded-full"></div>
                        Tình trạng bãi xe
                    </h3>
                    <span className="text-sm text-gray-500">
                      Tổng chỗ trống: <strong className="text-green-600 text-lg">{zoneStatus.reduce((acc, z) => acc + (z.available || 0), 0)}</strong>
                    </span>
                 </div>
                 
                 <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                 {(() => {
                    const visibleZones = zoneStatus.filter(z => {
                        // Quick filter: Show zones relevant to current vehicle selection roughly
                        // Or just show all? Showing all is safer for overview.
                        return true
                    })

                    if (visibleZones.length === 0) return <div className="col-span-3 text-center py-8 text-gray-400 italic">Đang tải dữ liệu khu vực...</div>

                    return visibleZones.map(z => {
                        const occupancy = z.capacity > 0 ? ((z.active / z.capacity) * 100) : 0
                        const isFull = z.isFull || occupancy >= 100
                        
                        return (
                            <div key={z.zoneId} className={`relative overflow-hidden rounded-xl border p-4 group transition-all duration-300 ${isFull ? 'bg-red-50 border-red-200' : 'bg-white border-gray-100 hover:border-blue-200 hover:shadow-md'}`}>
                                <div className="flex justify-between items-start mb-2 relative z-10">
                                    <span className="text-xs font-bold text-gray-500 uppercase">{z.name}</span>
                                    {isFull && <span className="text-[10px] font-black bg-red-500 text-white px-2 py-0.5 rounded-full">FULL</span>}
                                </div>
                                
                                <div className="flex items-end gap-1 mb-3 relative z-10">
                                     <span className={`text-3xl font-black ${isFull ? 'text-red-600' : 'text-gray-800'}`}>{z.available}</span>
                                     <span className="text-xs text-gray-400 font-medium mb-1">trống</span>
                                </div>
                                
                                {/* Progress Bar */}
                                <div className="w-full bg-gray-100 rounded-full h-2 overflow-hidden relative z-10">
                                     <div 
                                        className={`h-full rounded-full transition-all duration-1000 ${isFull ? 'bg-red-500' : occupancy > 80 ? 'bg-amber-500' : 'bg-emerald-500'}`} 
                                        style={{ width: `${occupancy}%` }}
                                     ></div>
                                </div>
                                
                                <div className="absolute right-0 top-0 h-full w-1/3 bg-gradient-to-l from-gray-50 to-transparent opacity-0 group-hover:opacity-100 transition-opacity"></div>
                            </div>
                        )
                    })
                 })()}
                 </div>
            </div>

            {/* 2. Logs Panel - Filling remaining height */}
            <div className="bg-gray-900 rounded-xl p-0 flex-1 flex flex-col overflow-hidden shadow-inner border border-gray-800">
                <div className="p-4 border-b border-gray-800 flex items-center gap-2">
                    <Clock size={16} className="text-green-500" />
                    <span className="text-green-500 font-mono font-bold text-sm">SYSTEM LOGS - REALTIME</span>
                </div>
                <div className="flex-1 overflow-auto p-4 font-mono text-xs space-y-2">
                    {logs.length === 0 && <div className="text-gray-600 italic">Hệ thống sẵn sàng...</div>}
                    {logs.map((log, idx) => (
                        <div key={idx} className="text-gray-300 border-l-2 border-gray-700 pl-3 py-1 hover:bg-gray-800 transition-colors">
                            {log}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    </div>
  )
}
