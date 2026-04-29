import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'

export function OvertimeNewPage() {
  const navigate = useNavigate()
  const [startAt, setStartAt] = useState('')
  const [endAt, setEndAt] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  return (
    <div className="stack">
      <h2>ขอ OT</h2>
      <form
        className="card"
        onSubmit={async (e) => {
          e.preventDefault()
          setLoading(true)
          setError(null)
          try {
            const created = await api.createOvertimeRequest({ startAt, endAt, reason })
            await api.submitOvertimeRequest(created.id)
            navigate('/ot/mine')
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Submit failed')
          } finally {
            setLoading(false)
          }
        }}
      >
        <div className="row">
          <label className="field">
            <span>เริ่ม</span>
            <input type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} />
          </label>
          <label className="field">
            <span>สิ้นสุด</span>
            <input type="datetime-local" value={endAt} onChange={(e) => setEndAt(e.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>เหตุผล</span>
          <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} />
        </label>
        {error ? <div className="error">{error}</div> : null}
        <button type="submit" disabled={loading}>
          {loading ? 'กำลังส่ง...' : 'ส่งขออนุมัติ OT'}
        </button>
      </form>
    </div>
  )
}

