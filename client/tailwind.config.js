/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        darkBg: '#090D16',
        cardBg: '#111827',
        borderMuted: '#1F2937',
        cardHover: '#161F30'
      }
    },
  },
  plugins: [],
}
