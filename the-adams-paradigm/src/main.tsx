import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { TermsPage } from './pages/TermsPage.tsx'

// The site is a single-page marketing site with no router: the only other
// route is this one static page, resolved from the URL on load. The server
// (see nginx.conf) falls back to index.html for unknown paths, so visiting
// or refreshing /terms directly still renders this correctly.
const page = window.location.pathname.replace(/\/+$/, '') === '/terms' ? <TermsPage /> : <App />

createRoot(document.getElementById('root')!).render(<StrictMode>{page}</StrictMode>)
