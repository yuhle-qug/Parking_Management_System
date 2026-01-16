import axios from 'axios'
import { useEffect, useState } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Membership from './pages/Membership'
import Report from './pages/Report'
import Admin from './pages/Admin'
import CheckIn from './pages/CheckIn'
import CheckOut from './pages/CheckOut'
import MainLayout from './layouts/MainLayout'

// Global interceptor for 401 handling
// Note: Ideally this should be in a separate auth setup, but putting here for simplicity
const setupAxios = (logoutFn) => {
  axios.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response && error.response.status === 401) {
        logoutFn();
      }
      return Promise.reject(error);
    }
  );
};

function App() {
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('user')
    return saved ? JSON.parse(saved) : null
  })

  useEffect(() => {
    if (user) {
      localStorage.setItem('user', JSON.stringify(user))
      // Set token from user object (support both cases just in case)
      const token = user.token || user.Token;
      if (token) {
        axios.defaults.headers.common['Authorization'] = `Bearer ${token}`
      }
    } else {
      delete axios.defaults.headers.common['Authorization']
    }
  }, [user])

  // Initial setup for interceptors
  useState(() => {
    setupAxios(() => {
      localStorage.removeItem('user')
      setUser(null)
    });
  });

  const handleLogin = (userData) => {
    setUser(userData)
  }

  const handleLogout = () => {
    localStorage.removeItem('user')
    setUser(null)
  }

  return (
    <BrowserRouter>
      <Routes>
        {!user ? (
          <Route path="*" element={<Login onLogin={handleLogin} />} />
        ) : (
          <Route element={<MainLayout user={user} onLogout={handleLogout} />}>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/membership" element={<Membership />} />
            <Route path="/checkin" element={<CheckIn />} />
            <Route path="/checkout" element={<CheckOut />} />
            {user.role?.toUpperCase() === 'ADMIN' && (
              <>
                <Route path="/report" element={<Report />} />
                <Route path="/admin" element={<Admin />} />
              </>
            )}
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Route>
        )}
      </Routes>
    </BrowserRouter>
  )
}

export default App
