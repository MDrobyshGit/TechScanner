import type { SupportStatus } from '../types/scan'

const statusConfig: Record<SupportStatus, { label: string; className: string }> = {
  Active: { label: 'Active', className: 'badge badge-active' },
  Slowing: { label: 'Slowing', className: 'badge badge-slowing' },
  Abandoned: { label: 'Abandoned', className: 'badge badge-abandoned' },
  Unknown: { label: 'Unknown', className: 'badge badge-unknown' },
}

interface StatusBadgeProps {
  status: SupportStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status] ?? statusConfig.Unknown
  return <span className={config.className}>{config.label}</span>
}
