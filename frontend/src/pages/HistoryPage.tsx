import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getHistory, deleteScan } from '../services/api'
import type { ScanSummary } from '../types/scan'

export function HistoryPage() {
  const [scans, setScans] = useState<ScanSummary[]>([])
  const [loading, setLoading] = useState(true)

  const load = () => getHistory(20).then(s => { setScans(s); setLoading(false) })

  useEffect(() => { load() }, [])

  const handleDelete = async (id: string) => {
    if (!window.confirm('Delete this scan record?')) return
    await deleteScan(id)
    setScans(prev => prev.filter(s => s.id !== id))
  }

  if (loading) return <p className="page">Loading history...</p>

  return (
    <div className="page">
      <h1>Scan History</h1>
      {scans.length === 0 ? (
        <p>No scans yet. <Link to="/">Start a scan</Link>.</p>
      ) : (
        <div className="history-list">
          {scans.map(s => (
            <div key={s.id} className="history-card">
              <Link to={`/scans/${s.id}`} className="history-link">
                <div className="history-source">{s.sourceType}: {s.sourceInput}</div>
                <div className="history-meta">
                  {new Date(s.createdAt).toLocaleString()} · Status: {s.status}
                  {s.status === 'Completed' && (
                    <> · {s.technologyCount} technologies · {s.recommendationCount} recommendations</>
                  )}
                </div>
              </Link>
              <button className="btn-danger" onClick={() => handleDelete(s.id)}>Delete</button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
