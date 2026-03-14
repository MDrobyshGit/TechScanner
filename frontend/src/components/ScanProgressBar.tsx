interface ScanProgressBarProps {
  percent: number
  message: string
}

export function ScanProgressBar({ percent, message }: ScanProgressBarProps) {
  return (
    <div className="progress-container">
      <div className="progress-bar-wrapper">
        <div className="progress-bar" style={{ width: `${Math.min(percent, 100)}%` }} />
      </div>
      <p className="progress-message">{message} — {percent}%</p>
    </div>
  )
}
