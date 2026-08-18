import { Play } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { usePlayer } from '../../contexts/PlayerContext';
import { catalogService } from '../../services/catalogService';
import { GenreDto } from '../../types/catalog.types';

const DEFAULT_GENRE_TILES = [
  { id: 'pop', name: 'Pop', color: 'from-pink-600 to-rose-500', image: 'https://images.unsplash.com/photo-1514525253161-7a46d19cd819?w=300&q=80' },
  { id: 'hiphop', name: 'Hip-Hop', color: 'from-amber-600 to-orange-500', image: 'https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=300&q=80' },
  { id: 'electronic', name: 'Electronic / Dance', color: 'from-blue-600 to-cyan-500', image: 'https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=300&q=80' },
  { id: 'rock', name: 'Rock & Metal', color: 'from-red-700 to-red-500', image: 'https://images.unsplash.com/photo-1498038432885-c6f3f1b912ee?w=300&q=80' },
  { id: 'chill', name: 'Chill & Lo-Fi', color: 'from-indigo-600 to-purple-500', image: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=300&q=80' },
  { id: 'indie', name: 'Indie & Alt', color: 'from-emerald-700 to-teal-500', image: 'https://images.unsplash.com/photo-1465847899084-d164df4dedc6?w=300&q=80' },
  { id: 'gaming', name: 'Gaming Soundtracks', color: 'from-violet-700 to-fuchsia-600', image: 'https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=300&q=80' },
  { id: 'synthwave', name: 'Synthwave & Retro', color: 'from-purple-800 to-pink-600', image: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=300&q=80' },
  { id: 'focus', name: 'Focus & Coding', color: 'from-slate-700 to-zinc-600', image: 'https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=300&q=80' },
  { id: 'jazz', name: 'Jazz & Soul', color: 'from-yellow-700 to-amber-600', image: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=300&q=80' },
  { id: 'classical', name: 'Classical & Cinematic', color: 'from-stone-700 to-neutral-500', image: 'https://images.unsplash.com/photo-1507838153414-b4b713384a76?w=300&q=80' },
  { id: 'ambient', name: 'Ambient & Sleep', color: 'from-cyan-800 to-blue-700', image: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=300&q=80' },
];

export const SearchPage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const query = searchParams.get('q') || '';
  const { playTrack } = usePlayer();
  const [genres, setGenres] = useState<GenreDto[]>([]);

  useEffect(() => {
    const loadGenres = async () => {
      try {
        const res = await catalogService.getGenres({ pageSize: 50 });
        if (res.items && res.items.length > 0) {
          setGenres(res.items);
        }
      } catch {
        // Fallback to default tiles if API is not yet running
      }
    };
    loadGenres();
  }, []);

  return (
    <div className="space-y-6 animate-in fade-in duration-300">
      {query ? (
        // Search Results View
        <div className="space-y-6">
          <h2 className="text-2xl font-bold text-white">
            Results for <span className="text-spotify-green">"{query}"</span>
          </h2>

          {/* Top Result + Songs Split View */}
          <div className="grid grid-cols-1 md:grid-cols-5 gap-6">
            {/* Top Result Card */}
            <div className="md:col-span-2 bg-spotify-card hover:bg-spotify-card-hover p-5 rounded-lg transition-colors group relative cursor-pointer flex flex-col justify-between">
              <div>
                <span className="text-xs font-bold text-spotify-muted uppercase tracking-wider mb-3 block">
                  Top result
                </span>
                <div className="w-24 h-24 rounded-md overflow-hidden shadow-lg mb-4 bg-black/40">
                  <img
                    src="https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80"
                    alt="Luna Waves"
                    className="w-full h-full object-cover"
                  />
                </div>
                <h3 className="text-2xl font-extrabold text-white mb-1">Luna Waves</h3>
                <div className="flex items-center gap-2 text-sm text-spotify-muted">
                  <span className="px-2 py-0.5 rounded-full bg-black/40 text-xs font-bold text-white">
                    Artist
                  </span>
                  <span>1.4M monthly listeners</span>
                </div>
              </div>

              {/* Floating Green Play Button */}
              <button
                onClick={() =>
                  playTrack({
                    id: 'track-1',
                    title: 'Midnight Echoes',
                    artistId: 'artist-1',
                    artistName: 'Luna Waves',
                    durationSeconds: 214,
                    playCount: 1420500,
                    isExplicit: false,
                    coverImageUrl:
                      'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
                  })
                }
                className="self-end mt-4 flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green text-black shadow-spotify-card opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0 group-hover:scale-105 transition-all duration-200 hover:bg-spotify-green-hover"
              >
                <Play className="w-5 h-5 fill-black text-black ml-0.5" />
              </button>
            </div>

            {/* Songs Result List */}
            <div className="md:col-span-3 space-y-2">
              <span className="text-xs font-bold text-spotify-muted uppercase tracking-wider mb-2 block">
                Songs
              </span>
              {[1, 2, 3, 4].map((i) => (
                <div
                  key={i}
                  onClick={() =>
                    playTrack({
                      id: `search-track-${i}`,
                      title: `Echoes of the Night Vol. ${i}`,
                      artistId: 'artist-1',
                      artistName: 'Luna Waves',
                      durationSeconds: 195,
                      playCount: 840000,
                      isExplicit: false,
                      coverImageUrl:
                        'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=200&q=80',
                    })
                  }
                  className="flex items-center justify-between p-2 rounded-md hover:bg-white/10 transition-colors group cursor-pointer"
                >
                  <div className="flex items-center gap-3">
                    <img
                      src="https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=200&q=80"
                      alt="Song"
                      className="w-10 h-10 rounded object-cover shadow-sm"
                    />
                    <div>
                      <h4 className="text-sm font-semibold text-white group-hover:text-spotify-green transition-colors">
                        Echoes of the Night Vol. {i}
                      </h4>
                      <p className="text-xs text-spotify-muted">Luna Waves</p>
                    </div>
                  </div>
                  <span className="text-xs text-spotify-muted tabular-nums">3:15</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : (
        // Browse All Categories / Genres Grid
        <div className="space-y-4">
          <h2 className="text-2xl font-bold tracking-tight text-white">Browse all</h2>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {(genres.length > 0
              ? genres.map((g, idx) => ({
                  id: g.id,
                  name: g.name,
                  color: DEFAULT_GENRE_TILES[idx % DEFAULT_GENRE_TILES.length].color,
                  image: g.imageUrl || DEFAULT_GENRE_TILES[idx % DEFAULT_GENRE_TILES.length].image,
                }))
              : DEFAULT_GENRE_TILES
            ).map((tile) => (
              <div
                key={tile.id}
                className={`relative overflow-hidden rounded-lg p-4 h-40 bg-gradient-to-br ${tile.color} shadow-md hover:scale-[1.02] transition-transform duration-200 cursor-pointer`}
              >
                <h3 className="text-xl font-extrabold text-white tracking-tight break-words max-w-[80%]">
                  {tile.name}
                </h3>
                <img
                  src={tile.image}
                  alt={tile.name}
                  className="absolute -right-4 -bottom-2 w-20 h-20 object-cover rounded rotate-[25deg] shadow-2xl pointer-events-none"
                />
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
