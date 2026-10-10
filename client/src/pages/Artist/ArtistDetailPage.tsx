import {
  AlertCircle,
  BadgeCheck,
  Camera,
  CheckCircle,
  Heart,
  Loader2,
  MoreHorizontal,
  Pause,
  Play,
} from 'lucide-react';
import React, { useRef, useState } from 'react';
import { usePlayer } from '../../contexts/PlayerContext';
import { catalogService } from '../../services/catalogService';
import { TrackDto } from '../../types/catalog.types';
import { cn } from '../../utils/cn';

const ARTIST_TOP_TRACKS: TrackDto[] = [
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
  {
    id: 'track-7',
    title: 'Cosmic Journey',
    artistId: 'artist-1',
    artistName: 'Luna Waves',
    albumId: 'album-5',
    albumTitle: 'Deep Space',
    coverImageUrl: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
    durationSeconds: 260,
    playCount: 890000,
    isExplicit: false,
  },
  {
    id: 'track-8',
    title: 'Aurora Borealis',
    artistId: 'artist-1',
    artistName: 'Luna Waves',
    albumId: 'album-5',
    albumTitle: 'Deep Space',
    coverImageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80',
    durationSeconds: 215,
    playCount: 750000,
    isExplicit: false,
  },
  {
    id: 'track-9',
    title: 'Solitude in Sound',
    artistId: 'artist-1',
    artistName: 'Luna Waves',
    albumId: 'album-6',
    albumTitle: 'Echoes of Calm',
    coverImageUrl: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=400&q=80',
    durationSeconds: 195,
    playCount: 620000,
    isExplicit: false,
  },
];

function formatNumber(num: number): string {
  return new Intl.NumberFormat('en-US').format(num);
}

function formatDuration(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
}

export const ArtistDetailPage: React.FC = () => {
  const { playTrack, currentTrack, isPlaying, togglePlay, isLiked, toggleLikeTrack } = usePlayer();
  const [isFollowing, setIsFollowing] = useState<boolean>(false);

  const [albums, setAlbums] = useState([
    {
      id: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
      title: 'Neon Horizon',
      year: '2026',
      type: 'Album',
      image: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
    },
    {
      id: '3fa85f64-5717-4562-b3fc-2c963f66afa7',
      title: 'Deep Space',
      year: '2025',
      type: 'Album',
      image: 'https://images.unsplash.com/photo-1508700115892-45ecd05ae2ad?w=400&q=80',
    },
    {
      id: '3fa85f64-5717-4562-b3fc-2c963f66afa8',
      title: 'Echoes of Calm',
      year: '2024',
      type: 'EP',
      image: 'https://images.unsplash.com/photo-1470225620780-dba8ba36b745?w=400&q=80',
    },
  ]);

  const [uploadingAlbumId, setUploadingAlbumId] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const fileInputRefs = useRef<{ [key: string]: HTMLInputElement | null }>({});

  const isCurrentArtistPlaying =
    isPlaying && currentTrack && ARTIST_TOP_TRACKS.some((t) => t.id === currentTrack.id);

  const handlePlayArtist = () => {
    if (isCurrentArtistPlaying) {
      togglePlay();
    } else {
      playTrack(ARTIST_TOP_TRACKS[0], ARTIST_TOP_TRACKS);
    }
  };

  const handleAlbumCoverChange = async (
    albumId: string,
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setErrorMsg(null);
    setSuccessMsg(null);

    // Validate size (< 5 MB)
    if (file.size > 5 * 1024 * 1024) {
      setErrorMsg('Album cover file size must be less than 5 MB.');
      if (fileInputRefs.current[albumId]) fileInputRefs.current[albumId]!.value = '';
      return;
    }

    // Validate mime type
    const validMimes = ['image/jpeg', 'image/png', 'image/webp'];
    if (!validMimes.includes(file.type)) {
      setErrorMsg('Only JPEG, PNG, or WebP images are allowed for album artwork.');
      if (fileInputRefs.current[albumId]) fileInputRefs.current[albumId]!.value = '';
      return;
    }

    const previousImage = albums.find((a) => a.id === albumId)?.image || '';
    const previewUrl = URL.createObjectURL(file);

    // Optimistic preview
    setAlbums((prev) =>
      prev.map((a) => (a.id === albumId ? { ...a, image: previewUrl } : a))
    );

    setUploadingAlbumId(albumId);
    try {
      const serverUrl = await catalogService.uploadAlbumCover(albumId, file);
      setAlbums((prev) =>
        prev.map((a) => (a.id === albumId ? { ...a, image: serverUrl } : a))
      );
      setSuccessMsg('Album artwork updated successfully!');
    } catch (err: any) {
      // Revert optimistic preview
      setAlbums((prev) =>
        prev.map((a) => (a.id === albumId ? { ...a, image: previousImage } : a))
      );
      setErrorMsg(
        err?.response?.data?.message || err?.message || 'Failed to upload album cover.'
      );
    } finally {
      setUploadingAlbumId(null);
      if (fileInputRefs.current[albumId]) fileInputRefs.current[albumId]!.value = '';
    }
  };

  return (
    <div className="-mt-16 -mx-6 animate-in fade-in duration-300">
      {/* Full-Bleed Hero Banner */}
      <div
        className="relative h-80 flex flex-col justify-end p-8 bg-cover bg-center"
        style={{
          backgroundImage: `linear-gradient(to bottom, rgba(0,0,0,0.1) 0%, rgba(18,18,18,0.9) 100%), url('https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?w=1200&q=80')`,
        }}
      >
        <div className="flex items-center gap-1.5 text-xs font-bold text-sky-400 mb-2">
          <BadgeCheck className="w-5 h-5 fill-sky-400 text-black" />
          <span className="text-white">Verified Artist</span>
        </div>
        <h1 className="text-5xl md:text-7xl font-black text-white tracking-tight mb-3">
          Luna Waves
        </h1>
        <p className="text-sm font-semibold text-white/90">
          1,420,500 monthly listeners
        </p>
      </div>

      {/* Action Bar & Popular Tracks */}
      <div className="p-6 space-y-8">
        {/* Notifications */}
        {errorMsg && (
          <div className="flex items-center gap-3 px-4 py-3 rounded-lg bg-red-950/80 border border-red-500/40 text-red-200 text-sm shadow-lg">
            <AlertCircle className="w-5 h-5 flex-shrink-0 text-red-400" />
            <span>{errorMsg}</span>
          </div>
        )}

        {successMsg && (
          <div className="flex items-center gap-3 px-4 py-3 rounded-lg bg-emerald-950/80 border border-emerald-500/40 text-emerald-200 text-sm shadow-lg">
            <CheckCircle className="w-5 h-5 flex-shrink-0 text-emerald-400" />
            <span>{successMsg}</span>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center gap-6">
          <button
            onClick={handlePlayArtist}
            className="flex items-center justify-center w-14 h-14 rounded-full bg-spotify-green hover:bg-spotify-green-hover text-black shadow-spotify-card hover:scale-105 transition-all duration-200 cursor-pointer"
          >
            {isCurrentArtistPlaying ? (
              <Pause className="w-6 h-6 fill-black text-black" />
            ) : (
              <Play className="w-6 h-6 fill-black text-black ml-1" />
            )}
          </button>

          <button
            onClick={() => setIsFollowing((prev) => !prev)}
            className={cn(
              'px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-wider transition-all border',
              isFollowing
                ? 'border-spotify-green text-spotify-green'
                : 'border-white/30 text-white hover:border-white hover:scale-105'
            )}
          >
            {isFollowing ? 'Following' : 'Follow'}
          </button>

          <button
            title="More options"
            className="p-2 text-spotify-muted hover:text-white transition-colors cursor-pointer"
          >
            <MoreHorizontal className="w-7 h-7" />
          </button>
        </div>

        {/* Popular Tracks */}
        <section className="space-y-4">
          <h2 className="text-2xl font-bold text-white">Popular</h2>

          <div className="space-y-1">
            {ARTIST_TOP_TRACKS.map((track, idx) => {
              const isThisPlaying = currentTrack?.id === track.id && isPlaying;
              const isThisCurrent = currentTrack?.id === track.id;
              const liked = isLiked(track.id);

              return (
                <div
                  key={track.id}
                  onDoubleClick={() => playTrack(track, ARTIST_TOP_TRACKS)}
                  className="grid grid-cols-[16px_1fr_1fr_40px] md:grid-cols-[16px_4fr_2fr_40px] gap-4 items-center px-4 py-2.5 rounded-md hover:bg-white/10 transition-colors group cursor-pointer"
                >
                  <div className="flex items-center justify-center text-sm font-medium text-spotify-muted">
                    <span className={cn('group-hover:hidden', isThisCurrent ? 'text-spotify-green' : '')}>
                      {idx + 1}
                    </span>
                    <button
                      onClick={() =>
                        isThisPlaying ? togglePlay() : playTrack(track, ARTIST_TOP_TRACKS)
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

                  <div className="flex items-center gap-3 min-w-0">
                    <img
                      src={track.coverImageUrl}
                      alt={track.title}
                      className="w-10 h-10 rounded object-cover flex-shrink-0"
                    />
                    <span
                      className={cn(
                        'text-sm font-semibold truncate',
                        isThisCurrent ? 'text-spotify-green' : 'text-white'
                      )}
                    >
                      {track.title}
                    </span>
                  </div>

                  <span className="text-xs text-spotify-muted tabular-nums">
                    {formatNumber(track.playCount)}
                  </span>

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
        </section>

        {/* Discography */}
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-bold text-white">Discography</h2>
            <span className="text-xs text-spotify-muted">Hover album artwork to upload custom cover</span>
          </div>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
            {albums.map((album) => {
              const isUploading = uploadingAlbumId === album.id;
              return (
                <div
                  key={album.id}
                  className="bg-spotify-card hover:bg-spotify-card-hover p-4 rounded-lg transition-colors group relative cursor-pointer flex flex-col"
                >
                  <div className="relative mb-4 w-full aspect-square rounded-md overflow-hidden shadow-lg bg-black/40">
                    <img src={album.image} alt={album.title} className="w-full h-full object-cover" />

                    {/* Hidden File Input */}
                    <input
                      ref={(el) => (fileInputRefs.current[album.id] = el)}
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      className="hidden"
                      onChange={(e) => handleAlbumCoverChange(album.id, e)}
                    />

                    {/* Artwork Change Button Overlay */}
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        fileInputRefs.current[album.id]?.click();
                      }}
                      disabled={isUploading}
                      title="Change album artwork"
                      className={cn(
                        'absolute top-2 right-2 flex items-center justify-center w-8 h-8 rounded-full bg-black/75 hover:bg-black/90 backdrop-blur-md text-white border border-white/20 transition-all z-10 cursor-pointer shadow-lg',
                        isUploading ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'
                      )}
                    >
                      {isUploading ? (
                        <Loader2 className="w-4 h-4 animate-spin text-spotify-green" />
                      ) : (
                        <Camera className="w-4 h-4" />
                      )}
                    </button>

                    {/* Play Button */}
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        playTrack(ARTIST_TOP_TRACKS[0]);
                      }}
                      className="absolute bottom-2 right-2 flex items-center justify-center w-12 h-12 rounded-full bg-spotify-green text-black shadow-spotify-card opacity-0 translate-y-2 group-hover:opacity-100 group-hover:translate-y-0 group-hover:scale-105 transition-all duration-200 hover:bg-spotify-green-hover"
                    >
                      <Play className="w-5 h-5 fill-black text-black ml-0.5" />
                    </button>
                  </div>
                  <h3 className="font-bold text-sm text-white truncate mb-1">{album.title}</h3>
                  <p className="text-xs text-spotify-muted">
                    {album.year} • {album.type}
                  </p>
                </div>
              );
            })}
          </div>
        </section>
      </div>
    </div>
  );
};
