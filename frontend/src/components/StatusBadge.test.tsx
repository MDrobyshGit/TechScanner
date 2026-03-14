import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { StatusBadge } from '../components/StatusBadge'

describe('StatusBadge', () => {
  it('renders Active with active class', () => {
    const { container } = render(<StatusBadge status="Active" />)
    const badge = container.firstChild as HTMLElement
    expect(badge.textContent).toBe('Active')
    expect(badge.className).toContain('badge-active')
  })

  it('renders Slowing with slowing class', () => {
    const { container } = render(<StatusBadge status="Slowing" />)
    const badge = container.firstChild as HTMLElement
    expect(badge.className).toContain('badge-slowing')
  })

  it('renders Abandoned with abandoned class', () => {
    const { container } = render(<StatusBadge status="Abandoned" />)
    const badge = container.firstChild as HTMLElement
    expect(badge.className).toContain('badge-abandoned')
  })

  it('renders Unknown with unknown class', () => {
    const { container } = render(<StatusBadge status="Unknown" />)
    const badge = container.firstChild as HTMLElement
    expect(badge.className).toContain('badge-unknown')
  })
})
