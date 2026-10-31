import { useEffect, useState } from 'react'

function App() {
  const [status, setStatus] = useState('checking...')

  useEffect(() => {
    fetch('/health')
      .then(async (r) => {
       const text = await r.text()
       // Vite returns 502 with an empty body when the backend is unreachable.
       setStatus(r.ok || text !== '' ? text : 'API unreachable')
    }).catch(() => setStatus('API unreachable'))
  }, [])

  return (
    <main>
      <h1>doc-qa</h1>
      <p>API status: {status}</p>
    </main>
  )
}

export default App
