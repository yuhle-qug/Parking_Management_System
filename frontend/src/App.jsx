import { useEffect, useMemo, useState } from 'react'
import axios from 'axios'
import { Toaster, toast } from 'react-hot-toast'
import {
  Bar,
  BarChart,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts'
import './App.css'

const API_BASE = 'http://localhost:5166/api'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')
const formatTime = (v) => new Date(v).toLocaleTimeString('vi-VN')

const Spinner = () => <div className="spinner" aria-label="Đang tải" />

const InputWithIcon = ({ icon, as = 'input', children, ...rest }) => {
  if (as === 'select') {
    return (
      <div className="input-wrap">
        <span className="input-icon">{icon}</span>
        <select className="input" {...rest}>{children}</select>
      </div>
    )
  }
  return (
    <div className="input-wrap">
      <span className="input-icon">{icon}</span>
      <input className="input" {...rest} />
    </div>
  )
}

const Breadcrumb = ({ items }) => (
  <div className="breadcrumb">
    {items.map((item, idx) => (
      <span key={item} className={idx === items.length - 1 ? 'breadcrumb-active' : ''}>
        {item}
        {idx < items.length - 1 && <span className="crumb-sep">/</span>}
      </span>
    ))}
  </div>
)

const Modal = ({ open, title, onClose, children }) => {
  if (!open) return null
  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="modal">
        <div className="modal-header">
          <h3>{title}</h3>
          <button className="btn ghost" onClick={onClose}>✕</button>
        </div>
        {children}
      </div>
    </div>
  )
}

const LoginScreen = ({ onLogin }) => {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)

  const handleSubmit = async () => {
    if (!username || !password) return toast.error('Vui lòng nhập đủ thông tin')
    setLoading(true)
    try {
      const res = await axios.post(`${API_BASE}/UserAccount/login`, { username, password })
      toast.success('Đăng nhập thành công')
      onLogin(res.data)
    } catch (err) {
      const msg = err?.response?.data?.message || err?.response?.data?.Message || err?.message || 'Đăng nhập thất bại'
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="glass-card auth-card">
        <h2>🔐 Đăng nhập hệ thống</h2>
        <p className="muted">Truy cập bảng điều khiển bãi xe</p>
        <InputWithIcon icon="👤" value={username} onChange={(e) => setUsername(e.target.value)} placeholder="Tài khoản" />
        <InputWithIcon icon="🔒" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Mật khẩu" />
        <button className="btn primary full" onClick={handleSubmit} disabled={loading}>
          {loading ? <div className="inline-spinner" /> : 'Đăng nhập'}
        </button>
      </div>
      <Toaster position="top-right" />
    </div>
  )
}

const Dashboard = () => {
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
    } catch (err) {
      toast.error('Không tải được danh sách xe')
    } finally {
      setLoadingSessions(false)
    }
  }

  useEffect(() => {
    fetchSessions()
    const interval = setInterval(fetchSessions, 2500)
    return () => clearInterval(interval)
  }, [])

  const handleCheckIn = async () => {
    if (!plateIn) return toast.error('Nhập biển số trước khi vào bến')
    try {
      const res = await axios.post(`${API_BASE}/CheckIn`, { plateNumber: plateIn, vehicleType: typeIn, gateId: 'GATE-01' })
      toast.success(`Vào bến: ${plateIn}`)
      addLog(`✅ Check-in: ${plateIn} - Vé: ${res.data.ticketId}`)
      setPlateIn('')
      fetchSessions()
    } catch (err) {
      toast.error(err.response?.data?.error || 'Lỗi check-in')
      addLog('❌ ' + (err.response?.data?.error || 'Check-in lỗi'))
    }
  }

  const handleCheckOut = async () => {
    if (!plateOut) return toast.error('Nhập vé hoặc biển số để kiểm tra')
    setCheckingOut(true)
    try {
      const res = await axios.post(`${API_BASE}/CheckOut`, { ticketIdOrPlate: plateOut, gateId: 'GATE-02' })
      setCheckoutInfo(res.data)
      addLog(`ℹ️ Xe ra: ${res.data.licensePlate} - Phí: ${res.data.amount}`)
    } catch (err) {
      toast.error(err.response?.data?.error || 'Không tìm thấy xe')
      addLog('❌ ' + (err.response?.data?.error || 'Không tìm thấy xe'))
    } finally {
      setCheckingOut(false)
    }
  }

  const handlePay = async () => {
    if (!checkoutInfo) return
    const confirmed = window.confirm('Xác nhận thanh toán và mở cổng?')
    if (!confirmed) return
    setPaying(true)
    try {
      await axios.post(`${API_BASE}/Payment`, { sessionId: checkoutInfo.sessionId, amount: checkoutInfo.amount })
      toast.success('Thanh toán thành công')
      addLog('💰 Thanh toán thành công')
      setCheckoutInfo(null)
      setPlateOut('')
      fetchSessions()
    } catch (err) {
      toast.error('Thanh toán thất bại')
    } finally {
      setPaying(false)
    }
  }

  return (
    <div className="grid two-cols">
      <div className="stack">
        <div className="card">
          <div className="card-header">
            <div>
              <div className="pill success">Cổng vào</div>
              <div className="muted">Kiểm soát luồng xe vào</div>
            </div>
            <div className="status-dot online">Online</div>
          </div>
          <InputWithIcon icon="🚘" value={plateIn} onChange={(e) => setPlateIn(e.target.value)} placeholder="Biển số..." />
          <InputWithIcon icon="🛵" as="select" value={typeIn} onChange={(e) => setTypeIn(e.target.value)}>
            <option value="CAR">Ô tô</option>
            <option value="MOTORBIKE">Xe máy</option>
          </InputWithIcon>
          <button className="btn primary" onClick={handleCheckIn}>Vào bến</button>
        </div>

        <div className="card">
          <div className="card-header">
            <div>
              <div className="pill info">Cổng ra</div>
              <div className="muted">Kiểm tra & tính phí</div>
            </div>
            <div className={`status-dot ${checkingOut ? 'busy' : 'idle'}`}>{checkingOut ? 'Đang kiểm tra' : 'Sẵn sàng'}</div>
          </div>
          <InputWithIcon icon="🎫" value={plateOut} onChange={(e) => setPlateOut(e.target.value)} placeholder="Nhập vé hoặc biển số..." />
          <button className="btn secondary" onClick={handleCheckOut} disabled={checkingOut}>
            {checkingOut ? <div className="inline-spinner" /> : 'Kiểm tra'}
          </button>
          {checkoutInfo && (
            <div className="checkout-box">
              <div>Biển số: <b>{checkoutInfo.licensePlate}</b></div>
              <div>Phí: <b className="price">{formatCurrency(checkoutInfo.amount)} đ</b></div>
              <button className="btn accent" onClick={handlePay} disabled={paying}>
                {paying ? <div className="inline-spinner" /> : 'Thanh toán & mở cổng'}
              </button>
            </div>
          )}
        </div>

        <div className="card log-card">
          <div className="card-header">
            <div className="pill">Logs</div>
            <div className="muted">Sự kiện gần đây</div>
          </div>
          <div className="log-list">
            {logs.map((l, i) => <div key={i} className="log-item fade-in">{l}</div>)}
            {logs.length === 0 && <div className="muted">Chưa có log</div>}
          </div>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div>
            <div className="pill">Xe trong bến</div>
            <div className="muted">{sessions.length} xe đang gửi</div>
          </div>
        </div>
        {loadingSessions ? <div className="skeleton tall" /> : (
          <div className="table-wrap">
            <table className="table zebra">
              <thead>
                <tr><th>Biển số</th><th>Vé</th><th>Giờ vào</th><th>TT</th></tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr key={s.sessionId}>
                    <td><b>{s.vehicle?.licensePlate}</b></td>
                    <td className="mono">{s.ticket?.ticketId}</td>
                    <td>{formatTime(s.entryTime)}</td>
                    <td><span className={`chip ${s.status === 'Active' ? 'chip-green' : 'chip-amber'}`}>{s.status}</span></td>
                  </tr>
                ))}
                {sessions.length === 0 && (
                  <tr><td colSpan="4">
                    <div className="empty-state">✨ Bãi xe đang trống. Chờ xe vào để hiển thị.</div>
                  </td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

const Membership = () => {
  const [form, setForm] = useState({ name: '', phone: '', identityNumber: '', plateNumber: '' })
  const [loading, setLoading] = useState(false)
  const benefits = ['Giữ chỗ cố định', 'Ra/vào nhanh không chờ', 'Ưu đãi phí theo tháng', 'Hóa đơn điện tử']

  const handleRegister = async () => {
    if (!form.name || !form.phone || !form.identityNumber || !form.plateNumber) return toast.error('Điền đủ thông tin')
    setLoading(true)
    try {
      await axios.post(`${API_BASE}/Membership/register`, form)
      toast.success(`Đăng ký vé tháng cho ${form.plateNumber}`)
      setForm({ name: '', phone: '', identityNumber: '', plateNumber: '' })
    } catch (err) {
      toast.error(err.response?.data?.Error || 'Đăng ký thất bại')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <div>
          <div className="pill accent">Vé tháng</div>
          <div className="muted">Đăng ký nhanh cho khách hàng</div>
        </div>
      </div>
      <div className="form-grid">
        <InputWithIcon icon="🙍" placeholder="Họ tên khách hàng" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
        <InputWithIcon icon="📞" placeholder="Số điện thoại" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        <InputWithIcon icon="🪪" placeholder="CCCD / CMND" value={form.identityNumber} onChange={(e) => setForm({ ...form, identityNumber: e.target.value })} />
        <InputWithIcon icon="🚗" placeholder="Biển số (VD: 30A-9999)" value={form.plateNumber} onChange={(e) => setForm({ ...form, plateNumber: e.target.value })} />
      </div>
      <button className="btn primary" onClick={handleRegister} disabled={loading}>{loading ? <div className="inline-spinner" /> : 'Đăng ký vé tháng'}</button>
      <div className="benefits">
        {benefits.map((b) => <div key={b} className="benefit-item">✅ {b}</div>)}
      </div>
    </div>
  )
}

const Report = () => {
  const [revenue, setRevenue] = useState(null)
  const [traffic, setTraffic] = useState(null)
  const [loading, setLoading] = useState(false)
  const [startDate, setStartDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [endDate, setEndDate] = useState(() => new Date().toISOString().slice(0, 10))

  const loadData = async () => {
    setLoading(true)
    try {
      const [rev, traf] = await Promise.all([
        axios.get(`${API_BASE}/Report/revenue`, { params: { startDate, endDate } }),
        axios.get(`${API_BASE}/Report/traffic`, { params: { startDate, endDate } })
      ])
      setRevenue(rev.data)
      setTraffic(traf.data)
      toast.success('Đã cập nhật báo cáo')
    } catch (err) {
      toast.error('Không tải được báo cáo')
    } finally {
      setLoading(false)
    }
  }

  const paymentChartData = useMemo(() => {
    if (!revenue?.revenueByPaymentMethod) return []
    return Object.entries(revenue.revenueByPaymentMethod).map(([k, v]) => ({ name: k, value: v }))
  }, [revenue])

  const vehicleChartData = useMemo(() => {
    if (!traffic?.vehiclesByType) return []
    return Object.entries(traffic.vehiclesByType).map(([k, v]) => ({ name: k, value: v }))
  }, [traffic])

  const trendData = useMemo(() => revenue?.hourlyRevenue || revenue?.dailyRevenue || [], [revenue])
  const pieColors = ['#0fb5ba', '#f97316', '#2563eb', '#7c3aed', '#0ea5e9']

  return (
    <div className="stack">
      <div className="card">
        <div className="card-header">
          <div>
            <div className="pill">Báo cáo</div>
            <div className="muted">Doanh thu & lưu lượng</div>
          </div>
          <div className="form-row">
            <input type="date" className="input" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
            <input type="date" className="input" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
            <button className="btn primary" onClick={loadData} disabled={loading}>{loading ? <div className="inline-spinner" /> : 'Làm mới'}</button>
          </div>
        </div>
      </div>

      <div className="grid two-cols">
        <div className="card kpi-card">
          <h3>💰 Doanh thu</h3>
          <p className="kpi-number">{formatCurrency(revenue?.totalRevenue || 0)} VNĐ</p>
          <p className="muted">Giao dịch: {revenue?.totalTransactions || 0}</p>
          <div className="chart-wrap">
            {paymentChartData.length === 0 ? <div className="empty-state">Chưa có dữ liệu</div> : (
              <ResponsiveContainer width="100%" height={240}>
                <PieChart>
                  <Pie data={paymentChartData} dataKey="value" nameKey="name" outerRadius={90} label>
                    {paymentChartData.map((_, i) => (
                      <Cell key={i} fill={pieColors[i % pieColors.length]} />
                    ))}
                  </Pie>
                  <Legend />
                  <Tooltip formatter={(v) => formatCurrency(v) + ' đ'} />
                </PieChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="card kpi-card">
          <h3>🚗 Lưu lượng</h3>
          <p className="kpi-number">Vào: {traffic?.totalVehiclesIn || 0} / Ra: {traffic?.totalVehiclesOut || 0}</p>
          <div className="chart-wrap">
            {vehicleChartData.length === 0 ? <div className="empty-state">Chưa có dữ liệu</div> : (
              <ResponsiveContainer width="100%" height={240}>
                <BarChart data={vehicleChartData}>
                  <XAxis dataKey="name" />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="value" fill="#1a73e8" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </div>

      <div className="card kpi-card">
        <h3>📈 Xu hướng doanh thu</h3>
        <div className="chart-wrap">
          {trendData.length === 0 ? <div className="empty-state">Chưa có dữ liệu</div> : (
            <ResponsiveContainer width="100%" height={240}>
              <LineChart data={trendData}>
                <XAxis dataKey="label" />
                <YAxis />
                <Tooltip formatter={(v) => formatCurrency(v) + ' đ'} />
                <Line dataKey="value" stroke="#0fb5ba" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  )
}

const passwordStrength = (pwd) => {
  let score = 0
  if (pwd.length >= 6) score += 1
  if (/[A-Z]/.test(pwd)) score += 1
  if (/[0-9]/.test(pwd)) score += 1
  if (/[^A-Za-z0-9]/.test(pwd)) score += 1
  return score
}

const AdminPanel = () => {
  const [userForm, setUserForm] = useState({ username: '', password: '', role: 'ATTENDANT' })
  const [loading, setLoading] = useState(false)
  const [showModal, setShowModal] = useState(false)
  const [recentUsers, setRecentUsers] = useState([])

  const handleCreate = async () => {
    if (!userForm.username || !userForm.password) return toast.error('Nhập đủ username/password')
    const confirmed = window.confirm(`Tạo tài khoản ${userForm.username}?`)
    if (!confirmed) return
    setLoading(true)
    try {
      await axios.post(`${API_BASE}/UserAccount/create`, userForm)
      toast.success(`Tạo user ${userForm.username} thành công`)
      setRecentUsers((list) => [{ ...userForm, status: 'Active', id: Date.now() }, ...list].slice(0, 5))
      setShowModal(false)
      setUserForm({ username: '', password: '', role: 'ATTENDANT' })
    } catch (err) {
      toast.error(err.response?.data?.Error || 'Không tạo được user')
    } finally {
      setLoading(false)
    }
  }

  const strength = passwordStrength(userForm.password)
  const strengthLabel = ['Yếu', 'Trung bình', 'Khá', 'Mạnh'][Math.max(0, strength - 1)] || 'Yếu'

  return (
    <div className="stack">
      <div className="card">
        <div className="card-header">
          <div>
            <div className="pill">Quản trị</div>
            <div className="muted">Tạo tài khoản nhân viên</div>
          </div>
          <button className="btn primary" onClick={() => setShowModal(true)}>Tạo user</button>
        </div>
      </div>

      <div className="card">
        <div className="card-header">
          <div className="pill info">Danh sách mới tạo</div>
        </div>
        <div className="table-wrap">
          <table className="table zebra">
            <thead><tr><th>User</th><th>Role</th><th>Trạng thái</th></tr></thead>
            <tbody>
              {recentUsers.map((u) => (
                <tr key={u.id}>
                  <td>{u.username}</td>
                  <td><span className="chip chip-blue">{u.role}</span></td>
                  <td><span className="chip chip-green">{u.status}</span></td>
                </tr>
              ))}
              {recentUsers.length === 0 && <tr><td colSpan="3"><div className="empty-state">Chưa có user mới</div></td></tr>}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={showModal} title="Tạo tài khoản" onClose={() => setShowModal(false)}>
        <div className="stack">
          <InputWithIcon icon="👤" placeholder="Username" value={userForm.username} onChange={(e) => setUserForm({ ...userForm, username: e.target.value })} />
          <InputWithIcon icon="🔒" type="password" placeholder="Password" value={userForm.password} onChange={(e) => setUserForm({ ...userForm, password: e.target.value })} />
          <InputWithIcon icon="🎯" as="select" value={userForm.role} onChange={(e) => setUserForm({ ...userForm, role: e.target.value })}>
            <option value="ATTENDANT">Nhân viên</option>
            <option value="ADMIN">Admin</option>
          </InputWithIcon>
          <div className="strength">
            <div>Độ mạnh mật khẩu: <b>{strengthLabel}</b></div>
            <div className="strength-bar">
              {[0, 1, 2, 3].map((i) => <span key={i} className={i < strength ? 'on' : ''} />)}
            </div>
          </div>
          <button className="btn primary" onClick={handleCreate} disabled={loading}>{loading ? <div className="inline-spinner" /> : 'Tạo user'}</button>
        </div>
      </Modal>
    </div>
  )
}

function App() {
  const [user, setUser] = useState(null)
  const [activeTab, setActiveTab] = useState('dashboard')

  const tabs = [
    { id: 'dashboard', label: 'Dashboard', icon: '🖥️' },
    { id: 'membership', label: 'Vé tháng', icon: '💎', role: 'ADMIN' },
    { id: 'report', label: 'Báo cáo', icon: '📊', role: 'ADMIN' },
    { id: 'admin', label: 'Admin', icon: '🛡️', role: 'ADMIN' }
  ]

  if (!user) return <LoginScreen onLogin={setUser} />

  const visibleTabs = tabs.filter((t) => !t.role || user.role === t.role)
  const activeLabel = visibleTabs.find((t) => t.id === activeTab)?.label || ''

  return (
    <div className="page-shell">
      <div className="nav-bar glass-card">
        <div className="brand">🅿️ Parking System Pro</div>
        <div className="nav-links">
          {visibleTabs.map((tab) => (
            <div key={tab.id} className={`nav-item ${activeTab === tab.id ? 'active' : ''}`} onClick={() => setActiveTab(tab.id)}>
              <span className="nav-icon">{tab.icon}</span>{tab.label}
            </div>
          ))}
        </div>
        <div className="user-chip">
          <div>
            <div className="muted">Xin chào</div>
            <div className="user-name">{user.username} · {user.role}</div>
          </div>
          <button className="btn ghost" onClick={() => setUser(null)}>Đăng xuất</button>
        </div>
      </div>

      <Breadcrumb items={[`Home`, activeLabel]} />

      <main className="content">
        {activeTab === 'dashboard' && <Dashboard />}
        {activeTab === 'membership' && <Membership />}
        {activeTab === 'report' && <Report />}
        {activeTab === 'admin' && <AdminPanel />}
      </main>

      <Toaster position="top-right" />
    </div>
  )
}

export default App
