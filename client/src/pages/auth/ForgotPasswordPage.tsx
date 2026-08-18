import { AlertCircle, CheckCircle2, Mail } from 'lucide-react';
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../components/common/Button';
import { Input } from '../../components/common/Input';
import { authService } from '../../services/authService';

export const ForgotPasswordPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      setLoading(true);
      await authService.forgotPassword({ email });
      setSubmitted(true);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Failed to send reset link.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-black text-white tracking-tight">Reset your password</h1>
        <p className="text-xs text-spotify-muted">
          Enter your email address and we'll send you a link to reset your password
        </p>
      </div>

      {submitted ? (
        <div className="space-y-4 text-center">
          <div className="flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green/20 text-spotify-green mx-auto">
            <CheckCircle2 className="w-6 h-6" />
          </div>
          <p className="text-sm text-white font-medium">
            If an account matches <span className="font-bold text-spotify-green">{email}</span>, a password reset link has been sent.
          </p>
          <Link to="/login">
            <Button variant="outline" size="md" className="w-full mt-4">
              Return to login
            </Button>
          </Link>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="flex items-center gap-2.5 p-3 rounded-md bg-red-900/40 border border-red-500/50 text-red-200 text-xs font-medium animate-in fade-in">
              <AlertCircle className="w-4 h-4 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <Input
            label="Email address"
            type="email"
            placeholder="name@domain.com"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            icon={<Mail className="w-4 h-4" />}
            required
          />

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full mt-2"
            isLoading={loading}
          >
            Send Reset Link
          </Button>

          <div className="text-center pt-2">
            <Link to="/login" className="text-xs text-spotify-muted hover:text-white hover:underline">
              Remember your password? Log in
            </Link>
          </div>
        </form>
      )}
    </div>
  );
};
