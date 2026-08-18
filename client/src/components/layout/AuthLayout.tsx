import { Radio } from 'lucide-react';
import React from 'react';
import { Link, Outlet } from 'react-router-dom';

export const AuthLayout: React.FC = () => {
  return (
    <div className="min-h-screen w-full bg-gradient-to-b from-zinc-900 via-spotify-black to-spotify-black flex flex-col items-center justify-between p-6">
      {/* Top Header Logo */}
      <header className="w-full max-w-sm flex items-center justify-center py-6">
        <Link to="/" className="flex items-center gap-2.5 text-white font-extrabold text-2xl tracking-tight">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-spotify-green text-black font-black">
            <Radio className="w-6 h-6 text-black fill-black" />
          </div>
          <span className="font-['Outfit'] tracking-wide text-2xl">SoundWave</span>
        </Link>
      </header>

      {/* Centered Auth Card */}
      <main className="w-full max-w-md bg-spotify-base p-8 sm:p-10 rounded-xl border border-white/10 shadow-spotify-card my-auto">
        <Outlet />
      </main>

      {/* Footer */}
      <footer className="w-full text-center py-6 text-xs text-spotify-muted">
        <p>This site is protected by reCAPTCHA and the Google Privacy Policy and Terms of Service apply.</p>
        <div className="flex items-center justify-center gap-4 mt-2">
          <Link to="/" className="hover:underline hover:text-white">Home</Link>
          <Link to="/login" className="hover:underline hover:text-white">Log in</Link>
          <Link to="/register" className="hover:underline hover:text-white">Sign up</Link>
        </div>
      </footer>
    </div>
  );
};
