import { Heart, Plus } from 'lucide-react';
import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { cn } from '../../utils/cn';

export const LibraryPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'playlists' | 'artists' | 'albums'>('playlists');

  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      {/* Header & Tabs */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          {(['playlists', 'artists', 'albums'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={cn(
                'px-4 py-2 rounded-full text-xs font-bold capitalize transition-colors',
                activeTab === tab
                  ? 'bg-white text-black font-extrabold'
                  : 'bg-white/10 text-white hover:bg-white/20'
              )}
            >
              {tab}
            </button>
          ))}
        </div>

        <button className="flex items-center gap-2 px-4 py-2 rounded-full bg-white/10 hover:bg-white/20 text-white text-xs font-bold transition-colors">
          <Plus className="w-4 h-4" />
          <span>New Playlist</span>
        </button>
      </div>

      {/* Grid of Saved Items */}
      {activeTab === 'playlists' && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {/* Liked Songs Special Tile */}
          <Link
            to="/playlist/liked-songs"
            className="col-span-2 bg-gradient-to-br from-indigo-700 via-indigo-900 to-emerald-800 p-5 rounded-lg flex flex-col justify-end relative group shadow-lg cursor-pointer min-h-[220px]"
          >
            <div className="flex flex-col gap-2">
              <Heart className="w-8 h-8 fill-white text-white" />
              <h3 className="text-2xl font-black text-white">Liked Songs</h3>
              <p className="text-xs text-white/80 font-medium">34 liked songs</p>
            </div>
          </Link>

          {[
            {
              id: 'p1',
              title: 'Synthwave & Cyberpunk Vibes',
              creator: 'SoundWave',
              image: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
            },
            {
              id: 'p2',
              title: 'Deep Focus & Coding Beats',
              creator: 'SoundWave',
              image: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80',
            },
            {
              id: 'p3',
              title: 'Late Night Lo-Fi Chill',
              creator: 'SoundWave',
              image: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
            },
          ].map((pl) => (
            <Link
              key={pl.id}
              to={`/playlist/${pl.id}`}
              className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col"
            >
              <div className="relative mb-4 w-full aspect-square rounded-md overflow-hidden shadow-lg bg-black/40">
                <img src={pl.image} alt={pl.title} className="w-full h-full object-cover" />
              </div>
              <h3 className="font-bold text-sm text-white truncate mb-1">{pl.title}</h3>
              <p className="text-xs text-spotify-muted truncate">By {pl.creator}</p>
            </Link>
          ))}
        </div>
      )}

      {activeTab === 'artists' && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {[
            { id: 'artist-1', name: 'Luna Waves', image: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&q=80' },
            { id: 'artist-2', name: 'Solar Pulse', image: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80' },
          ].map((art) => (
            <Link
              key={art.id}
              to={`/artist/${art.id}`}
              className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col items-center"
            >
              <div className="relative mb-4 w-full aspect-square rounded-full overflow-hidden shadow-lg bg-black/40">
                <img src={art.image} alt={art.name} className="w-full h-full object-cover" />
              </div>
              <h3 className="font-bold text-sm text-white truncate mb-1 w-full text-center">{art.name}</h3>
              <p className="text-xs text-spotify-muted">Artist</p>
            </Link>
          ))}
        </div>
      )}

      {activeTab === 'albums' && (
        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {[
            { title: 'Neon Horizon', artist: 'Luna Waves', image: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80' },
            { title: 'Cybernetic Symphony', artist: 'Solar Pulse', image: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80' },
          ].map((alb, idx) => (
            <div
              key={idx}
              className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col"
            >
              <div className="relative mb-4 w-full aspect-square rounded-md overflow-hidden shadow-lg bg-black/40">
                <img src={alb.image} alt={alb.title} className="w-full h-full object-cover" />
              </div>
              <h3 className="font-bold text-sm text-white truncate mb-1">{alb.title}</h3>
              <p className="text-xs text-spotify-muted truncate">{alb.artist}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
