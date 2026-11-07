import {
  AlertCircle,
  Check,
  ChevronLeft,
  ChevronRight,
  Headphones,
  Lock,
  Mail,
  Music,
  Shield,
  Sparkles,
} from 'lucide-react';
import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../../components/common/Button';
import { Input } from '../../components/common/Input';
import { useAuth } from '../../contexts/AuthContext';

interface DemoAccount {
  role: 'Admin' | 'Artist' | 'Listener';
  name: string;
  email: string;
  password: string;
  description: string;
  badgeColor: string;
}

const DEMO_ACCOUNTS: DemoAccount[] = [
  {
    role: 'Admin',
    name: 'System Admin',
    email: 'admin@soundwave.com',
    password: 'Password123!',
    description: 'Full moderation & platform admin',
    badgeColor: 'bg-purple-500/20 text-purple-300 border-purple-500/30',
  },
  {
    role: 'Artist',
    name: 'Luna Echo',
    email: 'artist@soundwave.com',
    password: 'Password123!',
    description: 'Music catalog & artist studio',
    badgeColor: 'bg-spotify-green/20 text-spotify-green border-spotify-green/30',
  },
  {
    role: 'Listener',
    name: 'Alex Rivera',
    email: 'listener@soundwave.com',
    password: 'Password123!',
    description: 'Streaming & playlists exploration',
    badgeColor: 'bg-sky-500/20 text-sky-300 border-sky-500/30',
  },
];

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [selectedRole, setSelectedRole] = useState<string | null>(null);
  const [isCollapsed, setIsCollapsed] = useState(false);

  const handleSelectDemo = (account: DemoAccount) => {
    setEmail(account.email);
    setPassword(account.password);
    setSelectedRole(account.role);
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!email || !password) {
      setError('Please enter both email and password.');
      return;
    }

    try {
      setLoading(true);
      await login({ email, password });
      navigate('/');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Invalid credentials. Please try again.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      {/* Floating Demo Accounts Helper on the Left */}
      {isCollapsed ? (
        <button
          type="button"
          onClick={() => setIsCollapsed(false)}
          className="fixed left-3 sm:left-6 top-1/2 -translate-y-1/2 z-40 flex items-center gap-2 px-3 py-2.5 rounded-full bg-spotify-card border border-white/15 hover:border-spotify-green/50 shadow-2xl text-xs font-semibold text-white transition-all hover:scale-105 group backdrop-blur-md"
          title="Show Demo Accounts"
          aria-label="Show Demo Accounts"
        >
          <Sparkles className="w-4 h-4 text-spotify-green animate-pulse" />
          <span className="hidden sm:inline">Demo Logins</span>
          <ChevronRight className="w-3.5 h-3.5 text-spotify-muted group-hover:text-white" />
        </button>
      ) : (
        <aside
          aria-label="Demo credentials helper"
          className="fixed left-3 sm:left-6 top-1/2 -translate-y-1/2 z-40 w-72 sm:w-80 bg-[#121212]/95 backdrop-blur-xl border border-white/15 rounded-2xl p-4 shadow-2xl text-white transition-all animate-in fade-in slide-in-from-left-4 duration-200"
        >
          <div className="flex items-center justify-between pb-3 border-b border-white/10">
            <div className="flex items-center gap-2">
              <div className="w-7 h-7 rounded-lg bg-spotify-green/20 border border-spotify-green/30 flex items-center justify-center">
                <Sparkles className="w-4 h-4 text-spotify-green" />
              </div>
              <div>
                <h3 className="text-xs font-bold uppercase tracking-wider text-white">Demo Logins</h3>
                <p className="text-[10px] text-spotify-muted">1-click auto-fill credentials</p>
              </div>
            </div>
            <button
              type="button"
              onClick={() => setIsCollapsed(true)}
              className="p-1 rounded-md text-spotify-muted hover:text-white hover:bg-white/10 transition-colors"
              title="Minimize demo panel"
              aria-label="Minimize"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
          </div>

          <div className="space-y-2 mt-3">
            {DEMO_ACCOUNTS.map((acc) => {
              const isSelected = selectedRole === acc.role;
              return (
                <button
                  key={acc.role}
                  type="button"
                  onClick={() => handleSelectDemo(acc)}
                  className={`w-full text-left p-2.5 rounded-xl border transition-all text-xs flex flex-col gap-1 group relative ${
                    isSelected
                      ? 'bg-white/10 border-spotify-green shadow-md shadow-spotify-green/10'
                      : 'bg-[#181818]/80 hover:bg-[#222222] border-white/5 hover:border-white/20'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      {acc.role === 'Admin' && <Shield className="w-3.5 h-3.5 text-purple-400" />}
                      {acc.role === 'Artist' && <Music className="w-3.5 h-3.5 text-spotify-green" />}
                      {acc.role === 'Listener' && <Headphones className="w-3.5 h-3.5 text-sky-400" />}
                      <span className="font-semibold text-white">{acc.name}</span>
                    </div>
                    <span className={`text-[10px] px-1.5 py-0.5 rounded-md font-medium border ${acc.badgeColor}`}>
                      {acc.role}
                    </span>
                  </div>

                  <div className="flex items-center justify-between text-[11px] text-spotify-muted group-hover:text-white">
                    <span className="truncate">{acc.email}</span>
                    {isSelected ? (
                      <span className="flex items-center gap-1 text-[10px] text-spotify-green font-semibold">
                        <Check className="w-3 h-3" /> Selected
                      </span>
                    ) : (
                      <span className="text-[10px] text-spotify-muted group-hover:text-spotify-green transition-colors">
                        Auto-fill &rarr;
                      </span>
                    )}
                  </div>
                  <div className="text-[10px] text-spotify-subdued">{acc.description}</div>
                </button>
              );
            })}
          </div>

          <div className="mt-3 pt-2.5 border-t border-white/5 flex items-center justify-between text-[10px] text-spotify-muted">
            <span>Password for all:</span>
            <code className="bg-black/50 px-1.5 py-0.5 rounded text-white font-mono text-[11px]">
              Password123!
            </code>
          </div>
        </aside>
      )}

      {/* Main Login Form Container */}
      <div className="space-y-6">
        <div className="text-center space-y-2">
          <h1 className="text-3xl font-black text-white tracking-tight">Log in to SoundWave</h1>
          <p className="text-xs text-spotify-muted">Enter your email address and password to continue</p>
        </div>

        {error && (
          <div className="flex items-center gap-2.5 p-3 rounded-md bg-red-900/40 border border-red-500/50 text-red-200 text-xs font-medium animate-in fade-in">
            <AlertCircle className="w-4 h-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Email address"
            type="email"
            placeholder="name@domain.com"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value);
              setSelectedRole(null);
            }}
            icon={<Mail className="w-4 h-4" />}
            autoComplete="email"
            required
          />

          <Input
            label="Password"
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => {
              setPassword(e.target.value);
              setSelectedRole(null);
            }}
            icon={<Lock className="w-4 h-4" />}
            autoComplete="current-password"
            required
          />

          <div className="flex items-center justify-between text-xs">
            <label className="flex items-center gap-2 cursor-pointer text-spotify-muted hover:text-white">
              <input type="checkbox" className="rounded bg-[#242424] border-white/20 text-spotify-green focus:ring-0" />
              <span>Remember me</span>
            </label>
            <Link to="/forgot-password" className="text-white hover:underline hover:text-spotify-green">
              Forgot your password?
            </Link>
          </div>

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full mt-2"
            isLoading={loading}
          >
            Log In
          </Button>
        </form>

        <div className="border-t border-white/10 pt-6 text-center text-xs text-spotify-muted">
          <span>Don't have an account? </span>
          <Link to="/register" className="text-white font-bold hover:underline hover:text-spotify-green">
            Sign up for SoundWave
          </Link>
        </div>
      </div>
    </>
  );
};
