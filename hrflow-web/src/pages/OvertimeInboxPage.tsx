import { useEffect, useState } from 'react'
import { api, type InboxOvertimeRequestDto } from '../lib/api'
import { leaveStatusLabel } from '../lib/status'

export function OvertimeInboxPage() {
  const [items, setItems] = useState<InboxOvertimeRequestDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const load = async () => {
    setLoading(true)
    setError(null)
    try {
      const res = await api.overtimeInbox()
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

  const act = async (id: string, action: 'approve' | 'reject' | 'return') => {
    const comment = window.prompt('หมายเหตุ (ไม่บังคับ)', '') ?? ''
    setLoading(true)
    setError(null)
    try {
      await api.overtimeApprovalAction(id, action, comment)
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Action failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="stack">
      <div className="rowSpace">
        <h2>OT Inbox</h2>
        <button type="button" onClick={() => void load()} disabled={loading}>
          รีเฟรช
        </button>
      </div>
      {error ? <div className="error">{error}</div> : null}
      <div className="card">
        {items.length === 0 ? (
          <div className="muted">ไม่มีรายการรออนุมัติ</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>ผู้ยื่น</th>
                <th>ช่วงเวลา</th>
                <th>สถานะ</th>
                <th>ขั้น</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((x) => (
                <tr key={x.id}>
                  <td>
                    <div>{x.requestorName}</div>
                    <div className="muted">{x.requestorEmail}</div>
                  </td>
                  <td>
                    {x.startAt} ถึง {x.endAt}
                  </td>
                  <td>{leaveStatusLabel(x.status)}</td>
                  <td>L{x.currentLevel}</td>
                  <td className="actions">
                    <button type="button" onClick={() => void act(x.id, 'approve')} disabled={loading}>
                      อนุมัติ
                    </button>
                    <button type="button" onClick={() => void act(x.id, 'return')} disabled={loading}>
                      ตีกลับ
                    </button>
                    <button type="button" onClick={() => void act(x.id, 'reject')} disabled={loading}>
                      ปฏิเสธ
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}

