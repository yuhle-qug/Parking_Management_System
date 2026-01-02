import { useState, useEffect } from 'react'
import axios from 'axios'
import './App.css'

// Thay đổi port này nếu backend của bạn khác
const API_BASE_URL = 'http://localhost:5166/api/Parking'

function App() {
  const [logs, setLogs] = useState([])
  const [sessions, setSessions] = useState([]) // [NEW] State lưu danh sách xe

  // State Check-in/out
  const [plateIn, setPlateIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR')
  const [plateOut, setPlateOut] = useState('')
  const [checkoutInfo, setCheckoutInfo] = useState(null)
  const [paymentSessionId, setPaymentSessionId] = useState('')
  const [amount, setAmount] = useState(0)

  const addLog = (msg) => setLogs((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev])

  // [NEW] Hàm lấy danh sách xe
  const fetchSessions = async () => {
    try {
      const res = await axios.get(`${API_BASE_URL}/sessions`)
      setSessions(res.data)
    } catch (err) {
      console.error('Không tải được danh sách xe')
    }
  }

  // [NEW] Tự động tải danh sách mỗi 2 giây
  useEffect(() => {
    fetchSessions()
    const interval = setInterval(fetchSessions, 2000)
    return () => clearInterval(interval)
  }, [])

  const handleCheckIn = async () => {
    try {
      const payload = { plateNumber: plateIn, vehicleType: typeIn, gateId: 'GATE-01' }
      const res = await axios.post(`${API_BASE_URL}/check-in`, payload)
      addLog(`✅ Check-in thành công! Xe: ${plateIn} - Vé: ${res.data.ticketId}`)
      setPlateIn('')
      fetchSessions()
    } catch (err) {
      addLog(`❌ Lỗi Check-in: ${err.response?.data?.error || err.message}`)
    }
  }

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

  const handlePayment = async () => {
    try {
      const payload = { sessionId: paymentSessionId, amount: amount }
      const res = await axios.post(`${API_BASE_URL}/pay`, payload)
      addLog(`💰 ${res.data.message}`)
      setCheckoutInfo(null)
      setPlateOut('')
      fetchSessions()
    } catch (err) {
      addLog(`❌ Thanh toán thất bại: ${err.response?.data?.message || err.message}`)
    }
  }

  return (
    <div style={{ padding: '20px', fontFamily: 'Arial', maxWidth: '1200px', margin: '0 auto' }}>
      <h1 style={{ textAlign: 'center', color: '#333' }}>🚗 HỆ THỐNG QUẢN LÝ BÃI XE THÔNG MINH</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '20px', marginBottom: '20px' }}>
        {/* PANEL TRÁI: ĐIỀU KHIỂN */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          {/* CỔNG VÀO */}
          <div style={cardStyle}>
            <h3 style={{ borderBottom: '2px solid #4CAF50', paddingBottom: '10px' }}>⬇️ Cổng Vào</h3>
            <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
              <input
                style={inputStyle}
                value={plateIn}
                onChange={(e) => setPlateIn(e.target.value)}
                placeholder="Nhập biển số xe..."
              />
              <select style={inputStyle} value={typeIn} onChange={(e) => setTypeIn(e.target.value)}>
                <option value="CAR">Ô tô</option>
                <option value="MOTORBIKE">Xe máy</option>
                <option value="ELECTRIC_CAR">Ô tô điện</option>
              </select>
            </div>
            <button onClick={handleCheckIn} style={{ ...btnStyle, background: '#4CAF50' }}>Mở Cổng Vào</button>
          </div>

          {/* CỔNG RA */}
          <div style={cardStyle}>
            <h3 style={{ borderBottom: '2px solid #2196F3', paddingBottom: '10px' }}>⬆️ Cổng Ra</h3>
            <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
              <input
                style={inputStyle}
                value={plateOut}
                onChange={(e) => setPlateOut(e.target.value)}
                placeholder="Nhập Mã vé hoặc Biển số..."
              />
              <button onClick={handleCheckOutRequest} style={{ ...btnStyle, background: '#2196F3', flex: 1 }}>Kiểm tra</button>
            </div>

            {checkoutInfo && (
              <div style={{ background: '#e3f2fd', padding: '15px', borderRadius: '5px', border: '1px dashed #2196F3' }}>
                <p>Biển số: <strong>{checkoutInfo.licensePlate}</strong></p>
                <p>Thành tiền: <strong style={{ color: 'red', fontSize: '1.4em' }}>{checkoutInfo.amount.toLocaleString()} VNĐ</strong></p>
                <button onClick={handlePayment} style={{ ...btnStyle, background: '#ff9800', width: '100%' }}>
                  💸 Nhận tiền & Mở cổng
                </button>
              </div>
            )}
          </div>

          {/* LOGS */}
          <div style={{ ...cardStyle, background: '#222', color: '#0f0', height: '200px', overflowY: 'auto' }}>
            <strong>📟 System Logs:</strong>
            {logs.map((log, index) => (
              <div key={index} style={{ fontSize: '0.9em', marginTop: '5px' }}>{log}</div>
            ))}
          </div>
        </div>

        {/* PANEL PHẢI: DANH SÁCH XE */}
        <div style={cardStyle}>
          <h3 style={{ borderBottom: '2px solid #9c27b0', paddingBottom: '10px' }}>📋 Danh Sách Xe Trong Bãi ({sessions.length})</h3>
          <div style={{ overflowY: 'auto', maxHeight: '600px' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ background: '#f0f0f0', textAlign: 'left' }}>
                  <th style={thStyle}>Biển số</th>
                  <th style={thStyle}>Loại xe</th>
                  <th style={thStyle}>Mã Vé</th>
                  <th style={thStyle}>Giờ vào</th>
                  <th style={thStyle}>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr key={s.sessionId} style={{ borderBottom: '1px solid #eee' }}>
                    <td style={tdStyle}><strong>{s.vehicle?.licensePlate}</strong></td>
                    <td style={tdStyle}>{s.vehicle?.vehicleType || 'Xe'}</td>
                    <td style={tdStyle}>{s.ticket?.ticketId}</td>
                    <td style={tdStyle}>{new Date(s.entryTime).toLocaleTimeString()}</td>
                    <td style={tdStyle}>
                      <span
                        style={{
                          background: s.status === 'Active' ? '#e8f5e9' : '#fff3e0',
                          color: s.status === 'Active' ? 'green' : 'orange',
                          padding: '3px 8px',
                          borderRadius: '10px',
                          fontSize: '0.8em'
                        }}
                      >
                        {s.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {sessions.length === 0 && (
                  <tr>
                    <td colSpan="5" style={{ textAlign: 'center', padding: '20px' }}>Bãi xe trống</td>
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

// CSS Styles đơn giản
const cardStyle = { background: 'white', padding: '20px', borderRadius: '10px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)' }
const inputStyle = { padding: '10px', borderRadius: '5px', border: '1px solid #ddd', flex: 1 }
const btnStyle = { padding: '10px 20px', border: 'none', borderRadius: '5px', color: 'white', cursor: 'pointer', fontWeight: 'bold' }
const thStyle = { padding: '10px', borderBottom: '2px solid #ddd' }
const tdStyle = { padding: '10px' }

export default App
