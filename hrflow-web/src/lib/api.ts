export type UserDto = {
  id: string
  email: string
  displayName: string
  roles: string[]
}

export type LoginResponse = {
  accessToken: string
  user: UserDto
}

export type LeaveTypeDto = {
  id: string
  name: string
  requiresAttachment: boolean
}

export type LeaveRequestStatus =
  | 'Draft'
  | 'Submitted'
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Returned'
  | 'Cancelled'

export type LeaveRequestDto = {
  id: string
  leaveTypeId: string
  leaveTypeName: string
  startDate: string
  endDate: string
  reason: string
  status: number
  createdAt: string
  submittedAt: string | null
}

export type InboxLeaveRequestDto = {
  id: string
  requestorName: string
  requestorEmail: string
  leaveTypeName: string
  startDate: string
  endDate: string
  status: number
  currentLevel: number
}

export type OvertimeRequestDto = {
  id: string
  startAt: string
  endAt: string
  reason: string
  status: number
  createdAt: string
  submittedAt: string | null
}

export type InboxOvertimeRequestDto = {
  id: string
  requestorName: string
  requestorEmail: string
  startAt: string
  endAt: string
  status: number
  currentLevel: number
}

const API_BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? ''

function getToken(): string | null {
  return localStorage.getItem('hrflow_token')
}

async function safeReadText(res: Response): Promise<string> {
  try {
    return await res.text()
  } catch {
    return ''
  }
}

function safeJsonParse<T>(text: string): T | null {
  const trimmed = text.trim()
  if (!trimmed) return null
  try {
    return JSON.parse(trimmed) as T
  } catch {
    return null
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken()
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  })

  if (!res.ok) {
    const contentType = res.headers.get('content-type') ?? ''
    const text = await safeReadText(res)

    if (contentType.includes('application/problem+json') || contentType.includes('application/json')) {
      const problem = safeJsonParse<{ title?: string; detail?: string }>(text)
      const msg = [problem?.title, problem?.detail].filter(Boolean).join(': ')
      throw new Error(msg || text || `HTTP ${res.status}`)
    }

    throw new Error(text || `HTTP ${res.status}`)
  }

  if (res.status === 204) {
    return undefined as T
  }

  const contentType = res.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return (undefined as T)
  }

  const text = await safeReadText(res)
  const json = safeJsonParse<T>(text)
  if (json === null) {
    return undefined as T
  }

  return json
}

export const api = {
  async login(email: string, password: string): Promise<LoginResponse> {
    return await request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
  },
  async me(): Promise<UserDto> {
    return await request<UserDto>('/api/users/me')
  },
  async getLeaveTypes(): Promise<LeaveTypeDto[]> {
    return await request<LeaveTypeDto[]>('/api/leave-types')
  },
  async createLeaveRequest(input: {
    leaveTypeId: string
    startDate: string
    endDate: string
    reason: string
  }): Promise<LeaveRequestDto> {
    return await request<LeaveRequestDto>('/api/leave-requests', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },
  async submitLeaveRequest(id: string): Promise<void> {
    await request<void>(`/api/leave-requests/${id}/submit`, { method: 'POST' })
  },
  async myLeaveRequests(): Promise<LeaveRequestDto[]> {
    return await request<LeaveRequestDto[]>('/api/leave-requests/mine')
  },
  async inbox(): Promise<InboxLeaveRequestDto[]> {
    return await request<InboxLeaveRequestDto[]>('/api/approvals/inbox')
  },
  async approvalAction(id: string, action: 'approve' | 'reject' | 'return', comment?: string): Promise<void> {
    await request<void>(`/api/approvals/leave-requests/${id}/action`, {
      method: 'POST',
      body: JSON.stringify({ action, comment: comment || null }),
    })
  },

  async createOvertimeRequest(input: { startAt: string; endAt: string; reason: string }): Promise<OvertimeRequestDto> {
    return await request<OvertimeRequestDto>('/api/overtime-requests', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  },
  async submitOvertimeRequest(id: string): Promise<void> {
    await request<void>(`/api/overtime-requests/${id}/submit`, { method: 'POST' })
  },
  async myOvertimeRequests(): Promise<OvertimeRequestDto[]> {
    return await request<OvertimeRequestDto[]>('/api/overtime-requests/mine')
  },
  async overtimeInbox(): Promise<InboxOvertimeRequestDto[]> {
    return await request<InboxOvertimeRequestDto[]>('/api/overtime-approvals/inbox')
  },
  async overtimeApprovalAction(id: string, action: 'approve' | 'reject' | 'return', comment?: string): Promise<void> {
    await request<void>(`/api/overtime-approvals/overtime-requests/${id}/action`, {
      method: 'POST',
      body: JSON.stringify({ action, comment: comment || null }),
    })
  },
}
