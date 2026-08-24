import { useState } from 'react'
import Schema from './Schema'
import BRD from './BRD'

function App() {
  const [view, setView] = useState('schema')

  return (
    <div style={{ position: 'relative' }}>
      <div style={{
        position: 'fixed',
        bottom: 20,
        right: 20,
        zIndex: 9999,
        display: 'flex',
        gap: 10,
        background: 'rgba(0,0,0,0.8)',
        padding: '10px',
        borderRadius: '30px',
        border: '1px solid #333',
        boxShadow: '0 4px 20px rgba(0,0,0,0.5)'
      }}>
        <button 
          onClick={() => setView('schema')}
          style={{
            padding: '8px 20px',
            borderRadius: '20px',
            border: 'none',
            background: view === 'schema' ? '#1DB954' : 'transparent',
            color: '#fff',
            cursor: 'pointer',
            fontWeight: 'bold',
            transition: 'all 0.2s'
          }}
        >
          🗄️ Schema
        </button>
        <button 
          onClick={() => setView('brd')}
          style={{
            padding: '8px 20px',
            borderRadius: '20px',
            border: 'none',
            background: view === 'brd' ? '#1DB954' : 'transparent',
            color: '#fff',
            cursor: 'pointer',
            fontWeight: 'bold',
            transition: 'all 0.2s'
          }}
        >
          📄 BRD
        </button>
      </div>
      {view === 'schema' ? <Schema /> : <BRD />}
    </div>
  )
}

export default App
