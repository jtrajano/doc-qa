import { render, screen } from '@testing-library/react'
import { expect, it, vi } from 'vitest'
import App from './App'

it('shows the API health status', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue({ text: () => Promise.resolve('Healthy') }),
  )
  render(<App />)
  expect(await screen.findByText(/Healthy/)).toBeInTheDocument()
})
