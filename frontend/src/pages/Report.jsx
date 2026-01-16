import { useEffect, useState } from 'react'
import axios from 'axios'
import { BarChart3, TrendingUp, DollarSign, Car, Calendar, RefreshCw } from 'lucide-react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from 'recharts'
import { API_BASE } from '../config/api'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')

const COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6']

export default function Report() {
  const [dateRange, setDateRange] = useState({ start: '', end: '' })
  const [summary, setSummary] = useState({ revenue: 0, vehicles: 0, sessions: 0, avgDuration: 0 })
  const [revenueData, setRevenueData] = useState([])
  const [vehicleTypeData, setVehicleTypeData] = useState([])
  const [hourlyData, setHourlyData] = useState([])
  const [loading, setLoading] = useState(false)
  const [membershipStats, setMembershipStats] = useState({ active: 0, expiring: 0 })
  const [lostTicketStats, setLostTicketStats] = useState({ count: 0 })

  // Set default date range to last 7 days
  useEffect(() => {
    const end = new Date()
    const start = new Date()
    start.setDate(start.getDate() - 7)
    setDateRange({
      start: start.toISOString().split('T')[0],
      end: end.toISOString().split('T')[0]
    })
  }, [])

  const fetchReports = async () => {
    if (!dateRange.start || !dateRange.end) return
    setLoading(true)
    try {
      const [revenueRes, trafficRes, lostTicketRes, membershipRes] = await Promise.all([
        axios.get(`${API_BASE}/Report/revenue`, { params: { from: dateRange.start, to: dateRange.end } }),
        axios.get(`${API_BASE}/Report/traffic`, { params: { from: dateRange.start, to: dateRange.end } }),
        axios.get(`${API_BASE}/Report/lost-tickets`, { params: { from: dateRange.start, to: dateRange.end } }),
        axios.get(`${API_BASE}/Report/membership`, { params: { from: dateRange.start, to: dateRange.end } })
      ])
      
      const rev = revenueRes.data
      const traf = trafficRes.data
      const lost = lostTicketRes.data
      const mem = membershipRes.data
      
      setSummary({
        revenue: rev.totalRevenue || 0,
        vehicles: traf.totalVehiclesIn || 0,
        sessions: rev.totalTransactions || 0,
        avgDuration: 0 // Not calculated yet
      })

      // Revenue Chart Data (Simplify: just show total revenue by day if backend supported daily breakdown, 
      // but backend currently sends aggregate. For now, let's just show Payment Method breakdown in a Pie)
      // If we want daily revenue, backend needs update. 
      // Reuse existing chart logic loosely or switch to Payment Method chart.
      // Let's repurpose the Revenue Chart to show mock daily distribution of the REAL total (since backend doesn't give daily series yet)
      // OR better: Visualize Payment Methods which WE HAVE real data for.
      
      // Show Total Revenue by Day (if backend provides daily series) logic... 
      // Currently just showing Total.
      setRevenueData([
        { date: 'Tổng', revenue: rev.totalRevenue || 0 }
      ])
      
      const vTypes = traf.vehiclesByType || {}
      setVehicleTypeData(Object.entries(vTypes).map(([k, v]) => ({ type: k, count: v })))
      
      setLostTicketStats({ count: lost.count || 0 })
      setMembershipStats({ active: mem.activeCount || 0, expiring: mem.expiringSoon || 0 })

      // Map hourly data from backend
      const hourly = traf.hourlyTraffic || traf.HourlyTraffic || []
      const mappedHourly = hourly.map(h => ({
        hour: h.hour || h.Hour,
        entries: h.entries || h.Entries || 0,
        exits: h.exits || h.Exits || 0
      }))
      setHourlyData(mappedHourly.length ? mappedHourly : []) 

    } catch (err) {
      console.error('Error fetching reports:', err)
      alert("Không thể tải dữ liệu báo cáo!")
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (dateRange.start && dateRange.end) fetchReports()
  }, [dateRange])

  const statCards = [
    { label: 'Tổng doanh thu', value: `${formatCurrency(summary.revenue)} đ`, icon: DollarSign, color: 'green' },
    { label: 'Lượt xe vào', value: summary.vehicles, icon: Car, color: 'blue' },
    { label: 'Vé tháng (Active)', value: membershipStats.active, icon: Calendar, color: 'purple' },
    { label: 'Mất vé / Sự cố', value: lostTicketStats.count, icon: TrendingUp, color: 'amber' }
  ]

  const colorClasses = {
    green: 'bg-green-100 text-green-600',
    blue: 'bg-blue-100 text-blue-600',
    purple: 'bg-purple-100 text-purple-600',
    amber: 'bg-amber-100 text-amber-600'
  }

  return (
    <div className="space-y-6">
      {/* Header with Date Filter */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Báo cáo & Thống kê</h1>
          <p className="text-sm text-gray-500">Phân tích dữ liệu hoạt động bãi xe</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 bg-white px-3 py-2 rounded-lg border border-gray-200">
            <Calendar size={16} className="text-gray-400" />
            <input
              type="date"
              className="text-sm outline-none"
              value={dateRange.start}
              onChange={(e) => setDateRange({ ...dateRange, start: e.target.value })}
            />
            <span className="text-gray-400">→</span>
            <input
              type="date"
              className="text-sm outline-none"
              value={dateRange.end}
              onChange={(e) => setDateRange({ ...dateRange, end: e.target.value })}
            />
          </div>
          <button
            onClick={fetchReports}
            disabled={loading}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2 rounded-lg text-sm font-medium transition disabled:opacity-60"
          >
            <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
            Làm mới
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((card, idx) => (
          <div key={idx} className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm text-gray-500">{card.label}</p>
                <p className="text-2xl font-bold text-gray-800 mt-1">{card.value}</p>
              </div>
              <div className={`w-12 h-12 rounded-xl flex items-center justify-center ${colorClasses[card.color]}`}>
                <card.icon size={24} />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Charts Row */}
      <div className="grid lg:grid-cols-3 gap-6">
        {/* Revenue Chart - Full width of its column */}
        <div className="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <h3 className="font-semibold text-gray-800 mb-4">Tổng Doanh thu</h3>
          <ResponsiveContainer width="100%" height={280}>
             <BarChart data={revenueData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="date" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} tickFormatter={(v) => `${(v / 1000000).toFixed(1)}M`} />
              <Tooltip formatter={(v) => [`${formatCurrency(v)} đ`, 'Doanh thu']} />
              <Bar dataKey="revenue" fill="#6366f1" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Vehicle Type Pie */}
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <h3 className="font-semibold text-gray-800 mb-4">Phân loại xe</h3>
          <ResponsiveContainer width="100%" height={200}>
            <PieChart>
              <Pie
                data={vehicleTypeData}
                dataKey="count"
                nameKey="type"
                cx="50%"
                cy="50%"
                innerRadius={50}
                outerRadius={80}
                label={({ type, percent }) => `${type} ${(percent * 100).toFixed(0)}%`}
                labelLine={false}
              >
                {vehicleTypeData.map((_, idx) => (
                  <Cell key={idx} fill={COLORS[idx % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
          <div className="flex justify-center gap-4 mt-2">
            {vehicleTypeData.map((item, idx) => (
              <div key={idx} className="flex items-center gap-1.5 text-xs">
                <div className="w-3 h-3 rounded-full" style={{ backgroundColor: COLORS[idx % COLORS.length] }} />
                <span className="text-gray-600">{item.type}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Hourly Traffic */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
        <h3 className="font-semibold text-gray-800 mb-4">Lưu lượng theo giờ</h3>
        <ResponsiveContainer width="100%" height={250}>
          <LineChart data={hourlyData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
            <XAxis dataKey="hour" tick={{ fontSize: 11 }} />
            <YAxis tick={{ fontSize: 12 }} />
            <Tooltip />
            <Line type="monotone" dataKey="entries" stroke="#10b981" strokeWidth={2} name="Xe vào" dot={false} />
            <Line type="monotone" dataKey="exits" stroke="#ef4444" strokeWidth={2} name="Xe ra" dot={false} />
          </LineChart>
        </ResponsiveContainer>
        <div className="flex justify-center gap-6 mt-2 text-sm">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-green-500" />
            <span className="text-gray-600">Xe vào</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-red-500" />
            <span className="text-gray-600">Xe ra</span>
          </div>
        </div>
      </div>
    </div>
  )
}
