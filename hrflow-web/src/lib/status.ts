export function leaveStatusLabel(status: number): string {
  switch (status) {
    case 0:
      return 'Draft'
    case 1:
      return 'Submitted'
    case 2:
      return 'Pending'
    case 3:
      return 'Approved'
    case 4:
      return 'Rejected'
    case 5:
      return 'Returned'
    case 6:
      return 'Cancelled'
    default:
      return `Unknown(${status})`
  }
}

