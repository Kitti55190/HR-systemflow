import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import { setSession } from '../lib/session'

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('employee@demo.com')
  const [password, setPassword] = useState('Password1!')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  return (
    <div className="stack">
      <div className="card">
      <h1>HrFlow</h1>
      <p className="muted">Demo Accounts: employee@demo.com / manager@demo.com / hr@demo.com (Password1!)</p>

      <form
        onSubmit={async (e) => {
          e.preventDefault()
          setLoading(true)
          setError(null)
          try {
            const res = await api.login(email, password)
            setSession(res.accessToken, res.user)
            navigate('/')
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Login failed')
          } finally {
            setLoading(false)
          }
        }}
      >
        <label className="field">
          <span>Email</span>
          <input value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="username" />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
        </label>
        {error ? <div className="error">{error}</div> : null}
        <button type="submit" disabled={loading}>
          {loading ? 'กำลังเข้าสู่ระบบ...' : 'เข้าสู่ระบบ'}
        </button>
      </form>
    </div>
    </div>
  )
}
