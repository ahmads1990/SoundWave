import React from 'react';
import { cn } from '../../utils/cn';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  icon?: React.ReactNode;
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, label, error, icon, ...props }, ref) => {
    return (
      <div className="w-full space-y-1.5 text-left">
        {label && (
          <label className="block text-xs font-bold uppercase tracking-wider text-spotify-muted">
            {label}
          </label>
        )}
        <div className="relative flex items-center">
          {icon && (
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-spotify-muted">
              {icon}
            </div>
          )}
          <input
            ref={ref}
            className={cn(
              'w-full rounded-md bg-[#242424] px-3.5 py-2.5 text-sm text-white placeholder-spotify-muted/70 transition-all border border-transparent focus:border-white/40 focus:bg-[#2a2a2a] focus:outline-none focus:ring-1 focus:ring-white/40',
              icon && 'pl-10',
              error && 'border-red-500 focus:border-red-500 focus:ring-red-500/30',
              className
            )}
            {...props}
          />
        </div>
        {error && <p className="text-xs font-medium text-red-400">{error}</p>}
      </div>
    );
  }
);

Input.displayName = 'Input';
