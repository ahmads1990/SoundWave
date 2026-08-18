import { Heart, Play } from 'lucide-react';
import React from 'react';
import { Link } from 'react-router-dom';
import { usePlayer } from '../../contexts/PlayerContext';
import { TrackDto } from '../../types/catalog.types';

export const HomePage: React.FC = () => {
  const { playTrack } = usePlayer();

  const getGreeting = () => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  };

  const quickPicks = [
    {
      id: 'quick-liked',
      title: 'Liked Songs',
      imageUrl: 'https://images.unsplash.com/photo-1518609878373-06d740f60d8b?w=200&q=80',
      isLiked: true,
      track: {
        id: 'track-1',
        title: 'Midnight Echoes',
        artistId: 'artist-1',
        artistName: 'Luna Waves',
        albumId: 'album-1',
        albumTitle: 'Neon Horizon',
        coverImageUrl: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
        durationSeconds: 214,
        playCount: 1420500,
        isExplicit: false,
      },
    },
    {
      id: 'quick-2',
      title: 'Synthwave & Cyberpunk Vibes',
      imageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=200&q=80',
      track: {
        id: 'track-2',
        title: 'Electric Dreams',
        artistId: 'artist-2',
        artistName: 'Solar Pulse',
        albumId: 'album-2',
        albumTitle: 'Cybernetic Symphony',
        coverImageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80',
        durationSeconds: 198,
        playCount: 980200,
        isExplicit: true,
      },
    },
    {
      id: 'quick-3',
      title: 'Deep Focus Beats',
      imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=200&q=80',
      track: {
        id: 'track-3',
        title: 'Velvet Sky',
        artistId: 'artist-3',
        artistName: 'Astral Drift',
        albumId: 'album-3',
        albumTitle: 'Starlight Memories',
        coverImageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=400&q=80',
        durationSeconds: 245,
        playCount: 2310000,
        isExplicit: false,
      },
    },
    {
      id: 'quick-4',
      title: 'Late Night Lo-Fi',
      imageUrl: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=200&q=80',
      track: {
        id: 'track-4',
        title: 'Neon Skyline',
        artistId: 'artist-4',
        artistName: 'Vapor Wave',
        durationSeconds: 182,
        playCount: 540000,
        isExplicit: false,
        coverImageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
      },
    },
    {
      id: 'quick-5',
      title: 'Top Hits 2026',
      imageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=200&q=80',
      track: {
        id: 'track-5',
        title: 'Starlight Odyssey',
        artistId: 'artist-1',
        artistName: 'Luna Waves',
        durationSeconds: 230,
        playCount: 3100000,
        isExplicit: false,
        coverImageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=400&q=80',
      },
    },
    {
      id: 'quick-6',
      title: 'Chill Melodies',
      imageUrl: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=200&q=80',
      track: {
        id: 'track-6',
        title: 'Sunset Mirage',
        artistId: 'artist-2',
        artistName: 'Solar Pulse',
        durationSeconds: 205,
        playCount: 890000,
        isExplicit: false,
        coverImageUrl: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=400&q=80',
      },
    },
  ];

  const featuredCards = [
    {
      id: 'card-1',
      title: 'Synthwave Radio',
      description: 'With Luna Waves, Solar Pulse, Astral Drift and more',
      imageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
      type: 'Playlist',
      track: quickPicks[0].track,
    },
    {
      id: 'card-2',
      title: 'Cybernetic Symphony',
      description: 'Solar Pulse • 2026 Album',
      imageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80',
      type: 'Album',
      track: quickPicks[1].track,
    },
    {
      id: 'card-3',
      title: 'Daily Mix 1',
      description: 'Vapor Wave, Luna Waves, and electronic chill beats',
      imageUrl: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
      type: 'Playlist',
      track: quickPicks[2].track,
    },
    {
      id: 'card-4',
      title: 'All Out 2020s',
      description: 'The biggest songs of the decade so far.',
      imageUrl: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=400&q=80',
      type: 'Playlist',
      track: quickPicks[3].track,
    },
    {
      id: 'card-5',
      title: 'Deep House Relax',
      description: 'Smooth and relaxing deep house grooves.',
      imageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=400&q=80',
      type: 'Playlist',
      track: quickPicks[4].track,
    },
  ];

  const popularArtists = [
    {
      id: 'artist-1',
      name: 'Luna Waves',
      role: 'Artist',
      monthlyListeners: '1.4M monthly listeners',
      imageUrl: 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&q=80',
    },
    {
      id: 'artist-2',
      name: 'Solar Pulse',
      role: 'Artist',
      monthlyListeners: '980K monthly listeners',
      imageUrl: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80',
    },
    {
      id: 'artist-3',
      name: 'Astral Drift',
      role: 'Artist',
      monthlyListeners: '2.3M monthly listeners',
      imageUrl: 'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&q=80',
    },
    {
      id: 'artist-4',
      name: 'Vapor Wave',
      role: 'Artist',
      monthlyListeners: '540K monthly listeners',
      imageUrl: 'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6?w=400&q=80',
    },
  ];

  const handlePlayCard = (e: React.MouseEvent, track: TrackDto) => {
    e.preventDefault();
    e.stopPropagation();
    playTrack(track);
  };

  return (
    <div className="space-y-8 animate-in fade-in duration-300">
      {/* Top Header Greeting */}
      <div>
        <h1 className="text-3xl font-extrabold tracking-tight text-white mb-4">
          {getGreeting()}
        </h1>

        {/* 6-Pack Quick Play Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {quickPicks.map((pick) => (
            <Link
              key={pick.id}
              to={`/playlist/${pick.id}`}
              className="flex items-center gap-4 bg-white/5 hover:bg-white/15 rounded-md overflow-hidden transition-colors group relative cursor-pointer pr-4"
            >
              {pick.isLiked ? (
                <div className="flex items-center justify-center w-20 h-20 bg-gradient-to-br from-indigo-600 to-emerald-400 text-white flex-shrink-0 shadow-md">
                  <Heart className="w-8 h-8 fill-white" />
                </div>
              ) : (
                <img
                  src={pick.imageUrl}
                  alt={pick.title}
                  className="w-20 h-20 object-cover flex-shrink-0 shadow-md"
                />
              )}
              <span className="font-bold text-sm text-white line-clamp-2 flex-1">
                {pick.title}
              </span>

              {/* Floating Green Play Button */}
              <button
                onClick={(e) => handlePlayCard(e, pick.track)}
                className="flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green text-black shadow-spotify-card opacity-0 group-hover:opacity-100 group-hover:scale-105 transition-all duration-200 hover:bg-spotify-green-hover flex-shrink-0"
              >
                <Play className="w-5 h-5 fill-black text-black ml-0.5" />
              </button>
            </Link>
          ))}
        </div>
      </div>

      {/* Made For You Carousel Grid */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold tracking-tight text-white hover:underline cursor-pointer">
            Made For You
          </h2>
          <span className="text-xs font-bold text-spotify-muted hover:underline cursor-pointer uppercase tracking-wider">
            Show all
          </span>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          {featuredCards.map((card) => (
            <div
              key={card.id}
              className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col"
            >
              <div className="relative mb-4 w-full aspect-square rounded-md overflow-hidden shadow-lg bg-black/40">
                <img
                  src={card.imageUrl}
                  alt={card.title}
                  className="w-full h-full object-cover"
                />
                {/* Floating Spotify Play Button */}
                <button
                  onClick={(e) => handlePlayCard(e, card.track)}
                  className="absolute bottom-2 right-2 flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green text-black shadow-spotify-card opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0 group-hover:scale-105 transition-all duration-200 hover:bg-spotify-green-hover"
                >
                  <Play className="w-5 h-5 fill-black text-black ml-0.5" />
                </button>
              </div>

              <h3 className="font-bold text-sm text-white truncate mb-1">
                {card.title}
              </h3>
              <p className="text-xs text-spotify-muted line-clamp-2 leading-relaxed">
                {card.description}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* Popular Artists Section */}
      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold tracking-tight text-white hover:underline cursor-pointer">
            Popular Artists
          </h2>
          <span className="text-xs font-bold text-spotify-muted hover:underline cursor-pointer uppercase tracking-wider">
            Show all
          </span>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-4 gap-4">
          {popularArtists.map((artist) => (
            <Link
              key={artist.id}
              to={`/artist/${artist.id}`}
              className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col items-start"
            >
              <div className="relative mb-4 w-full aspect-square rounded-full overflow-hidden shadow-lg bg-black/40">
                <img
                  src={artist.imageUrl}
                  alt={artist.name}
                  className="w-full h-full object-cover"
                />
                {/* Floating Spotify Play Button */}
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    playTrack(quickPicks[0].track);
                  }}
                  className="absolute bottom-2 right-2 flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green text-black shadow-spotify-card opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0 group-hover:scale-105 transition-all duration-200 hover:bg-spotify-green-hover"
                >
                  <Play className="w-5 h-5 fill-black text-black ml-0.5" />
                </button>
              </div>

              <h3 className="font-bold text-base text-white truncate mb-1 w-full">
                {artist.name}
              </h3>
              <p className="text-xs text-spotify-muted truncate w-full">
                {artist.monthlyListeners}
              </p>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
};
