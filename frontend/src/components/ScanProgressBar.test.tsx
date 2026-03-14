import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ScanProgressBar } from '../components/ScanProgressBar'

describe('ScanProgressBar', () => {
  it('renders percent and message', () => {
    render(<ScanProgressBar percent={42} message="Parsing manifests" />)
    expect(screen.getByText('Parsing manifests — 42%')).toBeTruthy()
  })

  it('clamps width to 100% at completion', () => {
    const { container } = render(<ScanProgressBar percent={100} message="Done" />)
    const bar = container.querySelector('.progress-bar') as HTMLElement
    expect(bar.style.width).toBe('100%')
  })

  it('shows zero percent', () => {
    render(<ScanProgressBar percent={0} message="Starting..." />)
    expect(screen.getByText('Starting... — 0%')).toBeTruthy()
  })
})
