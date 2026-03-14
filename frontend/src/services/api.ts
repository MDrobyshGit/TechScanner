import axios from 'axios'
import type { ScanResult, ScanSummary, StartScanRequest } from '../types/scan'

const http = axios.create({ baseURL: '/api' })

export const startScan = (req: StartScanRequest) =>
  http.post<{ scanId: string }>('/scans', req).then(r => r.data)

export const getScan = (id: string) =>
  http.get<ScanResult>(`/scans/${id}`).then(r => r.data)

export const getHistory = (limit = 20) =>
  http.get<ScanSummary[]>('/scans', { params: { limit } }).then(r => r.data)

export const deleteScan = (id: string) =>
  http.delete(`/scans/${id}`)

export const uploadZip = (file: File) => {
  const form = new FormData()
  form.append('file', file)
  return http.post<{ tempFilePath: string }>('/upload', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }).then(r => r.data)
}
