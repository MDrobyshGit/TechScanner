import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { getScan } from '../services/api'
import { useScanProgress } from '../services/scanProgress'
import { ScanProgressBar } from '../components/ScanProgressBar'
import { TechnologyTable } from '../components/TechnologyTable'
import type { ScanResult } from '../types/scan'

export function ScanPage() {
  const { id } = useParams<{ id: string }>()
  const [scan, setScan] = useState<ScanResult | null>(null)
  const [loading, setLoading] = useState(true)

  const isRunning = scan?.status === 'Queued' || scan?.status === 'Running'
  const { progress, isDone } = useScanProgress(isRunning ? id ?? null : null)

  useEffect(() => {
    if (!id) return
    getScan(id).then(s => { setScan(s); setLoading(false) })
  }, [id])

  // Reload scan once SSE says done
  useEffect(() => {
    if (isDone && id) {
      getScan(id).then(setScan)
    }
  }, [isDone, id])

  if (loading) return <p className="page">Loading...</p>
  if (!scan) return <p className="page">Scan not found.</p>

  return (
    <div className="page">
      <h1>Scan Results</h1>
      <p className="scan-meta">
        <strong>Source:</strong> {scan.sourceType} — {scan.sourceInput}
      </p>
      <p className="scan-meta">
        <strong>Started:</strong> {new Date(scan.createdAt).toLocaleString()}
        {scan.completedAt && <> · <strong>Completed:</strong> {new Date(scan.completedAt).toLocaleString()}</>}
      </p>

      {(scan.status === 'Queued' || scan.status === 'Running') && progress && (
        <ScanProgressBar percent={progress.percent} message={progress.message} />
      )}

      {scan.status === 'Failed' && (
        <div className="error-box">
          <strong>Scan failed:</strong> {scan.errorMessage}
        </div>
      )}

      {scan.status === 'Completed' && (
        <>
          <p>{scan.technologies.length} technologies found.</p>
          <TechnologyTable technologies={scan.technologies} />
        </>
      )}
    </div>
  )
}
