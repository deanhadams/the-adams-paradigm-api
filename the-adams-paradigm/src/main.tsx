import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { TermsPage } from './pages/TermsPage.tsx'
import { PaystackCallbackPage } from './pages/PaystackCallbackPage.tsx'
import { PayfastReturnPage } from './pages/PayfastReturnPage.tsx'

// The site is a single-page marketing site with no router: the few other
// routes are resolved from the URL on load. The server (see nginx.conf) falls
// back to index.html for unknown paths, so visiting or refreshing them
// directly still renders correctly.
const path = window.location.pathname.replace(/\/+$/, '')
const page =
  path === '/terms' ? <TermsPage />
  : path === '/payment/paystack-callback' ? <PaystackCallbackPage />
  : path === '/payment/payfast-return' ? <PayfastReturnPage />
  : path === '/payment/payfast-cancel' ? <PayfastReturnPage cancelled />
  : <App />

createRoot(document.getElementById('root')!).render(<StrictMode>{page}</StrictMode>)
