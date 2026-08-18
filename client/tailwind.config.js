/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        spotify: {
          green: '#1DB954',
          'green-hover': '#1ED760',
          'green-dark': '#169c46',
          black: '#000000',
          base: '#121212',
          card: '#181818',
          'card-hover': '#282828',
          highlight: '#2a2a2a',
          divider: '#242424',
          subdued: '#a7a7a7',
          muted: '#b3b3b3',
          white: '#ffffff',
        },
      },
      fontFamily: {
        sans: [
          'CircularStd',
          'Inter',
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'Roboto',
          'Helvetica Neue',
          'Arial',
          'sans-serif',
        ],
      },
      boxShadow: {
        'spotify-card': '0 8px 24px rgba(0,0,0,0.5)',
        'spotify-green': '0 8px 16px rgba(29, 185, 84, 0.3)',
      },
    },
  },
  plugins: [],
};
