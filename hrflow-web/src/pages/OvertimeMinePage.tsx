import { useEffect, useState } from 'react'
import { api, type OvertimeRequestDto } from '../lib/api'
import { leaveStatusLabel } from '../lib/status'

export function OvertimeMinePage() {
  const [items, setItems] = useState<OvertimeRequestDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.myOvertimeRequests()
      setItems(res)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Load failed')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  return (
    <div className="stack">
      <div className="rowSpace">
        <h2>OT ของฉัน</h2>
        <button type="button" onClick={() => void load()} disabled={loading}>
          รีเฟรช
        </button>
      </div>
      {error ? <div className="error">{error}</div> : null}
      <div className="card">
        {items.length === 0 ? (
          <div className="muted">ยังไม่มีคำขอ</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>ช่วงเวลา</th>
                <th>สถานะ</th>
                <th>สร้างเมื่อ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((x) => (
                <tr key={x.id}>
                  <td>
                    {x.startAt} ถึง {x.endAt}
                    <div className="muted">{x.reason}</div>
                  </td>
                  <td>{leaveStatusLabel(x.status)}</td>
                  <td>{new Date(x.createdAt).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

