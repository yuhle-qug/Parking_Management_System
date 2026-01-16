import { useEffect, useState } from 'react'
import axios from 'axios'
import { BarChart3, TrendingUp, DollarSign, Car, Calendar, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-react'
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell } from 'recharts'
import { API_BASE } from '../config/api'
const formatCurrency = (n) => (n || 0).toLocaleString('vi-VN')

const COLORS = ['#6366f1', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6']

export default function Report() {
  const [dateRange, setDateRange] = useState({ start: '', end: '' })
  const [summary, setSummary] = useState({ revenue: 0, vehicles: 0, sessions: 0, avgDuration: 0 })
  const [revenueSummary, setRevenueSummary] = useState({ today: 0, thisWeek: 0, thisMonth: 0, thisYear: 0 })
  const [revenueData, setRevenueData] = useState([])
  const [chartView, setChartView] = useState('WEEK') // WEEK | YEAR
  const [chartDate, setChartDate] = useState(new Date())
  const [vehicleTypeData, setVehicleTypeData] = useState([])
  const [hourlyData, setHourlyData] = useState([])
  const [loading, setLoading] = useState(false)
  const [membershipStats, setMembershipStats] = useState({ active: 0, expiring: 0 })
  const [lostTicketStats, setLostTicketStats] = useState({ count: 0, list: [] })
  const [showIncidentModal, setShowIncidentModal] = useState(false)

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
      const [revenueRes, trafficRes, lostTicketRes, membershipRes, summaryRes] = await Promise.all([
        axios.get(`${API_BASE}/Report`, { params: { type: 'REVENUE', from: dateRange.start, to: dateRange.end } }), // [Refactor] Use Strategy Endpoint
        axios.get(`${API_BASE}/Report`, { params: { type: 'TRAFFIC', from: dateRange.start, to: dateRange.end } }), // [Refactor] Use Strategy Endpoint
        axios.get(`${API_BASE}/Report/lost-tickets`, { params: { from: dateRange.start, to: dateRange.end } }),
        axios.get(`${API_BASE}/Report/membership`, { params: { from: dateRange.start, to: dateRange.end } }),
        axios.get(`${API_BASE}/Report/revenue/summary`)
      ])
      
      const rev = revenueRes.data
      const traf = trafficRes.data
      const lost = lostTicketRes.data
      const mem = membershipRes.data
      setRevenueSummary({
        today: summaryRes.data.today || summaryRes.data.Today || 0,
        thisWeek: summaryRes.data.thisWeek || summaryRes.data.ThisWeek || 0,
        thisMonth: summaryRes.data.thisMonth || summaryRes.data.ThisMonth || 0,
        thisYear: summaryRes.data.thisYear || summaryRes.data.ThisYear || 0
      })
      
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
      
      setLostTicketStats({ count: lost.count || 0, list: lost.list || lost.List || [] })
      setMembershipStats({ active: mem.activeCount || 0, expiring: mem.expiringSoon || 0 })

      // Map hourly data from backend
      const hourly = traf.hourlyTraffic || traf.HourlyTraffic || []
      const mappedHourly = hourly.map(h => ({
        hour: h.hour || h.Hour,
        entries: h.entries || h.Entries || 0,
        exits: h.exits || h.Exits || 0
      }))
      setHourlyData(mappedHourly.length ? mappedHourly : [])
      
      // Fetch Chart Data
      await fetchChartData(chartView, chartDate)

    } catch (err) {
      console.error('Error fetching reports:', err)
      alert("Không thể tải dữ liệu báo cáo!")
    } finally {
      setLoading(false)
    }
  }

  const fetchChartData = async (view, date) => {
      try {
          const res = await axios.get(`${API_BASE}/Report/revenue/chart`, { 
              params: { 
                  type: view,
                  date: date.toISOString() // Send full date
              } 
          })
          const data = Array.isArray(res.data) ? res.data : []
          const mapped = data.map(d => ({
              label: d.label || d.Label,
              value: d.value || d.Value || 0,
              date: d.date || d.Date
          }))
          setRevenueData(mapped)
      } catch (err) {
          console.error("Chart fetch error", err)
      }
  }

  useEffect(() => {
      fetchChartData(chartView, chartDate)
  }, [chartView, chartDate])

  const handlePrevChart = () => {
      const newDate = new Date(chartDate)
      if (chartView === 'WEEK') newDate.setDate(newDate.getDate() - 7)
      else newDate.setFullYear(newDate.getFullYear() - 1)
      setChartDate(newDate)
  }

  const handleNextChart = () => {
      const newDate = new Date(chartDate)
      if (chartView === 'WEEK') newDate.setDate(newDate.getDate() + 7)
      else newDate.setFullYear(newDate.getFullYear() + 1)
      setChartDate(newDate)
  }

  const getChartLabel = () => {
      if (chartView === 'WEEK') {
        // Find start/end of week for label
        // This is approximate logic matching backend "Mon-Sun"
        const d = new Date(chartDate)
        const day = d.getDay()
        const diff = d.getDate() - day + (day === 0 ? -6 : 1); 
        const start = new Date(d.setDate(diff));
        const end = new Date(start)
        end.setDate(end.getDate() + 6)
        return `Tuần ${start.toLocaleDateString('vi-VN')} - ${end.toLocaleDateString('vi-VN')}`
      }
      return `Năm ${chartDate.getFullYear()}`
  }

  useEffect(() => {
    if (dateRange.start && dateRange.end) fetchReports()
  }, [dateRange])

  const statCards = [
    { label: 'Tổng doanh thu', value: `${formatCurrency(summary.revenue)} đ`, icon: DollarSign, color: 'green' },
    { label: 'Lượt xe vào', value: summary.vehicles, icon: Car, color: 'blue' },
    { label: 'Vé tháng (Active)', value: membershipStats.active, icon: Calendar, color: 'purple' },
    { label: 'Mất vé / Sự cố', value: lostTicketStats.count, icon: TrendingUp, color: 'amber', onClick: () => setShowIncidentModal(true) }
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

      {/* Revenue Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white p-4 rounded-xl border border-indigo-100 bg-indigo-50/50">
             <div className="text-xs text-indigo-500 uppercase font-bold">Hôm nay</div>
             <div className="text-xl font-bold text-indigo-700">{formatCurrency(revenueSummary.today)} đ</div>
          </div>
          <div className="bg-white p-4 rounded-xl border border-blue-100 bg-blue-50/50">
             <div className="text-xs text-blue-500 uppercase font-bold">Tuần này</div>
             <div className="text-xl font-bold text-blue-700">{formatCurrency(revenueSummary.thisWeek)} đ</div>
          </div>
          <div className="bg-white p-4 rounded-xl border border-purple-100 bg-purple-50/50">
             <div className="text-xs text-purple-500 uppercase font-bold">Tháng này</div>
             <div className="text-xl font-bold text-purple-700">{formatCurrency(revenueSummary.thisMonth)} đ</div>
          </div>
          <div className="bg-white p-4 rounded-xl border border-emerald-100 bg-emerald-50/50">
             <div className="text-xs text-emerald-500 uppercase font-bold">Năm nay</div>
             <div className="text-xl font-bold text-emerald-700">{formatCurrency(revenueSummary.thisYear)} đ</div>
          </div>
      </div>

      {/* Stats Cards */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {statCards.map((card, idx) => (
           <div 
            key={idx} 
            className={`bg-white rounded-xl shadow-sm border border-gray-100 p-5 ${card.onClick ? 'cursor-pointer hover:shadow-md transition' : ''}`}
            onClick={card.onClick}
           >
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
           <div className="flex flex-col md:flex-row md:items-center justify-between mb-4 gap-3">
             <div className="flex items-center gap-4">
                 <h3 className="font-semibold text-gray-800">Biểu đồ doanh thu</h3>
                 <div className="flex items-center gap-2 bg-gray-50 rounded-lg px-2 py-1 text-sm border border-gray-100">
                    <button onClick={handlePrevChart} className="p-1 hover:bg-gray-200 rounded-md transition text-gray-500">
                        <ChevronLeft size={16} />
                    </button>
                    <span className="font-medium text-gray-700 min-w-[140px] text-center">{getChartLabel()}</span>
                    <button onClick={handleNextChart} className="p-1 hover:bg-gray-200 rounded-md transition text-gray-500">
                        <ChevronRight size={16} />
                    </button>
                 </div>
             </div>

             <div className="flex bg-gray-100 p-1 rounded-lg self-start md:self-auto">
                <button 
                    onClick={() => { setChartView('WEEK'); setChartDate(new Date()) }}
                    className={`px-3 py-1.5 text-xs font-semibold rounded-md transition ${chartView === 'WEEK' ? 'bg-white shadow-sm text-indigo-600' : 'text-gray-500 hover:text-gray-700'}`}
                >
                    Theo tuần
                </button>
                <button 
                    onClick={() => { setChartView('YEAR'); setChartDate(new Date()) }}
                    className={`px-3 py-1.5 text-xs font-semibold rounded-md transition ${chartView === 'YEAR' ? 'bg-white shadow-sm text-indigo-600' : 'text-gray-500 hover:text-gray-700'}`}
                >
                    Theo năm
                </button>
             </div>
           </div>
           
           <ResponsiveContainer width="100%" height={280}>
             <BarChart data={revenueData}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="label" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} tickFormatter={(v) => `${(v / 1000000).toFixed(1)}M`} />
              <Tooltip formatter={(v) => [`${formatCurrency(v)} đ`, 'Doanh thu']} />
              <Bar dataKey="value" fill="#6366f1" radius={[4, 4, 0, 0]} />
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
          <div className="flex flex-wrap justify-center gap-4 mt-2">
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


      {/* Incident Modal */}
      {showIncidentModal && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-xl shadow-xl max-w-2xl w-full max-h-[80vh] flex flex-col">
            <div className="p-6 border-b border-gray-100 flex items-center justify-between">
              <h3 className="text-lg font-bold text-gray-800">Danh sách sự cố & Mất vé</h3>
              <button 
                onClick={() => setShowIncidentModal(false)}
                className="text-gray-400 hover:text-gray-600 font-bold text-xl"
              >
                ×
              </button>
            </div>
            
            <div className="p-6 overflow-y-auto">
              {lostTicketStats.list.length === 0 ? (
                <p className="text-center text-gray-500 py-8">Không có dữ liệu trong khoảng thời gian này.</p>
              ) : (
                <div className="border border-gray-200 rounded-lg overflow-hidden">
                  <table className="w-full text-sm text-left">
                    <thead className="bg-gray-50 text-gray-600 font-medium border-b border-gray-200">
                      <tr>
                        <th className="px-4 py-3">Thời gian</th>
                        <th className="px-4 py-3">Nội dung</th>
                        <th className="px-4 py-3">Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {lostTicketStats.list.map((item, i) => (
                        <tr key={i} className="hover:bg-gray-50/50">
                          <td className="px-4 py-3 text-gray-600">
                             {new Date(item.date || item.Date || item.reportedDate || item.ReportedDate).toLocaleString('vi-VN')}
                          </td>
                          <td className="px-4 py-3 font-medium text-gray-800">{item.title || item.Title}</td>
                          <td className="px-4 py-3">
                            <span className={`inline-flex px-2 py-1 rounded text-xs font-medium 
                              ${(item.status || item.Status) === 'Resolved' || (item.status || item.Status) === 'Completed' ? 'bg-green-100 text-green-700' : 'bg-amber-100 text-amber-700'}`}>
                              {item.status || item.Status}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
            
            <div className="p-4 border-t border-gray-100 bg-gray-50 rounded-b-xl flex justify-end">
              <button 
                onClick={() => setShowIncidentModal(false)}
                className="px-4 py-2 bg-white border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
