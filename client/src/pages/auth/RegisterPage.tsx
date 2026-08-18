import { AlertCircle, CheckCircle2, Lock, Mail, User } from 'lucide-react';
import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Button } from '../../components/common/Button';
import { Input } from '../../components/common/Input';
import { useAuth } from '../../contexts/AuthContext';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    if (password.length < 6) {
      setError('Password must be at least 6 characters long.');
      return;
    }

    try {
      setLoading(true);
      await register({
        userName,
        email,
        password,
        confirmPassword,
      });
      navigate('/');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Registration failed. Please try again.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-black text-white tracking-tight">Sign up to start listening</h1>
        <p className="text-xs text-spotify-muted">Create your free SoundWave account today</p>
      </div>

      {error && (
        <div className="flex items-center gap-2.5 p-3 rounded-md bg-red-900/40 border border-red-500/50 text-red-200 text-xs font-medium animate-in fade-in">
          <AlertCircle className="w-4 h-4 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Username"
          type="text"
          placeholder="What should we call you?"
          value={userName}
          onChange={(e) => setUserName(e.target.value)}
          icon={<User className="w-4 h-4" />}
          autoComplete="username"
          required
        />

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
          label="Create a password"
          type="password"
          placeholder="Create a password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          icon={<Lock className="w-4 h-4" />}
          autoComplete="new-password"
          required
        />

        <Input
          label="Confirm password"
          type="password"
          placeholder="Repeat your password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          icon={<Lock className="w-4 h-4" />}
          autoComplete="new-password"
          required
        />

        <div className="space-y-2 pt-2">
          <div className="flex items-start gap-2 text-xs text-spotify-muted">
            <CheckCircle2 className="w-4 h-4 text-spotify-green flex-shrink-0 mt-0.5" />
            <span>High-quality streaming music and playlists</span>
          </div>
          <div className="flex items-start gap-2 text-xs text-spotify-muted">
            <CheckCircle2 className="w-4 h-4 text-spotify-green flex-shrink-0 mt-0.5" />
            <span>Artist profile application and release management</span>
          </div>
        </div>

        <Button
          type="submit"
          variant="primary"
          size="lg"
          className="w-full mt-4"
          isLoading={loading}
        >
          Sign Up
        </Button>
      </form>

      <div className="border-t border-white/10 pt-6 text-center text-xs text-spotify-muted">
        <span>Already have an account? </span>
        <Link to="/login" className="text-white font-bold hover:underline hover:text-spotify-green">
          Log in here
        </Link>
      </div>
    </div>
  );
};
