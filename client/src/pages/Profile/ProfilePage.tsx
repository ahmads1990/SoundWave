import {
  AlertCircle,
  Camera,
  CheckCircle,
  Clock,
  Loader2,
  Mail,
  Shield,
  Sparkles,
  User as UserIcon,
} from 'lucide-react';
import React, { useEffect, useRef, useState } from 'react';
import { Skeleton } from '../../components/common/Skeleton';
import { useAuth } from '../../contexts/AuthContext';
import { authService, UserProfile } from '../../services/authService';

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB
const ALLOWED_MIME_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

export const ProfilePage: React.FC = () => {
  const { user, updateProfileImages } = useAuth();

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState<boolean>(true);

  const [isUploadingAvatar, setIsUploadingAvatar] = useState<boolean>(false);
  const [isUploadingBanner, setIsUploadingBanner] = useState<boolean>(false);

  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const avatarInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);

  // Load full profile details on mount
  useEffect(() => {
    let isMounted = true;
    const fetchProfile = async () => {
      try {
        setIsLoadingProfile(true);
        const data = await authService.getProfile();
        if (isMounted) {
          setProfile(data);
          updateProfileImages(data.profilePicUrl || undefined, data.coverImageUrl || undefined);
        }
      } catch (err) {
        // Fallback to auth context user if profile API is unreachable
        if (isMounted && user) {
          setProfile({
            id: user.id,
            userId: user.id,
            firstName: user.userName,
            lastName: '',
            displayName: user.userName,
            email: user.email,
            profilePicUrl: user.profilePicUrl,
            coverImageUrl: user.coverImageUrl,
          });
        }
      } finally {
        if (isMounted) setIsLoadingProfile(false);
      }
    };

    fetchProfile();
    return () => {
      isMounted = false;
    };
  }, [user?.id]);

  const handleAvatarSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setErrorMsg(null);
    setSuccessMsg(null);

    // Validate size
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setErrorMsg('Avatar file size must be less than 5 MB.');
      if (avatarInputRef.current) avatarInputRef.current.value = '';
      return;
    }

    // Validate mime type
    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
      setErrorMsg('Only JPEG, PNG, or WebP images are supported.');
      if (avatarInputRef.current) avatarInputRef.current.value = '';
      return;
    }

    // Optimistic local preview
    const previewUrl = URL.createObjectURL(file);
    const previousPic = profile?.profilePicUrl;
    setProfile((prev) => (prev ? { ...prev, profilePicUrl: previewUrl } : null));
    updateProfileImages(previewUrl, profile?.coverImageUrl || undefined);

    setIsUploadingAvatar(true);
    try {
      const serverUrl = await authService.uploadAvatar(file);
      setProfile((prev) => (prev ? { ...prev, profilePicUrl: serverUrl } : null));
      updateProfileImages(serverUrl, profile?.coverImageUrl || undefined);
      setSuccessMsg('Avatar updated successfully!');
    } catch (err: any) {
      // Revert optimistic preview
      setProfile((prev) => (prev ? { ...prev, profilePicUrl: previousPic } : null));
      updateProfileImages(previousPic || undefined, profile?.coverImageUrl || undefined);
      setErrorMsg(err?.response?.data?.message || err?.message || 'Failed to upload avatar.');
    } finally {
      setIsUploadingAvatar(false);
      if (avatarInputRef.current) avatarInputRef.current.value = '';
    }
  };

  const handleBannerSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setErrorMsg(null);
    setSuccessMsg(null);

    // Validate size
    if (file.size > MAX_FILE_SIZE_BYTES) {
      setErrorMsg('Banner file size must be less than 5 MB.');
      if (bannerInputRef.current) bannerInputRef.current.value = '';
      return;
    }

    // Validate mime type
    if (!ALLOWED_MIME_TYPES.includes(file.type)) {
      setErrorMsg('Only JPEG, PNG, or WebP images are supported.');
      if (bannerInputRef.current) bannerInputRef.current.value = '';
      return;
    }

    // Optimistic local preview
    const previewUrl = URL.createObjectURL(file);
    const previousBanner = profile?.coverImageUrl;
    setProfile((prev) => (prev ? { ...prev, coverImageUrl: previewUrl } : null));
    updateProfileImages(profile?.profilePicUrl || undefined, previewUrl);

    setIsUploadingBanner(true);
    try {
      const serverUrl = await authService.uploadBanner(file);
      setProfile((prev) => (prev ? { ...prev, coverImageUrl: serverUrl } : null));
      updateProfileImages(profile?.profilePicUrl || undefined, serverUrl);
      setSuccessMsg('Profile banner updated successfully!');
    } catch (err: any) {
      // Revert optimistic preview
      setProfile((prev) => (prev ? { ...prev, coverImageUrl: previousBanner } : null));
      updateProfileImages(profile?.profilePicUrl || undefined, previousBanner || undefined);
      setErrorMsg(err?.response?.data?.message || err?.message || 'Failed to upload banner.');
    } finally {
      setIsUploadingBanner(false);
      if (bannerInputRef.current) bannerInputRef.current.value = '';
    }
  };

  if (isLoadingProfile) {
    return (
      <div className="space-y-6 pb-12 animate-pulse">
        <Skeleton className="w-full h-64 md:h-80 rounded-2xl" />
        <div className="px-4 md:px-8 -mt-20 md:-mt-24 flex flex-col md:flex-row items-center md:items-end gap-6">
          <Skeleton className="w-36 h-36 md:w-44 md:h-44 rounded-full border-4 border-spotify-base" />
          <div className="space-y-3 flex-1">
            <Skeleton className="w-24 h-4 rounded-full" />
            <Skeleton className="w-64 h-10 rounded-md" />
            <Skeleton className="w-48 h-4 rounded-md" />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-6">
          <Skeleton className="h-48 rounded-2xl" />
          <Skeleton className="h-48 rounded-2xl" />
          <Skeleton className="h-48 rounded-2xl" />
        </div>
      </div>
    );
  }

  const displayName =
    profile?.displayName ||
    (profile?.firstName ? `${profile.firstName} ${profile.lastName || ''}`.trim() : user?.userName) ||
    'User';

  const avatarUrl = profile?.profilePicUrl || user?.profilePicUrl;
  const bannerUrl = profile?.coverImageUrl || user?.coverImageUrl;

  return (
    <div className="space-y-6 pb-12 animate-in fade-in duration-300">
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

      {/* Hero Banner Section */}
      <div className="relative w-full h-64 md:h-80 rounded-2xl overflow-hidden group shadow-2xl bg-[#181818] border border-white/5">
        {bannerUrl ? (
          <img
            src={bannerUrl}
            alt="Profile cover banner"
            className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
          />
        ) : (
          <div className="w-full h-full bg-gradient-to-br from-indigo-950 via-purple-950 to-spotify-base" />
        )}

        {/* Ambient Gradient Overlay */}
        <div className="absolute inset-0 bg-gradient-to-t from-spotify-base via-spotify-base/30 to-transparent pointer-events-none" />

        {/* Banner Upload Button Pill */}
        <div className="absolute top-4 right-4 z-20">
          <input
            ref={bannerInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={handleBannerSelect}
          />
          <button
            onClick={() => bannerInputRef.current?.click()}
            disabled={isUploadingBanner}
            className="flex items-center gap-2 px-4 py-2 rounded-full bg-black/70 hover:bg-black/90 backdrop-blur-md text-white text-xs font-bold border border-white/20 transition-all shadow-lg hover:border-white/40 cursor-pointer disabled:opacity-50"
          >
            {isUploadingBanner ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin text-spotify-green" />
                <span>Uploading banner...</span>
              </>
            ) : (
              <>
                <Camera className="w-4 h-4" />
                <span>Change Banner</span>
              </>
            )}
          </button>
        </div>
      </div>

      {/* Avatar & User Header */}
      <div className="relative z-10 px-4 md:px-8 -mt-20 md:-mt-24 flex flex-col md:flex-row items-center md:items-end gap-6">
        {/* Avatar Container */}
        <div className="relative group w-36 h-36 md:w-44 md:h-44 rounded-full border-4 border-spotify-base shadow-2xl bg-[#282828] overflow-hidden flex-shrink-0">
          <input
            ref={avatarInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={handleAvatarSelect}
          />

          {avatarUrl ? (
            <img src={avatarUrl} alt={displayName} className="w-full h-full object-cover" />
          ) : (
            <div className="w-full h-full flex items-center justify-center bg-gradient-to-tr from-purple-800 to-indigo-600 text-white font-extrabold text-4xl">
              {displayName.charAt(0).toUpperCase()}
            </div>
          )}

          {/* Avatar Hover Overlay */}
          <button
            onClick={() => avatarInputRef.current?.click()}
            disabled={isUploadingAvatar}
            className="absolute inset-0 bg-black/60 group-hover:opacity-100 opacity-0 transition-opacity flex flex-col items-center justify-center text-white text-xs font-bold gap-1 cursor-pointer disabled:opacity-100"
          >
            {isUploadingAvatar ? (
              <>
                <Loader2 className="w-6 h-6 animate-spin text-spotify-green" />
                <span>Uploading...</span>
              </>
            ) : (
              <>
                <Camera className="w-6 h-6" />
                <span>Change photo</span>
              </>
            )}
          </button>
        </div>

        {/* User Identity Details */}
        <div className="flex-1 text-center md:text-left space-y-2">
          <div className="flex flex-wrap items-center justify-center md:justify-start gap-2">
            <span className="text-xs uppercase font-extrabold tracking-widest text-spotify-muted">
              Profile
            </span>
            {user?.role && (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-spotify-green/20 text-spotify-green border border-spotify-green/30">
                <Shield className="w-3 h-3" />
                {user.role}
              </span>
            )}
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-white/10 text-white/80">
              <Sparkles className="w-3 h-3 text-amber-400" />
              SoundWave Member
            </span>
          </div>

          <h1 className="text-3xl md:text-5xl font-black text-white tracking-tight">
            {displayName}
          </h1>

          <p className="text-sm text-spotify-muted flex items-center justify-center md:justify-start gap-2">
            <Mail className="w-4 h-4 text-spotify-muted" />
            <span>{profile?.email || user?.email}</span>
            <span className="text-white/20">•</span>
            <span className="text-emerald-400 font-semibold text-xs flex items-center gap-1">
              <CheckCircle className="w-3.5 h-3.5" />
              Verified Account
            </span>
          </p>
        </div>
      </div>

      {/* Profile Overview & Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 pt-6">
        {/* Card 1: Account Information */}
        <div className="bg-spotify-card/80 backdrop-blur-md rounded-2xl p-6 border border-white/5 space-y-4 shadow-spotify-card">
          <div className="flex items-center gap-2 text-white font-bold text-base">
            <UserIcon className="w-5 h-5 text-spotify-green" />
            <h3>Personal Information</h3>
          </div>
          <div className="space-y-3 text-xs">
            <div>
              <span className="text-spotify-muted block">Username</span>
              <span className="text-white font-semibold text-sm">{user?.userName || 'User'}</span>
            </div>
            {profile?.firstName && (
              <div>
                <span className="text-spotify-muted block">Full Name</span>
                <span className="text-white font-semibold text-sm">
                  {profile.firstName} {profile.lastName || ''}
                </span>
              </div>
            )}
            {profile?.phoneNumber && (
              <div>
                <span className="text-spotify-muted block">Phone</span>
                <span className="text-white font-semibold text-sm">{profile.phoneNumber}</span>
              </div>
            )}
            <div>
              <span className="text-spotify-muted block">Interface Language</span>
              <span className="text-white font-semibold text-sm">
                {profile?.language || 'en-US'}
              </span>
            </div>
          </div>
        </div>

        {/* Card 2: Subscription & Security */}
        <div className="bg-spotify-card/80 backdrop-blur-md rounded-2xl p-6 border border-white/5 space-y-4 shadow-spotify-card">
          <div className="flex items-center gap-2 text-white font-bold text-base">
            <Shield className="w-5 h-5 text-spotify-green" />
            <h3>Subscription & Tier</h3>
          </div>
          <div className="space-y-3 text-xs">
            <div>
              <span className="text-spotify-muted block">Streaming Quality</span>
              <span className="text-white font-semibold text-sm">HiFi Lossless Audio</span>
            </div>
            <div>
              <span className="text-spotify-muted block">Account Type</span>
              <span className="text-white font-semibold text-sm">
                {user?.role === 'Artist'
                  ? 'Artist Studio Enabled'
                  : user?.role === 'Admin'
                  ? 'System Administrator'
                  : 'Standard Listener'}
              </span>
            </div>
            <div>
              <span className="text-spotify-muted block">Security Status</span>
              <span className="text-emerald-400 font-semibold text-sm flex items-center gap-1.5 mt-0.5">
                <CheckCircle className="w-4 h-4" />
                Protected via JWT & Identity Core
              </span>
            </div>
          </div>
        </div>

        {/* Card 3: Storage & Assets */}
        <div className="bg-spotify-card/80 backdrop-blur-md rounded-2xl p-6 border border-white/5 space-y-4 shadow-spotify-card">
          <div className="flex items-center gap-2 text-white font-bold text-base">
            <Clock className="w-5 h-5 text-spotify-green" />
            <h3>Asset Storage Info</h3>
          </div>
          <div className="space-y-3 text-xs">
            <div>
              <span className="text-spotify-muted block">Storage Backend</span>
              <span className="text-white font-semibold text-sm">RustFS / S3 Compatible</span>
            </div>
            <div>
              <span className="text-spotify-muted block">Image Limits</span>
              <span className="text-white font-semibold text-sm">5 MB max per image (.jpg, .png, .webp)</span>
            </div>
            <div>
              <span className="text-spotify-muted block">Content Validation</span>
              <span className="text-emerald-400 font-semibold text-sm">Magic-Byte Verified</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
