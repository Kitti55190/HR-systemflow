import type { ReactNode } from 'react'
import { Component } from 'react'

type Props = {
  children: ReactNode
}

type State = {
  error: unknown
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: unknown): State {
    return { error }
  }

  render() {
    if (this.state.error) {
      const message = this.state.error instanceof Error ? this.state.error.message : String(this.state.error)
      return (
        <main className="content">
          <div className="error">เกิดข้อผิดพลาดในหน้าเว็บ: {message}</div>
        </main>
      )
    }

    return this.props.children
  }
}

