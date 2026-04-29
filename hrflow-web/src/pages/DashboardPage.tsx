import { Link } from 'react-router-dom'
import { getSessionUser } from '../lib/session'

export function DashboardPage() {
  const user = getSessionUser()

  return (
    <div className="stack">
      <h2>สวัสดี{user ? `, ${user.displayName}` : ''}</h2>
      <div className="grid">
        <Link className="tile" to="/leave/new">
          ยื่นใบลา
        </Link>
        <Link className="tile" to="/leave/mine">
          ดูคำขอของฉัน
        </Link>
        <Link className="tile" to="/ot/new">
          ขอ OT
        </Link>
        <Link className="tile" to="/ot/mine">
          ดู OT ของฉัน
        </Link>
        <Link className="tile" to="/approvals/inbox">
          กล่องอนุมัติ
        </Link>
        <Link className="tile" to="/ot/approvals/inbox">
          OT Inbox
        </Link>
      </div>
    </div>
  )
}
