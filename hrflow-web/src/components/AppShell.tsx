import { Link, Outlet, useNavigate } from 'react-router-dom'
import { clearSession, getSessionUser } from '../lib/session'

export function AppShell() {
  const navigate = useNavigate()
  const user = getSessionUser()

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <Link to="/">HrFlow</Link>
        </div>
        <nav className="nav">
          <Link to="/leave/new">ยื่นลา</Link>
          <Link to="/leave/mine">คำขอของฉัน</Link>
          <Link to="/ot/new">ขอ OT</Link>
          <Link to="/ot/mine">OT ของฉัน</Link>
          <Link to="/approvals/inbox">กล่องอนุมัติ</Link>
          <Link to="/ot/approvals/inbox">OT Inbox</Link>
        </nav>
        <div className="user">
          <span className="userName">{user?.displayName ?? ''}</span>
          <button
            type="button"
            onClick={() => {
              clearSession()
              navigate('/login')
            }}
          >
            ออกจากระบบ
          </button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
