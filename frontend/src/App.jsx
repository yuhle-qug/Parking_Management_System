import { useState } from 'react'
import axios from 'axios'
import './App.css'

// Cấu hình địa chỉ Backend (điền đúng port backend của bạn; mặc định http://localhost:5166)
const API_BASE_URL = 'http://localhost:5166/api/Parking'

function App() {
  const [logs, setLogs] = useState([])

  // State cho Check-in
  const [plateIn, setPlateIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR')

  // State cho Check-out
  const [plateOut, setPlateOut] = useState('')
  const [checkoutInfo, setCheckoutInfo] = useState(null)

  // State cho Thanh toán
  const [paymentSessionId, setPaymentSessionId] = useState('')
  const [amount, setAmount] = useState(0)

  // Ghi log ra màn hình
  const addLog = (msg) => setLogs((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev])

  // --- 1. XỬ LÝ CHECK-IN ---
  const handleCheckIn = async () => {
    try {
      const payload = { plateNumber: plateIn, vehicleType: typeIn, gateId: 'GATE-01' }
      const res = await axios.post(`${API_BASE_URL}/check-in`, payload)
      addLog(`✅ Check-in thành công! Xe: ${plateIn} - Vé: ${res.data.ticketId}`)
    } catch (err) {
      addLog(`❌ Lỗi Check-in: ${err.response?.data?.error || err.message}`)
    }
  }

  // --- 2. XỬ LÝ YÊU CẦU CHECK-OUT ---
  const handleCheckOutRequest = async () => {
    try {
      const payload = { ticketIdOrPlate: plateOut, gateId: 'GATE-02' }
      const res = await axios.post(`${API_BASE_URL}/check-out`, payload)

      setCheckoutInfo(res.data)
      setPaymentSessionId(res.data.sessionId)
      setAmount(res.data.amount)

      addLog(`ℹ️ Xe ${res.data.licensePlate} muốn ra. Phí: ${res.data.amount.toLocaleString()} VNĐ`)
    } catch (err) {
      addLog(`❌ Lỗi tìm xe: ${err.response?.data?.error || err.message}`)
    }
  }

  // --- 3. XỬ LÝ THANH TOÁN ---
  const handlePayment = async () => {
    try {
      const payload = { sessionId: paymentSessionId, amount: amount }
      const res = await axios.post(`${API_BASE_URL}/pay`, payload)
      addLog(`💰 ${res.data.message}`)
      setCheckoutInfo(null) // Reset form
    } catch (err) {
      addLog(`❌ Thanh toán thất bại: ${err.response?.data?.message || err.message}`)
    }
  }

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial' }}>
      <h1>🚗 Hệ thống Quản lý Bãi xe (React Client)</h1>

      <div style={{ display: 'flex', gap: '20px' }}>
        {/* PANEL 1: CỔNG VÀO */}
        <div style={{ border: '1px solid #ccc', padding: '15px', borderRadius: '8px', flex: 1 }}>
          <h3>⬇️ Cổng Vào (Check-In)</h3>
          <div>
            <label>Biển số:</label>
            <input value={plateIn} onChange={(e) => setPlateIn(e.target.value)} placeholder="VD: 30A-12345" />
          </div>
          <div style={{ marginTop: '10px' }}>
            <label>Loại xe:</label>
            <select value={typeIn} onChange={(e) => setTypeIn(e.target.value)}>
              <option value="CAR">Ô tô</option>
              <option value="MOTORBIKE">Xe máy</option>
              <option value="ELECTRIC_CAR">Ô tô điện (Giảm giá)</option>
            </select>
          </div>
          <button onClick={handleCheckIn} style={{ marginTop: '15px', background: '#4CAF50', color: 'white' }}>
            Mở Cổng Vào
          </button>
        </div>

        {/* PANEL 2: CỔNG RA */}
        <div style={{ border: '1px solid #ccc', padding: '15px', borderRadius: '8px', flex: 1 }}>
          <h3>⬆️ Cổng Ra (Check-Out)</h3>
          <div>
            <label>Nhập Vé / Biển số:</label>
            <input value={plateOut} onChange={(e) => setPlateOut(e.target.value)} placeholder="Tìm xe..." />
          </div>
          <button onClick={handleCheckOutRequest} style={{ marginTop: '15px', background: '#2196F3', color: 'white' }}>
            Kiểm tra Phí
          </button>

          {checkoutInfo && (
            <div style={{ marginTop: '20px', background: '#f9f9f9', padding: '10px' }}>
              <h4>Thanh toán:</h4>
              <p>Biển số: <b>{checkoutInfo.licensePlate}</b></p>
              <p>Số tiền: <b style={{ color: 'red', fontSize: '1.2em' }}>{checkoutInfo.amount.toLocaleString()} VNĐ</b></p>
              <button onClick={handlePayment} style={{ width: '100%', background: '#ff9800', color: 'white' }}>
                💸 Xác nhận Thanh toán & Mở cổng
              </button>
            </div>
          )}
        </div>
      </div>

      {/* PANEL 3: LOG HỆ THỐNG */}
      <div
        style={{
          marginTop: '20px',
          background: '#333',
          color: '#0f0',
          padding: '10px',
          borderRadius: '5px',
          height: '200px',
          overflowY: 'scroll'
        }}
      >
        <strong>📟 System Logs:</strong>
        {logs.map((log, index) => (
          <div key={index}>{log}</div>
        ))}
      </div>
    </div>
  )
}

export default App
