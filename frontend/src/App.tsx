import { BrowserRouter, Link, NavLink, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HomePage } from './pages/HomePage'
import { ScanPage } from './pages/ScanPage'
import { HistoryPage } from './pages/HistoryPage'
import './App.css'

const queryClient = new QueryClient()

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <nav className="navbar">
          <Link to="/" className="logo">🔍 TechScanner</Link>
          <div className="nav-links">
            <NavLink to="/" end className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              New Scan
            </NavLink>
            <NavLink to="/history" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
              History
            </NavLink>
          </div>
        </nav>
        <main>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/scans/:id" element={<ScanPage />} />
            <Route path="/history" element={<HistoryPage />} />
          </Routes>
        </main>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App

