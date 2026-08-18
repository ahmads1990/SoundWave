import { AlertCircle, Lock, Mail } from 'lucide-react';
import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../../components/common/Button';
import { Input } from '../../components/common/Input';
import { useAuth } from '../../contexts/AuthContext';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

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
          onChange={(e) => setEmail(e.target.value)}
          icon={<Mail className="w-4 h-4" />}
          autoComplete="email"
          required
        />

        <Input
          label="Password"
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
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
  );
};
