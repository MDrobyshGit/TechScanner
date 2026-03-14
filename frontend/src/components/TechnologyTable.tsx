import { useState } from 'react'
import type { ScanTechnology, SupportStatus } from '../types/scan'
import { StatusBadge } from './StatusBadge'

type FilterOption = 'all' | 'active-in-code' | 'needs-update' | 'abandoned'

interface TechnologyTableProps {
  technologies: ScanTechnology[]
}

export function TechnologyTable({ technologies }: TechnologyTableProps) {
  const [filter, setFilter] = useState<FilterOption>('all')
  const [sortField, setSortField] = useState<'name' | 'supportStatus'>('name')

  const filtered = technologies.filter(t => {
    if (filter === 'active-in-code') return t.isActiveInCode
    if (filter === 'needs-update') return !!t.recommendation
    if (filter === 'abandoned') return t.supportStatus === 'Abandoned'
    return true
  })

  const sorted = [...filtered].sort((a, b) => {
    if (sortField === 'supportStatus') {
      const order: Record<SupportStatus, number> = { Abandoned: 0, Slowing: 1, Unknown: 2, Active: 3 }
      return (order[a.supportStatus] ?? 2) - (order[b.supportStatus] ?? 2)
    }
    return a.name.localeCompare(b.name)
  })

  return (
    <div>
      <div className="table-filters">
        {(['all', 'active-in-code', 'needs-update', 'abandoned'] as FilterOption[]).map(opt => (
          <button
            key={opt}
            className={`filter-btn${filter === opt ? ' active' : ''}`}
            onClick={() => setFilter(opt)}
          >
            {opt === 'all' ? 'All' : opt === 'active-in-code' ? 'Active in code' : opt === 'needs-update' ? 'Needs update' : 'Abandoned'}
          </button>
        ))}
        <button className="filter-btn" onClick={() => setSortField(s => s === 'name' ? 'supportStatus' : 'name')}>
          Sort by {sortField === 'name' ? 'status' : 'name'}
        </button>
      </div>

      {sorted.length === 0 ? (
        <p className="empty-state">
          {filter === 'abandoned' ? 'No abandoned technologies found.' : 'No results match the current filter.'}
        </p>
      ) : (
        <table className="tech-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Version</th>
              <th>Manifest</th>
              <th>In Code</th>
              <th>Support</th>
              <th>Last Release</th>
              <th>Recommendation</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map(t => (
              <tr key={t.id}>
                <td className="tech-name">{t.name}</td>
                <td>{t.version ?? '—'}</td>
                <td className="manifest-file" title={t.manifestFile}>{t.manifestFile}</td>
                <td className="center">{t.isActiveInCode ? '✓' : '✗'}</td>
                <td><StatusBadge status={t.supportStatus} /></td>
                <td>{t.lastReleaseDate ?? '—'}</td>
                <td>{t.recommendation ?? ''}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
