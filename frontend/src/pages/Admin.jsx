import { useEffect, useState } from 'react'
import axios from 'axios'
import { Users, UserPlus, Shield, Trash2, Edit, X, Check, Settings, Key, Layers, DollarSign, Edit3, PlusCircle } from 'lucide-react'
import { API_BASE } from '../config/api'

export default function Admin() {
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(false)
  const [showModal, setShowModal] = useState(false)
  const [editUser, setEditUser] = useState(null)
  const [form, setForm] = useState({ username: '', password: '', role: 'ATTENDANT' })

  const [pricePolicies, setPricePolicies] = useState([])
  const [membershipPolicies, setMembershipPolicies] = useState([])
  const [priceModal, setPriceModal] = useState({ open: false, mode: 'create', data: emptyPricePolicy() })
  const [membershipModal, setMembershipModal] = useState({ open: false, mode: 'create', data: emptyMembershipPolicy() })

  const fetchUsers = async () => {
    setLoading(true)
    try {
      const res = await axios.get(`${API_BASE}/UserAccount`)
      const mapped = (res.data || []).map((u) => ({
        userId: u.userId,
        username: u.username,
        role: (u.role || '').toUpperCase(),
        status: u.status || 'Unknown',
      }))
      setUsers(mapped)
    } catch {
      setUsers([])
    } finally {
      setLoading(false)
    }
  }

  const fetchPricePolicies = async () => {
    try {
      const res = await axios.get(`${API_BASE}/PricePolicy`)
      setPricePolicies(Array.isArray(res.data) ? res.data : [])
    } catch {
      setPricePolicies([])
    }
  }

  const fetchMembershipPolicies = async () => {
    try {
      const res = await axios.get(`${API_BASE}/Membership/policies`)
      setMembershipPolicies(Array.isArray(res.data) ? res.data : [])
    } catch {
      setMembershipPolicies([])
    }
  }

  useEffect(() => {
    fetchUsers()
    fetchPricePolicies()
    fetchMembershipPolicies()
  }, [])

  const openCreateModal = () => {
    setEditUser(null)
    setForm({ username: '', password: '', role: 'ATTENDANT' })
    setShowModal(true)
  }

  const openEditModal = (user) => {
    setEditUser(user)
    setForm({ username: user.username, password: '', role: user.role, fullName: user.fullName || '', email: user.email || '', phone: user.phone || '' })
    setShowModal(true)
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.username || (!editUser && !form.password)) {
      return alert('Vui lòng điền đầy đủ thông tin')
    }
    try {
      if (editUser) {
        // Gọi API backend để cập nhật user
        await axios.put(`${API_BASE}/UserAccount/${editUser.userId}`, {
          fullName: form.fullName,
          email: form.email,
          phone: form.phone,
          password: form.password || undefined // Chỉ gửi password nếu có nhập
        })
        await fetchUsers()
      } else {
        // Gọi API backend để tạo user mới
        await axios.post(`${API_BASE}/UserAccount/create`, {
          username: form.username,
          password: form.password,
          role: form.role
        })
        await fetchUsers()
      }
      setShowModal(false)
    } catch (err) {
      alert(err.response?.data?.error || 'Lỗi lưu người dùng')
    }
  }

  const handleDelete = async (userId) => {
    if (!window.confirm('Bạn có chắc muốn xóa người dùng này?')) return
    try {
      await axios.delete(`${API_BASE}/UserAccount/${userId}`)
      await fetchUsers()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.message || 'Lỗi xóa người dùng')
    }
  }

  const openPriceModal = (mode, data = emptyPricePolicy()) => {
    setPriceModal({ open: true, mode, data: { ...data } })
  }

  const openMembershipModal = (mode, data = emptyMembershipPolicy()) => {
    setMembershipModal({ open: true, mode, data: { ...data } })
  }

  const savePricePolicy = async () => {
    const p = priceModal.data
    if (!p.policyId || !p.name) return alert('Nhập PolicyId và Name')
    try {
      if (priceModal.mode === 'create') {
        await axios.post(`${API_BASE}/PricePolicy`, p)
      } else {
        await axios.put(`${API_BASE}/PricePolicy/${p.policyId}`, p)
      }
      setPriceModal({ open: false, mode: 'create', data: emptyPricePolicy() })
      await fetchPricePolicies()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Lưu policy thất bại')
    }
  }

  const deletePricePolicy = async (policyId) => {
    if (!window.confirm('Xóa policy này?')) return
    try {
      await axios.delete(`${API_BASE}/PricePolicy/${policyId}`)
      await fetchPricePolicies()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Không xóa được policy (có thể đang được dùng)')
    }
  }

  const saveMembershipPolicy = async () => {
    const p = membershipModal.data
    if (!p.policyId || !p.name || !p.vehicleType) return alert('Nhập đủ PolicyId / Name / VehicleType')
    try {
      if (membershipModal.mode === 'create') {
        await axios.post(`${API_BASE}/Membership/policies`, p)
      } else {
        await axios.put(`${API_BASE}/Membership/policies/${p.policyId}`, p)
      }
      setMembershipModal({ open: false, mode: 'create', data: emptyMembershipPolicy() })
      await fetchMembershipPolicies()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Lưu policy thất bại')
    }
  }

  const deleteMembershipPolicy = async (policyId) => {
    if (!window.confirm('Xóa membership policy này?')) return
    try {
      await axios.delete(`${API_BASE}/Membership/policies/${policyId}`)
      await fetchMembershipPolicies()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.Error || 'Không xóa được policy')
    }
  }

  const handleToggleActive = async (user) => {
    const current = (user.status || '').toLowerCase()
    const nextStatus = current === 'active' ? 'Locked' : 'Active'
    try {
      await axios.patch(`${API_BASE}/UserAccount/${user.userId}/status`, { status: nextStatus })
      await fetchUsers()
    } catch (err) {
      alert(err.response?.data?.error || err.response?.data?.message || 'Lỗi cập nhật trạng thái')
    }
  }

  const roleColors = {
    ADMIN: 'bg-purple-100 text-purple-700',
    ATTENDANT: 'bg-green-100 text-green-700'
  }

  const normalizePricePolicy = (p) => ({
    policyId: p.policyId || p.PolicyId || '',
    name: p.name || p.Name || '',
    vehicleType: (p.vehicleType || p.VehicleType || 'CAR').toUpperCase(),
    ratePerHour: p.ratePerHour ?? p.RatePerHour ?? 0,
    overnightSurcharge: p.overnightSurcharge ?? p.OvernightSurcharge ?? 0,
    dailyMax: p.dailyMax ?? p.DailyMax ?? 0,
    lostTicketFee: p.lostTicketFee ?? p.LostTicketFee ?? 0,
    peakRanges: (p.peakRanges ?? p.PeakRanges ?? []).map(r => ({
      startHour: r.startHour ?? r.StartHour ?? 0,
      endHour: r.endHour ?? r.EndHour ?? 0,
      multiplier: r.multiplier ?? r.Multiplier ?? 1
    }))
  })

  const normalizeMembershipPolicy = (p) => ({
    policyId: p.policyId || p.PolicyId || '',
    name: p.name || p.Name || '',
    vehicleType: (p.vehicleType || p.VehicleType || 'CAR').toUpperCase(),
    monthlyPrice: p.monthlyPrice ?? p.MonthlyPrice ?? 0
  })

  function emptyPricePolicy() {
    return {
      policyId: '',
      name: '',
      vehicleType: 'CAR',
      ratePerHour: 0,
      overnightSurcharge: 0,
      dailyMax: 0,
      lostTicketFee: 0,
      peakRanges: [{ startHour: 17, endHour: 21, multiplier: 1.5 }]
    }
  }

  function emptyMembershipPolicy() {
    return {
      policyId: '',
      name: '',
      vehicleType: 'CAR',
      monthlyPrice: 0
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Quản trị hệ thống</h1>
          <p className="text-sm text-gray-500">Quản lý tài khoản người dùng và cấu hình</p>
        </div>
        <button
          onClick={openCreateModal}
          className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-4 py-2.5 rounded-lg text-sm font-medium transition"
        >
          <UserPlus size={18} />
          Thêm người dùng
        </button>
      </div>

      {/* Stats */}
      <div className="grid sm:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-indigo-100 flex items-center justify-center">
              <Users className="text-indigo-600" size={24} />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-800">{users.length}</p>
              <p className="text-sm text-gray-500">Tổng người dùng</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center">
              <Check className="text-green-600" size={24} />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-800">{users.filter(u => (u.status || '').toLowerCase() === 'active').length}</p>
              <p className="text-sm text-gray-500">Đang hoạt động</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-purple-100 flex items-center justify-center">
              <Shield className="text-purple-600" size={24} />
            </div>
            <div>
              <p className="text-2xl font-bold text-gray-800">{users.filter(u => u.role === 'ADMIN').length}</p>
              <p className="text-sm text-gray-500">Quản trị viên</p>
            </div>
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="p-5 border-b border-gray-100">
          <h3 className="font-semibold text-gray-800">Danh sách người dùng</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-5 py-3 text-sm font-medium text-gray-500">Tên đăng nhập</th>
                <th className="text-left px-5 py-3 text-sm font-medium text-gray-500">Vai trò</th>
                <th className="text-left px-5 py-3 text-sm font-medium text-gray-500">Trạng thái</th>
                <th className="text-center px-5 py-3 text-sm font-medium text-gray-500">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan="4" className="text-center py-8 text-gray-400">Đang tải...</td></tr>
              ) : users.length === 0 ? (
                <tr><td colSpan="4" className="text-center py-8 text-gray-400">Không có người dùng</td></tr>
              ) : (
                users.map(user => (
                  <tr key={user.userId} className="border-t border-gray-50 hover:bg-gray-50">
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-9 h-9 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-600 font-semibold text-sm">
                          {user.username.charAt(0).toUpperCase()}
                        </div>
                        <span className="font-medium text-gray-800">{user.username}</span>
                      </div>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${roleColors[user.role] || 'bg-gray-100 text-gray-600'}`}>
                        {user.role}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <button
                        onClick={() => handleToggleActive(user)}
                        className={`text-xs px-2.5 py-1 rounded-full font-medium transition ${(user.status || '').toLowerCase() === 'active' ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-red-100 text-red-700 hover:bg-red-200'}`}
                      >
                        {(user.status || '').toLowerCase() === 'active' ? 'Hoạt động' : 'Vô hiệu'}
                      </button>
                    </td>
                    <td className="px-5 py-4">
                      <div className="flex items-center justify-center gap-2">
                        <button
                          onClick={() => openEditModal(user)}
                          className="p-2 rounded-lg hover:bg-indigo-50 text-indigo-600 transition"
                          title="Chỉnh sửa"
                        >
                          <Edit size={16} />
                        </button>
                        <button
                          onClick={() => handleDelete(user.userId)}
                          className="p-2 rounded-lg hover:bg-red-50 text-red-500 transition"
                          title="Xóa"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Price Policy */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 rounded-xl bg-amber-100 flex items-center justify-center"><DollarSign className="text-amber-600" size={18} /></div>
            <div>
              <h3 className="font-semibold text-gray-800">Bảng giá gửi xe</h3>
              <p className="text-xs text-gray-500">Quản lý PricePolicy (giờ cao điểm, qua đêm, daily max, lost fee)</p>
            </div>
          </div>
          <button onClick={() => openPriceModal('create')} className="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-600 text-white text-sm font-medium hover:bg-amber-700">
            <PlusCircle size={16} /> Thêm PricePolicy
          </button>
        </div>
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
          {pricePolicies.length === 0 && <div className="text-gray-400 text-sm">Chưa có policy</div>}
          {pricePolicies.map(p => (
            <div key={p.policyId || p.PolicyId} className="border border-gray-100 rounded-lg p-4 hover:border-amber-200">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="text-sm font-semibold text-gray-800">{p.name || p.Name}</div>
                  <div className="text-xs text-gray-500">ID: {p.policyId || p.PolicyId}</div>
                  <div className="text-xs text-gray-500">Loại xe: {(p.vehicleType || p.VehicleType || '').toUpperCase()}</div>
                </div>
                <div className="flex gap-2">
                  <button onClick={() => openPriceModal('edit', normalizePricePolicy(p))} className="p-2 rounded-lg hover:bg-amber-50 text-amber-700" title="Sửa">
                    <Edit3 size={16} />
                  </button>
                  <button onClick={() => deletePricePolicy(p.policyId || p.PolicyId)} className="p-2 rounded-lg hover:bg-red-50 text-red-500" title="Xóa">
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
              <div className="mt-3 space-y-1 text-xs text-gray-600">
                <div>Giá/giờ: {(p.ratePerHour ?? p.RatePerHour ?? 0).toLocaleString('vi-VN')} đ</div>
                <div>Qua đêm: {(p.overnightSurcharge ?? p.OvernightSurcharge ?? 0).toLocaleString('vi-VN')} đ</div>
                <div>Daily max: {(p.dailyMax ?? p.DailyMax ?? 0).toLocaleString('vi-VN')} đ</div>
                <div>Lost fee: {(p.lostTicketFee ?? p.LostTicketFee ?? 0).toLocaleString('vi-VN')} đ</div>
                {(p.peakRanges ?? p.PeakRanges)?.length ? (
                  <div>Peak: {(p.peakRanges ?? p.PeakRanges).map(r => `${r.startHour ?? r.StartHour}-${r.endHour ?? r.EndHour}h x${r.multiplier ?? r.Multiplier}`).join(', ')}</div>
                ) : <div>Peak: --</div>}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Membership Policy */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5 space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-10 h-10 rounded-xl bg-indigo-100 flex items-center justify-center"><Layers className="text-indigo-600" size={18} /></div>
            <div>
              <h3 className="font-semibold text-gray-800">Gói vé tháng (MembershipPolicy)</h3>
              <p className="text-xs text-gray-500">Quản lý giá vé tháng theo loại xe</p>
            </div>
          </div>
          <button onClick={() => openMembershipModal('create')} className="flex items-center gap-2 px-3 py-2 rounded-lg bg-indigo-600 text-white text-sm font-medium hover:bg-indigo-700">
            <PlusCircle size={16} /> Thêm gói
          </button>
        </div>
        <div className="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
          {membershipPolicies.length === 0 && <div className="text-gray-400 text-sm">Chưa có gói</div>}
          {membershipPolicies.map(p => (
            <div key={p.policyId || p.PolicyId} className="border border-gray-100 rounded-lg p-4 hover:border-indigo-200">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="text-sm font-semibold text-gray-800">{p.name || p.Name}</div>
                  <div className="text-xs text-gray-500">ID: {p.policyId || p.PolicyId}</div>
                  <div className="text-xs text-gray-500">Loại xe: {(p.vehicleType || p.VehicleType || '').toUpperCase()}</div>
                </div>
                <div className="flex gap-2">
                  <button onClick={() => openMembershipModal('edit', normalizeMembershipPolicy(p))} className="p-2 rounded-lg hover:bg-indigo-50 text-indigo-700" title="Sửa">
                    <Edit3 size={16} />
                  </button>
                  <button onClick={() => deleteMembershipPolicy(p.policyId || p.PolicyId)} className="p-2 rounded-lg hover:bg-red-50 text-red-500" title="Xóa">
                    <Trash2 size={16} />
                  </button>
                </div>
              </div>
              <div className="mt-3 text-xs text-gray-600">Giá tháng: {(p.monthlyPrice ?? p.MonthlyPrice ?? 0).toLocaleString('vi-VN')} đ</div>
            </div>
          ))}
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between p-5 border-b border-gray-100">
              <h3 className="text-lg font-semibold text-gray-800">
                {editUser ? 'Chỉnh sửa người dùng' : 'Thêm người dùng mới'}
              </h3>
              <button onClick={() => setShowModal(false)} className="p-2 hover:bg-gray-100 rounded-lg transition">
                <X size={20} className="text-gray-400" />
              </button>
            </div>
            <form onSubmit={handleSubmit} className="p-5 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">Tên đăng nhập</label>
                <input
                  type="text"
                  className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm outline-none focus:border-indigo-500"
                  value={form.username}
                  onChange={(e) => setForm({ ...form, username: e.target.value })}
                  disabled={!!editUser}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">
                  {editUser ? 'Mật khẩu mới (bỏ trống nếu không đổi)' : 'Mật khẩu'}
                </label>
                <div className="flex items-center gap-2 bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5">
                  <Key size={16} className="text-gray-400" />
                  <input
                    type="password"
                    className="flex-1 bg-transparent text-sm outline-none"
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">Vai trò</label>
                <select
                  className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2.5 text-sm outline-none focus:border-indigo-500"
                  value={form.role}
                  onChange={(e) => setForm({ ...form, role: e.target.value })}
                >
                  <option value="ADMIN">Admin - Toàn quyền</option>
                  <option value="ATTENDANT">Attendant - Nhân viên</option>
                </select>
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2.5 rounded-lg transition"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2.5 rounded-lg transition"
                >
                  {editUser ? 'Cập nhật' : 'Tạo mới'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* PricePolicy Modal */}
      {priceModal.open && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-xl">
            <div className="flex items-center justify-between p-5 border-b border-gray-100">
              <h3 className="text-lg font-semibold text-gray-800">{priceModal.mode === 'create' ? 'Thêm PricePolicy' : 'Sửa PricePolicy'}</h3>
              <button onClick={() => setPriceModal({ open: false, mode: 'create', data: emptyPricePolicy() })} className="p-2 hover:bg-gray-100 rounded-lg transition"><X size={20} className="text-gray-400" /></button>
            </div>
            <div className="p-5 space-y-4 text-sm text-gray-700">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Mã bảng giá (PolicyId)</label>
                  <input className="w-full border border-gray-200 rounded-lg px-3 py-2" disabled={priceModal.mode === 'edit'} value={priceModal.data.policyId} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, policyId: e.target.value } })} />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Tên bảng giá</label>
                  <input className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.name} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, name: e.target.value } })} />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Loại xe áp dụng</label>
                  <select className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.vehicleType} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, vehicleType: e.target.value } })}>
                    <option value="CAR">Ô tô</option>
                    <option value="ELECTRIC_CAR">Ô tô điện</option>
                    <option value="MOTORBIKE">Xe máy</option>
                    <option value="ELECTRIC_MOTORBIKE">Xe máy điện</option>
                    <option value="BICYCLE">Xe đạp</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Giá theo giờ (đ/giờ)</label>
                  <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.ratePerHour} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, ratePerHour: Number(e.target.value) } })} />
                  <p className="text-[11px] text-gray-500 mt-1">Phí cơ bản mỗi giờ, nhân hệ số loại xe.</p>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Phụ thu qua đêm (đ)</label>
                  <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.overnightSurcharge} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, overnightSurcharge: Number(e.target.value) } })} />
                  <p className="text-[11px] text-gray-500 mt-1">Áp dụng nếu phiên qua ngày (entry != exit date).</p>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Trần phí/ngày (đ)</label>
                  <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.dailyMax} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, dailyMax: Number(e.target.value) } })} />
                  <p className="text-[11px] text-gray-500 mt-1">Nếu &gt; 0 sẽ lấy min(fee, trần).</p>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Phí mất vé (đ)</label>
                  <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2" value={priceModal.data.lostTicketFee} onChange={(e) => setPriceModal({ ...priceModal, data: { ...priceModal.data, lostTicketFee: Number(e.target.value) } })} />
                  <p className="text-[11px] text-gray-500 mt-1">Cộng thêm khi xử lý mất vé.</p>
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-xs text-gray-500">Peak ranges</span>
                  <button type="button" onClick={() => setPriceModal({ ...priceModal, data: { ...priceModal.data, peakRanges: [...(priceModal.data.peakRanges || []), { startHour: 7, endHour: 9, multiplier: 1.2 }] } })} className="text-xs text-amber-600 hover:text-amber-700">+ Add</button>
                </div>
                <div className="space-y-2">
                  {(priceModal.data.peakRanges || []).map((r, idx) => (
                    <div key={idx} className="grid grid-cols-4 gap-2 items-center">
                      <input type="number" min="0" max="24" className="border border-gray-200 rounded-lg px-2 py-1 text-sm" value={r.startHour} onChange={(e) => {
                        let val = Number(e.target.value);
                        if (val < 0) val = 0;
                        if (val > 24) val = 24;
                        const next = [...priceModal.data.peakRanges];
                        next[idx] = { ...next[idx], startHour: val };
                        setPriceModal({ ...priceModal, data: { ...priceModal.data, peakRanges: next } });
                      }} placeholder="Start" />
                      <input type="number" min="0" max="24" className="border border-gray-200 rounded-lg px-2 py-1 text-sm" value={r.endHour} onChange={(e) => {
                        let val = Number(e.target.value);
                        if (val < 0) val = 0;
                        if (val > 24) val = 24;
                        const next = [...priceModal.data.peakRanges];
                        next[idx] = { ...next[idx], endHour: val };
                        setPriceModal({ ...priceModal, data: { ...priceModal.data, peakRanges: next } });
                      }} placeholder="End" />
                      <input type="number" className="border border-gray-200 rounded-lg px-2 py-1 text-sm" value={r.multiplier} onChange={(e) => {
                        const next = [...priceModal.data.peakRanges];
                        next[idx] = { ...next[idx], multiplier: Number(e.target.value) };
                        setPriceModal({ ...priceModal, data: { ...priceModal.data, peakRanges: next } });
                      }} placeholder="x" />
                      <button type="button" onClick={() => {
                        const next = [...priceModal.data.peakRanges];
                        next.splice(idx, 1);
                        setPriceModal({ ...priceModal, data: { ...priceModal.data, peakRanges: next } });
                      }} className="text-xs text-red-500">Xóa</button>
                    </div>
                  ))}
                </div>
              </div>

              <div className="flex gap-3 pt-2">
                <button onClick={() => setPriceModal({ open: false, mode: 'create', data: emptyPricePolicy() })} className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2.5 rounded-lg transition">Hủy</button>
                <button onClick={savePricePolicy} className="flex-1 bg-amber-600 hover:bg-amber-700 text-white font-medium py-2.5 rounded-lg transition">Lưu</button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* MembershipPolicy Modal */}
      {membershipModal.open && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between p-5 border-b border-gray-100">
              <h3 className="text-lg font-semibold text-gray-800">{membershipModal.mode === 'create' ? 'Thêm gói vé tháng' : 'Sửa gói vé tháng'}</h3>
              <button onClick={() => setMembershipModal({ open: false, mode: 'create', data: emptyMembershipPolicy() })} className="p-2 hover:bg-gray-100 rounded-lg transition"><X size={20} className="text-gray-400" /></button>
            </div>
            <div className="p-5 space-y-4 text-sm text-gray-700">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs text-gray-500 mb-1">PolicyId</label>
                  <input className="w-full border border-gray-200 rounded-lg px-3 py-2" disabled={membershipModal.mode === 'edit'} value={membershipModal.data.policyId} onChange={(e) => setMembershipModal({ ...membershipModal, data: { ...membershipModal.data, policyId: e.target.value } })} />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Name</label>
                  <input className="w-full border border-gray-200 rounded-lg px-3 py-2" value={membershipModal.data.name} onChange={(e) => setMembershipModal({ ...membershipModal, data: { ...membershipModal.data, name: e.target.value } })} />
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">VehicleType</label>
                  <select className="w-full border border-gray-200 rounded-lg px-3 py-2" value={membershipModal.data.vehicleType} onChange={(e) => setMembershipModal({ ...membershipModal, data: { ...membershipModal.data, vehicleType: e.target.value } })}>
                    <option value="CAR">CAR</option>
                    <option value="ELECTRIC_CAR">ELECTRIC_CAR</option>
                    <option value="MOTORBIKE">MOTORBIKE</option>
                    <option value="ELECTRIC_MOTORBIKE">ELECTRIC_MOTORBIKE</option>
                    <option value="BICYCLE">BICYCLE</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs text-gray-500 mb-1">Monthly price (đ)</label>
                  <input type="number" className="w-full border border-gray-200 rounded-lg px-3 py-2" value={membershipModal.data.monthlyPrice} onChange={(e) => setMembershipModal({ ...membershipModal, data: { ...membershipModal.data, monthlyPrice: Number(e.target.value) } })} />
                </div>
              </div>

              <div className="flex gap-3 pt-2">
                <button onClick={() => setMembershipModal({ open: false, mode: 'create', data: emptyMembershipPolicy() })} className="flex-1 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2.5 rounded-lg transition">Hủy</button>
                <button onClick={saveMembershipPolicy} className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2.5 rounded-lg transition">Lưu</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
