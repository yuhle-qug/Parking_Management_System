import { useEffect, useState } from 'react'
import axios from 'axios'
import { Users, UserPlus, Shield, Trash2, Edit, X, Check, Settings, Key } from 'lucide-react'
import { API_BASE } from '../config/api'

export default function Admin() {
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(false)
  const [showModal, setShowModal] = useState(false)
  const [editUser, setEditUser] = useState(null)
  const [form, setForm] = useState({ username: '', password: '', role: 'ATTENDANT' })

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

  useEffect(() => { fetchUsers() }, [])

  const openCreateModal = () => {
    setEditUser(null)
    setForm({ username: '', password: '', role: 'ATTENDANT' })
    setShowModal(true)
  }

  const openEditModal = (user) => {
    alert('Backend chưa hỗ trợ cập nhật user. Hiện chỉ hỗ trợ tạo user và xem danh sách.')
  }

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!form.username || (!editUser && !form.password)) {
      return alert('Vui lòng điền đầy đủ thông tin')
    }
    try {
      if (editUser) {
        alert('Backend chưa hỗ trợ cập nhật user. Hiện chỉ hỗ trợ tạo user và xem danh sách.')
        return
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
    </div>
  )
}
