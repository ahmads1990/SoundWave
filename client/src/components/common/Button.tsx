import { Loader2 } from 'lucide-react';
import React from 'react';
import { cn } from '../../utils/cn';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'white' | 'outline' | 'ghost' | 'dark';
  size?: 'sm' | 'md' | 'lg' | 'icon';
  isLoading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  className,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled,
  ...props
}) => {
  const baseStyles =
    'inline-flex items-center justify-center font-bold tracking-tight rounded-full transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-spotify-black active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed disabled:active:scale-100';

  const variants = {
    primary:
      'bg-spotify-green hover:bg-spotify-green-hover text-black font-extrabold focus:ring-spotify-green shadow-spotify-green',
    white:
      'bg-spotify-white hover:bg-zinc-200 text-black font-bold focus:ring-white',
    outline:
      'border border-spotify-muted/50 hover:border-white bg-transparent text-white hover:scale-105 focus:ring-white',
    ghost:
      'bg-transparent hover:bg-white/10 text-spotify-muted hover:text-white focus:ring-white',
    dark:
      'bg-spotify-card hover:bg-spotify-card-hover text-white border border-white/5 focus:ring-white/20',
  };

  const sizes = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-6 py-3 text-sm',
    lg: 'px-8 py-3.5 text-base',
    icon: 'p-2.5 rounded-full aspect-square',
  };

  return (
    <button
      className={cn(baseStyles, variants[variant], sizes[size], className)}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
      {children}
    </button>
  );
};
