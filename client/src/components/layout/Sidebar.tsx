import {
  Compass,
  Heart,
  Home,
  Library,
  Plus,
  Radio,
  Search,
  Sparkles,
} from 'lucide-react';
import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { cn } from '../../utils/cn';

interface SidebarProps {
  className?: string;
}

export const Sidebar: React.FC<SidebarProps> = ({ className }) => {
  const location = useLocation();
  const { user } = useAuth();
  const [filter, setFilter] = useState<'all' | 'playlists' | 'artists' | 'albums'>('all');

  const mainNav = [
    { name: 'Home', href: '/', icon: Home },
    { name: 'Search', href: '/search', icon: Search },
    { name: 'Discover', href: '/search', icon: Compass },
  ];

  const demoPlaylists = [
    {
      id: 'liked-songs',
      title: 'Liked Songs',
      type: 'Playlist • 34 songs',
      icon: Heart,
      isLiked: true,
    },
    {
      id: 'p1',
      title: 'Synthwave & Cyberpunk Vibes',
      type: 'Playlist • SoundWave',
      imageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=100&q=80',
    },
    {
      id: 'p2',
      title: 'Deep Focus & Coding Beats',
      type: 'Playlist • SoundWave',
      imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=100&q=80',
    },
    {
      id: 'p3',
      title: 'Late Night Lo-Fi Chill',
      type: 'Playlist • SoundWave',
      imageUrl: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=100&q=80',
    },
    {
      id: 'p4',
      title: 'Top Hits 2026',
      type: 'Playlist • SoundWave',
      imageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=100&q=80',
    },
  ];

  return (
    <aside
      className={cn(
        'flex flex-col gap-2 w-72 h-full bg-spotify-black p-2 text-spotify-muted select-none',
        className
      )}
    >
      {/* Top Navigation Block */}
      <div className="flex flex-col gap-1 rounded-lg bg-spotify-base p-3">
        {/* Brand Logo */}
        <Link to="/" className="flex items-center gap-2.5 px-3 py-2 text-white font-extrabold text-lg tracking-tight">
          <div className="flex items-center justify-center w-8 h-8 rounded-full bg-spotify-green text-black font-black">
            <Radio className="w-5 h-5 text-black fill-black" />
          </div>
          <span className="font-['Outfit'] tracking-wide text-xl">SoundWave</span>
        </Link>

        <nav className="mt-2 flex flex-col gap-1 font-semibold text-sm">
          {mainNav.map((item) => {
            const isActive = location.pathname === item.href;
            const Icon = item.icon;
            return (
              <Link
                key={item.name}
                to={item.href}
                className={cn(
                  'flex items-center gap-4 px-3 py-2.5 rounded-md transition-colors duration-200',
                  isActive
                    ? 'text-white bg-white/10 font-bold'
                    : 'text-spotify-muted hover:text-white'
                )}
              >
                <Icon className={cn('w-6 h-6', isActive ? 'text-spotify-green' : '')} />
                <span>{item.name}</span>
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Your Library Block */}
      <div className="flex flex-col flex-1 rounded-lg bg-spotify-base p-2 overflow-hidden">
        {/* Library Header */}
        <div className="flex items-center justify-between px-3 py-2">
          <Link
            to="/library"
            className="flex items-center gap-3 text-spotify-muted hover:text-white font-bold text-sm transition-colors"
          >
            <Library className="w-6 h-6" />
            <span>Your Library</span>
          </Link>
          <div className="flex items-center gap-1">
            <button
              title="Create playlist or folder"
              className="p-1.5 rounded-full text-spotify-muted hover:text-white hover:bg-white/10 transition-colors"
            >
              <Plus className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Filter Chips */}
        <div className="flex items-center gap-2 px-3 py-2 overflow-x-auto no-scrollbar">
          {(['all', 'playlists', 'artists', 'albums'] as const).map((tag) => (
            <button
              key={tag}
              onClick={() => setFilter(tag)}
              className={cn(
                'px-3 py-1 rounded-full text-xs font-semibold capitalize transition-colors',
                filter === tag
                  ? 'bg-white text-black font-bold'
                  : 'bg-white/10 text-white hover:bg-white/20'
              )}
            >
              {tag}
            </button>
          ))}
        </div>

        {/* Playlists List */}
        <div className="flex-1 overflow-y-auto px-1 py-1 space-y-1 mt-1">
          {demoPlaylists.map((pl) => (
            <Link
              key={pl.id}
              to={`/playlist/${pl.id}`}
              className="flex items-center gap-3 p-2 rounded-md hover:bg-white/5 transition-colors group cursor-pointer"
            >
              {pl.isLiked ? (
                <div className="flex items-center justify-center w-12 h-12 rounded bg-gradient-to-br from-indigo-600 to-emerald-400 text-white shadow-sm flex-shrink-0">
                  <Heart className="w-6 h-6 fill-white" />
                </div>
              ) : (
                <img
                  src={pl.imageUrl}
                  alt={pl.title}
                  className="w-12 h-12 rounded object-cover flex-shrink-0 shadow-sm"
                />
              )}
              <div className="flex flex-col min-w-0 flex-1">
                <span className="text-sm font-semibold text-white truncate group-hover:text-spotify-green transition-colors">
                  {pl.title}
                </span>
                <span className="text-xs text-spotify-muted truncate">{pl.type}</span>
              </div>
            </Link>
          ))}
        </div>

        {/* Artist Banner Callout if Listener */}
        {user?.role === 'Listener' && (
          <div className="mt-auto p-3 rounded-lg bg-[#242424] border border-white/5 flex flex-col gap-2">
            <div className="flex items-center gap-2 text-xs font-bold text-spotify-green uppercase tracking-wide">
              <Sparkles className="w-4 h-4" />
              <span>Are you an Artist?</span>
            </div>
            <p className="text-xs text-spotify-muted leading-relaxed">
              Upload your tracks, manage releases, and check streaming analytics.
            </p>
            <Link
              to="/apply-artist"
              className="mt-1 text-center py-1.5 px-3 rounded-full bg-white text-black font-bold text-xs hover:scale-105 transition-transform"
            >
              Apply for Artist Account
            </Link>
          </div>
        )}
      </div>
    </aside>
  );
};
