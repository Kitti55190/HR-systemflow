import type { UserDto } from './api'

export function setSession(token: string, user: UserDto): void {
  localStorage.setItem('hrflow_token', token)
  localStorage.setItem('hrflow_user', JSON.stringify(user))
}

export function clearSession(): void {
  localStorage.removeItem('hrflow_token')
  localStorage.removeItem('hrflow_user')
}

export function getSessionUser(): UserDto | null {
  const raw = localStorage.getItem('hrflow_user')
  if (!raw) return null
  try {
    return JSON.parse(raw) as UserDto
  } catch {
    return null
  }
}

export function isLoggedIn(): boolean {
  return !!localStorage.getItem('hrflow_token')
}
