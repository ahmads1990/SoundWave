import { Clock, Heart, MoreHorizontal, Pause, Play } from 'lucide-react';
import React from 'react';
import { useParams } from 'react-router-dom';
import { usePlayer } from '../../contexts/PlayerContext';
import { TrackDto } from '../../types/catalog.types';
import { cn } from '../../utils/cn';

const PLAYLIST_TRACKS: TrackDto[] = [
  {
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
  {
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
  {
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
  {
    id: 'track-4',
    title: 'Neon Skyline',
    artistId: 'artist-4',
    artistName: 'Vapor Wave',
    albumId: 'album-4',
    albumTitle: 'Retro Future',
    coverImageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
    durationSeconds: 182,
    playCount: 540000,
    isExplicit: false,
  },
  {
    id: 'track-5',
    title: 'Starlight Odyssey',
    artistId: 'artist-1',
    artistName: 'Luna Waves',
    albumId: 'album-1',
    albumTitle: 'Neon Horizon',
    coverImageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=400&q=80',
    durationSeconds: 230,
    playCount: 3100000,
    isExplicit: false,
  },
];

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
}

export const PlaylistDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const { playTrack, currentTrack, isPlaying, togglePlay, isLiked, toggleLikeTrack } = usePlayer();

  const isLikedPlaylist = id === 'liked-songs';

  const isCurrentPlaylistPlaying =
    isPlaying && currentTrack && PLAYLIST_TRACKS.some((t) => t.id === currentTrack.id);

  const handleMainPlay = () => {
    if (isCurrentPlaylistPlaying) {
      togglePlay();
    } else {
      playTrack(PLAYLIST_TRACKS[0], PLAYLIST_TRACKS);
    }
  };

  return (
    <div className="-mt-16 -mx-6 animate-in fade-in duration-300">
      {/* Dynamic Hero Header */}
      <div
        className={cn(
          'p-8 pt-20 flex flex-col md:flex-row items-end gap-6 bg-gradient-to-b',
          isLikedPlaylist
            ? 'from-indigo-800 via-indigo-950/60 to-spotify-base'
            : 'from-emerald-800 via-emerald-950/60 to-spotify-base'
        )}
      >
        {/* Cover Art */}
        <div className="w-52 h-52 rounded shadow-2xl overflow-hidden flex-shrink-0 flex items-center justify-center bg-black/40">
          {isLikedPlaylist ? (
            <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-indigo-600 to-emerald-400 text-white shadow-2xl">
              <Heart className="w-20 h-20 fill-white" />
            </div>
          ) : (
            <img
              src="https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=600&q=80"
              alt="Playlist Cover"
              className="w-full h-full object-cover shadow-2xl"
            />
          )}
        </div>

        {/* Playlist Info */}
        <div className="flex flex-col gap-2 min-w-0">
          <span className="text-xs font-bold uppercase tracking-wider text-white">
            Playlist
          </span>
          <h1 className="text-4xl md:text-6xl font-black text-white tracking-tight">
            {isLikedPlaylist ? 'Liked Songs' : 'Synthwave & Cyberpunk Vibes'}
          </h1>
          <p className="text-sm text-spotify-muted mt-1">
            The ultimate retro synthesizer collection for deep coding and late-night drives.
          </p>
          <div className="flex items-center gap-2 text-xs font-semibold text-white/90 mt-2">
            <span className="font-bold">SoundWave</span>
            <span>•</span>
            <span>5 songs, about 17 min</span>
          </div>
        </div>
      </div>

      {/* Content & Action Bar */}
      <div className="p-6 space-y-6">
        {/* Actions Bar */}
        <div className="flex items-center gap-6">
          <button
            onClick={handleMainPlay}
            className="flex items-center justify-center w-14 h-14 rounded-full bg-spotify-green hover:bg-spotify-green-hover text-black shadow-spotify-card hover:scale-105 transition-all duration-200 cursor-pointer"
          >
            {isCurrentPlaylistPlaying ? (
              <Pause className="w-6 h-6 fill-black text-black" />
            ) : (
              <Play className="w-6 h-6 fill-black text-black ml-1" />
            )}
          </button>

          <button
            onClick={() => toggleLikeTrack('playlist-current')}
            title="Save to Your Library"
            className="p-2 text-spotify-muted hover:text-white transition-colors cursor-pointer"
          >
            <Heart className="w-8 h-8 hover:scale-105 transition-transform" />
          </button>

          <button
            title="More options"
            className="p-2 text-spotify-muted hover:text-white transition-colors cursor-pointer"
          >
            <MoreHorizontal className="w-7 h-7" />
          </button>
        </div>

        {/* Spotify Tracklist Table */}
        <div className="space-y-1">
          {/* Table Header */}
          <div className="grid grid-cols-[16px_1fr_1fr_40px] md:grid-cols-[16px_4fr_3fr_2fr_40px] gap-4 px-4 py-2 text-xs font-bold uppercase tracking-wider text-spotify-muted border-b border-white/10">
            <span>#</span>
            <span>Title</span>
            <span className="hidden md:block">Album</span>
            <span className="hidden md:block">Date added</span>
            <div className="flex justify-end">
              <Clock className="w-4 h-4" />
            </div>
          </div>

          {/* Table Rows */}
          {PLAYLIST_TRACKS.map((track, idx) => {
            const isThisPlaying = currentTrack?.id === track.id && isPlaying;
            const isThisCurrent = currentTrack?.id === track.id;
            const liked = isLiked(track.id);

            return (
              <div
                key={track.id}
                onDoubleClick={() => playTrack(track, PLAYLIST_TRACKS)}
                className="grid grid-cols-[16px_1fr_1fr_40px] md:grid-cols-[16px_4fr_3fr_2fr_40px] gap-4 items-center px-4 py-2.5 rounded-md hover:bg-white/10 transition-colors group cursor-pointer"
              >
                {/* Index / Play Hover Icon */}
                <div className="flex items-center justify-center text-sm font-medium text-spotify-muted">
                  <span className={cn('group-hover:hidden', isThisCurrent ? 'text-spotify-green' : '')}>
                    {idx + 1}
                  </span>
                  <button
                    onClick={() =>
                      isThisPlaying ? togglePlay() : playTrack(track, PLAYLIST_TRACKS)
                    }
                    className="hidden group-hover:flex items-center justify-center text-white"
                  >
                    {isThisPlaying ? (
                      <Pause className="w-4 h-4 fill-white" />
                    ) : (
                      <Play className="w-4 h-4 fill-white ml-0.5" />
                    )}
                  </button>
                </div>

                {/* Title & Artist */}
                <div className="flex items-center gap-3 min-w-0">
                  <img
                    src={track.coverImageUrl}
                    alt={track.title}
                    className="w-10 h-10 rounded object-cover flex-shrink-0"
                  />
                  <div className="flex flex-col min-w-0">
                    <span
                      className={cn(
                        'text-sm font-semibold truncate',
                        isThisCurrent ? 'text-spotify-green' : 'text-white'
                      )}
                    >
                      {track.title}
                    </span>
                    <span className="text-xs text-spotify-muted hover:underline truncate">
                      {track.artistName}
                    </span>
                  </div>
                </div>

                {/* Album Name */}
                <span className="hidden md:block text-xs text-spotify-muted hover:underline truncate">
                  {track.albumTitle}
                </span>

                {/* Date Added */}
                <span className="hidden md:block text-xs text-spotify-muted">
                  2 days ago
                </span>

                {/* Duration & Like */}
                <div className="flex items-center justify-end gap-3 text-xs text-spotify-muted">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      toggleLikeTrack(track.id);
                    }}
                    className={cn(
                      'transition-colors p-1',
                      liked ? 'text-spotify-green' : 'opacity-0 group-hover:opacity-100 hover:text-white'
                    )}
                  >
                    <Heart className={cn('w-4 h-4', liked ? 'fill-spotify-green' : '')} />
                  </button>
                  <span className="tabular-nums">
                    {formatDuration(track.durationSeconds)}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
