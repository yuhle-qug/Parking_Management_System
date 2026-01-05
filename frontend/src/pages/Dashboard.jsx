export default function Dashboard() {
  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-gray-800">Dashboard</h1>
      <div className="grid md:grid-cols-2 gap-4">
        <div className="p-4 bg-white rounded-xl shadow border border-gray-100">
          <p className="text-sm text-gray-500">Luồng xe</p>
          <p className="text-lg font-semibold text-gray-800">Sắp cập nhật</p>
        </div>
        <div className="p-4 bg-white rounded-xl shadow border border-gray-100">
          <p className="text-sm text-gray-500">Thông tin bãi</p>
          <p className="text-lg font-semibold text-gray-800">Sắp cập nhật</p>
        </div>
      </div>
    </div>
  )
}
