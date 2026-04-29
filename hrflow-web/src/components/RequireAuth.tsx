import { Navigate, Outlet } from 'react-router-dom'
import { isLoggedIn } from '../lib/session'

export function RequireAuth() {
  if (!isLoggedIn()) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

