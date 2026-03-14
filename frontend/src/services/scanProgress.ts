import { useEffect, useRef, useState } from 'react'
import type { ScanProgress } from '../types/scan'

interface UseScanProgressResult {
  progress: ScanProgress | null
  isDone: boolean
}

export function useScanProgress(scanId: string | null): UseScanProgressResult {
  const [progress, setProgress] = useState<ScanProgress | null>(null)
  const [isDone, setIsDone] = useState(false)
  const esRef = useRef<EventSource | null>(null)

  useEffect(() => {
    if (!scanId) return
    setIsDone(false)
    setProgress(null)

    const es = new EventSource(`/api/scans/${scanId}/progress`)
    esRef.current = es

    es.onmessage = (event) => {
      try {
        const data: ScanProgress = JSON.parse(event.data)
        setProgress(data)
        if (data.percent >= 100) {
          setIsDone(true)
          es.close()
        }
      } catch {
        // ignore parse errors
      }
    }

    es.onerror = () => {
      setIsDone(true)
      es.close()
    }

    return () => {
      es.close()
    }
  }, [scanId])

  return { progress, isDone }
}
