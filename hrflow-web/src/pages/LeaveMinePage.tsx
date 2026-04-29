import { useEffect, useState } from 'react'
import { api, type LeaveRequestDto } from '../lib/api'
import { leaveStatusLabel } from '../lib/status'

export function LeaveMinePage() {
  const [items, setItems] = useState<LeaveRequestDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.myLeaveRequests()
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
        <h2>คำขอของฉัน</h2>
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
                <th>ประเภท</th>
                <th>ช่วงวันที่</th>
                <th>สถานะ</th>
                <th>สร้างเมื่อ</th>
              </tr>
            </thead>
            <tbody>
              {items.map((x) => (
                <tr key={x.id}>
                  <td>{x.leaveTypeName}</td>
                  <td>
                    {x.startDate} ถึง {x.endDate}
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

