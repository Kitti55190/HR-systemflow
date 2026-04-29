import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, type LeaveTypeDto } from '../lib/api'

export function LeaveNewPage() {
  const navigate = useNavigate()
  const [types, setTypes] = useState<LeaveTypeDto[]>([])
  const [leaveTypeId, setLeaveTypeId] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    api
      .getLeaveTypes()
      .then((x) => {
        setTypes(x)
        if (x.length > 0) setLeaveTypeId(x[0].id)
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Load failed'))
  }, [])

  const selectedType = useMemo(() => types.find((t) => t.id === leaveTypeId) ?? null, [types, leaveTypeId])

  return (
    <div className="stack">
      <h2>ยื่นใบลา</h2>
      <form
        className="card"
        onSubmit={async (e) => {
          e.preventDefault()
          setLoading(true)
          setError(null)
          try {
            const created = await api.createLeaveRequest({ leaveTypeId, startDate, endDate, reason })
            await api.submitLeaveRequest(created.id)
            navigate('/leave/mine')
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Submit failed')
          } finally {
            setLoading(false)
          }
        }}
      >
        <label className="field">
          <span>ประเภทลา</span>
          <select value={leaveTypeId} onChange={(e) => setLeaveTypeId(e.target.value)}>
            {types.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
        <div className="row">
          <label className="field">
            <span>เริ่ม</span>
            <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} />
          </label>
          <label className="field">
            <span>สิ้นสุด</span>
            <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} />
          </label>
        </div>
        <label className="field">
          <span>เหตุผล</span>
          <textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} />
        </label>
        {selectedType?.requiresAttachment ? <div className="warn">ประเภทนี้กำหนดให้แนบไฟล์ (MVP ยังไม่ทำอัปโหลด)</div> : null}
        {error ? <div className="error">{error}</div> : null}
        <button type="submit" disabled={loading}>
          {loading ? 'กำลังส่ง...' : 'ส่งขออนุมัติ'}
        </button>
      </form>
    </div>
  )
}

