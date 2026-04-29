import { BrowserRouter, Route, Routes } from 'react-router-dom'
import './App.css'
import { AppShell } from './components/AppShell'
import { ErrorBoundary } from './components/ErrorBoundary'
import { RequireAuth } from './components/RequireAuth'
import { DashboardPage } from './pages/DashboardPage'
import { InboxPage } from './pages/InboxPage'
import { LeaveMinePage } from './pages/LeaveMinePage'
import { LeaveNewPage } from './pages/LeaveNewPage'
import { LoginPage } from './pages/LoginPage'
import { OvertimeInboxPage } from './pages/OvertimeInboxPage'
import { OvertimeMinePage } from './pages/OvertimeMinePage'
import { OvertimeNewPage } from './pages/OvertimeNewPage'

export default function App() {
  return (
    <div className="app">
      <ErrorBoundary>
        <BrowserRouter>
          <Routes>
            <Route
              path="/login"
              element={
                <main className="content">
                  <LoginPage />
                </main>
              }
            />
            <Route element={<RequireAuth />}>
              <Route element={<AppShell />}>
                <Route path="/" element={<DashboardPage />} />
                <Route path="/leave/new" element={<LeaveNewPage />} />
                <Route path="/leave/mine" element={<LeaveMinePage />} />
                <Route path="/approvals/inbox" element={<InboxPage />} />
                <Route path="/ot/new" element={<OvertimeNewPage />} />
                <Route path="/ot/mine" element={<OvertimeMinePage />} />
                <Route path="/ot/approvals/inbox" element={<OvertimeInboxPage />} />
              </Route>
            </Route>
            <Route
              path="*"
              element={
                <main className="content">
                  <div className="card">ไม่พบหน้านี้</div>
                </main>
              }
            />
          </Routes>
        </BrowserRouter>
      </ErrorBoundary>
    </div>
  )
}
