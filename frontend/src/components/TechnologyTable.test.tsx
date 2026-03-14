import { describe, it, expect } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { TechnologyTable } from '../components/TechnologyTable'
import type { ScanTechnology } from '../types/scan'

const makeTech = (overrides: Partial<ScanTechnology>): ScanTechnology => ({
  id: crypto.randomUUID(),
  name: 'lib',
  version: '1.0.0',
  manifestFile: 'package.json',
  isActiveInCode: false,
  supportStatus: 'Unknown',
  lastReleaseDate: null,
  recommendation: null,
  category: null,
  ...overrides,
})

describe('TechnologyTable', () => {
  it('renders all technologies by default', () => {
    const techs = [
      makeTech({ name: 'react', supportStatus: 'Active' }),
      makeTech({ name: 'lodash', supportStatus: 'Abandoned' }),
    ]
    render(<TechnologyTable technologies={techs} />)
    expect(screen.getByText('react')).toBeTruthy()
    expect(screen.getByText('lodash')).toBeTruthy()
  })

  it('filters to only abandoned technologies', () => {
    const techs = [
      makeTech({ name: 'react', supportStatus: 'Active' }),
      makeTech({ name: 'old-lib', supportStatus: 'Abandoned' }),
    ]
    render(<TechnologyTable technologies={techs} />)
    fireEvent.click(screen.getByRole('button', { name: 'Abandoned' }))
    expect(screen.queryByText('react')).toBeNull()
    expect(screen.getByText('old-lib')).toBeTruthy()
  })

  it('shows empty state message when no abandoned techs', () => {
    const techs = [makeTech({ name: 'react', supportStatus: 'Active' })]
    render(<TechnologyTable technologies={techs} />)
    fireEvent.click(screen.getByText('Abandoned'))
    expect(screen.getByText('No abandoned technologies found.')).toBeTruthy()
  })

  it('filters to active-in-code', () => {
    const techs = [
      makeTech({ name: 'react', isActiveInCode: true }),
      makeTech({ name: 'unused', isActiveInCode: false }),
    ]
    render(<TechnologyTable technologies={techs} />)
    fireEvent.click(screen.getByText('Active in code'))
    expect(screen.getByText('react')).toBeTruthy()
    expect(screen.queryByText('unused')).toBeNull()
  })

  it('renders empty list without crashing', () => {
    render(<TechnologyTable technologies={[]} />)
    expect(screen.getByText('No results match the current filter.')).toBeTruthy()
  })
})
