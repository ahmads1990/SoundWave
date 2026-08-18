import { Radio } from 'lucide-react';
import React from 'react';
import { Link } from 'react-router-dom';
import { Button } from '../../components/common/Button';

export const NotFoundPage: React.FC = () => {
  return (
    <div className="flex flex-col items-center justify-center min-h-[70vh] text-center space-y-6 animate-in fade-in duration-300">
      <div className="flex items-center justify-center w-20 h-20 rounded-full bg-white/5 text-spotify-green">
        <Radio className="w-10 h-10" />
      </div>

      <div className="space-y-2">
        <h1 className="text-4xl md:text-5xl font-black text-white">Page not found</h1>
        <p className="text-sm text-spotify-muted max-w-sm mx-auto">
          We can't seem to find the page you are looking for. Try searching for something else.
        </p>
      </div>

      <Link to="/">
        <Button variant="white" size="lg">
          Home
        </Button>
      </Link>
    </div>
  );
};
