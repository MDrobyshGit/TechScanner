export type ScanStatus = 'Queued' | 'Running' | 'Completed' | 'Failed'
export type SourceType = 'LocalFolder' | 'ZipArchive' | 'GitRepository'
export type SupportStatus = 'Active' | 'Slowing' | 'Abandoned' | 'Unknown'

export interface ScanTechnology {
  id: string
  name: string
  version: string | null
  manifestFile: string
  isActiveInCode: boolean
  supportStatus: SupportStatus
  lastReleaseDate: string | null
  recommendation: string | null
  category: string | null
}

export interface ScanResult {
  id: string
  status: ScanStatus
  sourceType: SourceType
  sourceInput: string
  createdAt: string
  completedAt: string | null
  errorMessage: string | null
  technologies: ScanTechnology[]
}

export interface ScanSummary {
  id: string
  status: ScanStatus
  sourceType: SourceType
  sourceInput: string
  createdAt: string
  completedAt: string | null
  technologyCount: number
  recommendationCount: number
}

export interface StartScanRequest {
  sourceType: SourceType
  sourceInput: string
  gitToken?: string
}

export interface ScanProgress {
  percent: number
  message: string
}
