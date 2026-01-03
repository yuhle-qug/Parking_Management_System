import { useState, useEffect } from 'react'
import axios from 'axios'
import './App.css'

// Base URL chung (bỏ /Parking)
const API_BASE = 'http://localhost:5166/api'

function App() {
  const [logs, setLogs] = useState([])
  const [sessions, setSessions] = useState([])

  const [plateIn, setPlateIn] = useState('')
  const [typeIn, setTypeIn] = useState('CAR')
  const [plateOut, setPlateOut] = useState('')
  const [checkoutInfo, setCheckoutInfo] = useState(null)
  const [paymentSessionId, setPaymentSessionId] = useState('')
  const [amount, setAmount] = useState(0)

  const addLog = (msg) => setLogs((prev) => [`[${new Date().toLocaleTimeString()}] ${msg}`, ...prev])

  const fetchSessions = async () => {
    try {
      const res = await axios.get(`${API_BASE}/Report/active-sessions`)
      setSessions(res.data)
    } catch (err) {
      console.error("Lỗi tải danh sách xe:", err)
    }
  }

  useEffect(() => {
    fetchSessions();
    const interval = setInterval(fetchSessions, 2000);
    return () => clearInterval(interval);
  }, [])

  const handleCheckIn = async () => {
    try {
      const payload = { plateNumber: plateIn, vehicleType: typeIn, gateId: 'GATE-01' }
      const res = await axios.post(`${API_BASE}/CheckIn`, payload)
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
      const res = await axios.post(`${API_BASE}/CheckOut`, payload)
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
      const res = await axios.post(`${API_BASE}/Payment`, payload)
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
      <h1 style={{textAlign: 'center', color: '#333'}}>🚗 HỆ THỐNG QUẢN LÝ BÃI XE (MICRO-SERVICES)</h1>

      <div style={{ display: 'grid', gridTemplateColumns: '400px 1fr', gap: '20px' }}>
        
        {/* CỘT TRÁI */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          
          <div style={cardStyle}>
            <h3 style={{borderBottom: '2px solid #4CAF50', paddingBottom: '10px', marginTop: 0}}>⬇️ Cổng Vào</h3>
            <div style={{marginBottom: '10px'}}>
              <label>Biển số:</label>
              <input style={inputStyle} value={plateIn} onChange={(e) => setPlateIn(e.target.value)} placeholder="VD: 30A-12345" />
            </div>
            <div style={{marginBottom: '10px'}}>
              <label>Loại xe:</label>
              <select style={inputStyle} value={typeIn} onChange={(e) => setTypeIn(e.target.value)}>
                <option value="CAR">Ô tô</option>
                <option value="MOTORBIKE">Xe máy</option>
                <option value="ELECTRIC_CAR">Ô tô điện</option>
              </select>
            </div>
            <button onClick={handleCheckIn} style={{...btnStyle, background: '#4CAF50', width: '100%'}}>Mở Cổng</button>
          </div>

          <div style={cardStyle}>
            <h3 style={{borderBottom: '2px solid #2196F3', paddingBottom: '10px', marginTop: 0}}>⬆️ Cổng Ra</h3>
            <div style={{display: 'flex', gap: '5px', marginBottom: '10px'}}>
              <input style={inputStyle} value={plateOut} onChange={(e) => setPlateOut(e.target.value)} placeholder="Nhập vé / biển số..." />
              <button onClick={handleCheckOutRequest} style={{...btnStyle, background: '#2196F3'}}>Tìm</button>
            </div>

            {checkoutInfo && (
              <div style={{background: '#e3f2fd', padding: '10px', borderRadius: '5px'}}>
                <p style={{margin: '5px 0'}}>Biển số: <strong>{checkoutInfo.licensePlate}</strong></p>
                <p style={{margin: '5px 0'}}>Phí: <strong style={{color: 'red', fontSize: '1.2em'}}>{checkoutInfo.amount.toLocaleString()} đ</strong></p>
                <button onClick={handlePayment} style={{...btnStyle, background: '#ff9800', width: '100%', marginTop: '5px'}}>
                  💸 Thanh toán & Mở cổng
                </button>
              </div>
            )}
          </div>

          <div style={{...cardStyle, background: '#222', color: '#0f0', height: '200px', overflowY: 'auto'}}>
            <strong style={{display: 'block', marginBottom: '10px'}}>📟 System Logs:</strong>
            {logs.map((log, index) => <div key={index} style={{fontSize: '0.85em', marginBottom: '5px'}}>{log}</div>)}
          </div>
        </div>

        {/* CỘT PHẢI */}
        <div style={cardStyle}>
          <h3 style={{borderBottom: '2px solid #9c27b0', paddingBottom: '10px', marginTop: 0}}>
            📋 Danh Sách Xe Trong Bãi ({sessions.length})
          </h3>
          <div style={{overflowX: 'auto'}}>
            <table style={{width: '100%', borderCollapse: 'collapse'}}>
              <thead>
                <tr style={{background: '#f5f5f5', textAlign: 'left'}}>
                  <th style={thStyle}>Biển số</th>
                  <th style={thStyle}>Loại xe</th>
                  <th style={thStyle}>Mã Vé</th>
                  <th style={thStyle}>Giờ vào</th>
                  <th style={thStyle}>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map(s => (
                  <tr key={s.sessionId} style={{borderBottom: '1px solid #eee'}}>
                    <td style={tdStyle}><strong>{s.vehicle?.licensePlate}</strong></td>
                    <td style={tdStyle}>{s.vehicle?.vehicleType || 'Xe'}</td>
                    <td style={tdStyle}><span style={{fontFamily: 'monospace', background: '#eee', padding: '2px 5px'}}>{s.ticket?.ticketId}</span></td>
                    <td style={tdStyle}>{new Date(s.entryTime).toLocaleTimeString()}</td>
                    <td style={tdStyle}>
                      <span style={{
                        background: s.status === 'Active' ? '#e8f5e9' : '#fff3e0',
                        color: s.status === 'Active' ? 'green' : 'orange',
                        padding: '4px 8px', borderRadius: '12px', fontSize: '0.8em', fontWeight: 'bold'
                      }}>
                        {s.status === 'Active' ? 'Đang gửi' : 'Chờ thanh toán'}
                      </span>
                    </td>
                  </tr>
                ))}
                {sessions.length === 0 && (
                  <tr><td colSpan="5" style={{padding: '20px', textAlign: 'center', color: '#888'}}>Bãi xe đang trống</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

      </div>
    </div>
  )
}

const cardStyle = { background: 'white', padding: '20px', borderRadius: '8px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' }
const inputStyle = { padding: '8px', width: '100%', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #ccc' }
const btnStyle = { padding: '8px 15px', border: 'none', borderRadius: '4px', color: 'white', cursor: 'pointer', fontWeight: 'bold' }
const thStyle = { padding: '12px 8px', borderBottom: '2px solid #ddd' }
const tdStyle = { padding: '12px 8px' }

export default App
