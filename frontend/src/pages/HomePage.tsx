import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { startScan, uploadZip } from '../services/api'
import type { SourceType } from '../types/scan'

type Tab = 'local' | 'zip' | 'git'

export function HomePage() {
  const navigate = useNavigate()
  const [tab, setTab] = useState<Tab>('local')
  const [localPath, setLocalPath] = useState('')
  const [zipFile, setZipFile] = useState<File | null>(null)
  const [gitUrl, setGitUrl] = useState('')
  const [gitToken, setGitToken] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleScan = async () => {
    setError(null)
    setLoading(true)
    try {
      if (tab === 'local') {
        if (!localPath.trim()) { setError('Enter a local folder path.'); return }
        const { scanId } = await startScan({ sourceType: 'LocalFolder', sourceInput: localPath.trim() })
        navigate(`/scans/${scanId}`)
      } else if (tab === 'zip') {
        if (!zipFile) { setError('Select a ZIP file.'); return }
        const { tempFilePath } = await uploadZip(zipFile)
        const { scanId } = await startScan({ sourceType: 'ZipArchive', sourceInput: tempFilePath })
        navigate(`/scans/${scanId}`)
      } else {
        if (!gitUrl.trim()) { setError('Enter a repository URL.'); return }
        const { scanId } = await startScan({
          sourceType: 'GitRepository',
          sourceInput: gitUrl.trim(),
          gitToken: gitToken || undefined,
        })
        navigate(`/scans/${scanId}`)
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'An error occurred.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="page">
      <h1>Scan a Project</h1>
      <div className="tabs">
        {(['local', 'zip', 'git'] as Tab[]).map(t => (
          <button key={t} className={`tab${tab === t ? ' active' : ''}`} onClick={() => setTab(t)}>
            {t === 'local' ? 'Local Path' : t === 'zip' ? 'ZIP Archive' : 'Git Repository'}
          </button>
        ))}
      </div>

      <div className="tab-content">
        {tab === 'local' && (
          <input
            className="input"
            value={localPath}
            onChange={e => setLocalPath(e.target.value)}
            placeholder="C:\path\to\project or /home/user/project"
          />
        )}
        {tab === 'zip' && (
          <div className="drop-zone">
            <input
              type="file"
              accept=".zip"
              onChange={e => setZipFile(e.target.files?.[0] ?? null)}
            />
            {zipFile && <p>Selected: {zipFile.name}</p>}
          </div>
        )}
        {tab === 'git' && (
          <>
            <input
              className="input"
              value={gitUrl}
              onChange={e => setGitUrl(e.target.value)}
              placeholder="https://github.com/owner/repo"
            />
            <input
              className="input"
              type="password"
              value={gitToken}
              onChange={e => setGitToken(e.target.value)}
              placeholder="Personal access token (optional, for private repos)"
              autoComplete="off"
            />
          </>
        )}
      </div>

      {error && <p className="error">{error}</p>}

      <button className="btn-primary" onClick={handleScan} disabled={loading}>
        {loading ? 'Starting...' : 'Scan'}
      </button>
    </div>
  )
}
