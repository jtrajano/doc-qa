import { useEffect, useState } from 'react'

function App() {
  const [status, setStatus] = useState('checking...')

  useEffect(() => {
    fetch('/health')
      .then((r) => r.text())
      .then(setStatus)
      .catch(() => setStatus('API unreachable'))
  }, [])

  return (
    <main>
      <h1>doc-qa</h1>
      <p>API status: {status}</p>
    </main>
  )
}

export default App
