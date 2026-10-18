import React, { useState, useRef, useEffect } from 'react';
import {
  UploadCloud,
  X,
  Music,
  CheckCircle2,
  AlertCircle,
  Loader2,
  Trash2,
  Plus,
  FileAudio,
} from 'lucide-react';
import { catalogService } from '../../services/catalogService';
import { BatchTrackItemRequest } from '../../types/catalog.types';
import { cn } from '../../utils/cn';

interface BatchTrackUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  albumId: string;
  albumTitle: string;
  onSuccess?: () => void;
}

interface TrackUploadQueueItem {
  id: string;
  file: File;
  title: string;
  sizeBytes: number;
  extension: string;
  /** Content type sent both when signing the URL and when uploading — they must match */
  contentType: string;
  /** Set once the track is registered server-side; failed uploads are retried against it */
  trackId?: string;
  status: 'idle' | 'creating' | 'uploading' | 'completed' | 'error';
  progress: number;
  errorMessage?: string;
}

// Mirror backend limits (FileMetadataValidator.DefaultMaxAudioSizeBytes, BatchCreateTracksRequestValidator.MaxBatchSize)
const MAX_AUDIO_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB
const MAX_BATCH_SIZE = 50;
const AUDIO_CONTENT_TYPES: Record<string, string> = {
  '.mp3': 'audio/mpeg',
  '.flac': 'audio/flac',
  '.wav': 'audio/wav',
  '.aac': 'audio/aac',
  '.m4a': 'audio/mp4',
};
const ALLOWED_EXTENSIONS = Object.keys(AUDIO_CONTENT_TYPES);

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(1)} ${sizes[i]}`;
}

function cleanTitleFromFilename(fileName: string): string {
  return fileName.replace(/\.[^/.]+$/, '').replace(/[_-]/g, ' ').trim();
}

/** Browsers often report an empty type for FLAC/M4A, so fall back to the extension */
function resolveContentType(file: File, ext: string): string {
  return file.type.startsWith('audio/') ? file.type : AUDIO_CONTENT_TYPES[ext];
}

function getErrorMessage(err: any, fallback: string): string {
  return err?.response?.data?.message || err?.message || fallback;
}

export const BatchTrackUploadModal: React.FC<BatchTrackUploadModalProps> = ({
  isOpen,
  onClose,
  albumId,
  albumTitle,
  onSuccess,
}) => {
  const [items, setItems] = useState<TrackUploadQueueItem[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [allCompleted, setAllCompleted] = useState(false);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    if (!isOpen) {
      setItems([]);
      setGlobalError(null);
      setIsProcessing(false);
      setAllCompleted(false);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const updateItem = (id: string, patch: Partial<TrackUploadQueueItem>) => {
    setItems((prev) => prev.map((i) => (i.id === id ? { ...i, ...patch } : i)));
  };

  const handleFiles = (fileList: FileList | null) => {
    if (!fileList || fileList.length === 0) return;
    setGlobalError(null);

    const newItems: TrackUploadQueueItem[] = [];
    const errors: string[] = [];
    let remainingSlots = MAX_BATCH_SIZE - items.filter((i) => !i.trackId).length;

    Array.from(fileList).forEach((file) => {
      const ext = `.${file.name.split('.').pop()?.toLowerCase()}`;
      if (!ALLOWED_EXTENSIONS.includes(ext)) {
        errors.push(`"${file.name}" has unsupported format. Only MP3, FLAC, WAV, AAC, and M4A are allowed.`);
        return;
      }

      if (file.size === 0) {
        errors.push(`"${file.name}" is empty.`);
        return;
      }

      if (file.size > MAX_AUDIO_SIZE_BYTES) {
        errors.push(`"${file.name}" exceeds the maximum allowed size of 50 MB.`);
        return;
      }

      if (remainingSlots <= 0) {
        errors.push(`"${file.name}" was skipped — a batch can contain at most ${MAX_BATCH_SIZE} tracks.`);
        return;
      }
      remainingSlots--;

      newItems.push({
        id: `${file.name}-${file.size}-${Date.now()}-${Math.random()}`,
        file,
        title: cleanTitleFromFilename(file.name),
        sizeBytes: file.size,
        extension: ext.replace('.', '').toUpperCase(),
        contentType: resolveContentType(file, ext),
        status: 'idle',
        progress: 0,
      });
    });

    if (errors.length > 0) {
      setGlobalError(errors.join(' '));
    }

    setItems((prev) => [...prev, ...newItems]);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    if (!isProcessing) setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    if (!isProcessing) {
      handleFiles(e.dataTransfer.files);
    }
  };

  const handleTitleChange = (id: string, newTitle: string) => {
    updateItem(id, { title: newTitle });
  };

  const handleRemoveItem = (id: string) => {
    if (isProcessing) return;
    // Registered tracks already exist server-side, so they stay in the queue for retry
    setItems((prev) => prev.filter((item) => item.id !== id || item.trackId));
  };

  /** Uploads one file to its presigned URL. Resolves to true on success; never throws. */
  const uploadItem = async (item: TrackUploadQueueItem, uploadUrl: string): Promise<boolean> => {
    updateItem(item.id, { status: 'uploading', progress: 10, errorMessage: undefined });
    try {
      await catalogService.uploadToPresignedUrl(
        uploadUrl,
        item.file,
        (percent) => updateItem(item.id, { progress: Math.max(10, percent) }),
        item.contentType
      );
      updateItem(item.id, { status: 'completed', progress: 100 });
      return true;
    } catch (uploadErr: any) {
      updateItem(item.id, {
        status: 'error',
        errorMessage: getErrorMessage(uploadErr, 'Failed to upload audio to storage'),
      });
      return false;
    }
  };

  const handleStartUpload = async () => {
    if (isProcessing) return;
    const pending = items.filter((i) => i.status !== 'completed');
    if (pending.length === 0) return;
    setGlobalError(null);

    // Validate titles
    if (pending.some((i) => !i.title.trim())) {
      setGlobalError('Every track must have a valid title.');
      return;
    }

    setIsProcessing(true);
    const toCreate = pending.filter((i) => !i.trackId);
    const toRetry = pending.filter((i) => i.trackId);
    const targets: { item: TrackUploadQueueItem; uploadUrl: string }[] = [];
    let failedCount = 0;

    try {
      // Step 1: Register new tracks in one batch call to obtain presigned upload URLs
      if (toCreate.length > 0) {
        toCreate.forEach((i) => updateItem(i.id, { status: 'creating', progress: 5, errorMessage: undefined }));

        try {
          const batchRequest: BatchTrackItemRequest[] = toCreate.map((item) => ({
            title: item.title.trim(),
            fileName: item.file.name,
            fileSizeBytes: item.sizeBytes,
            contentType: item.contentType,
          }));

          const batchResult = await catalogService.batchCreateTracks(albumId, { tracks: batchRequest });

          toCreate.forEach((item, index) => {
            const uploadTarget = batchResult.tracks[index];
            if (!uploadTarget) {
              failedCount++;
              updateItem(item.id, { status: 'error', errorMessage: 'Missing presigned URL' });
              return;
            }
            updateItem(item.id, { trackId: uploadTarget.trackId });
            targets.push({ item, uploadUrl: uploadTarget.uploadUrl });
          });
        } catch (err: any) {
          failedCount += toCreate.length;
          const message = getErrorMessage(err, 'Failed to register tracks');
          toCreate.forEach((i) => updateItem(i.id, { status: 'error', progress: 0, errorMessage: message }));
        }
      }

      // Step 2: Previously registered tracks whose upload failed get a fresh presigned URL
      await Promise.all(
        toRetry.map(async (item) => {
          updateItem(item.id, { status: 'creating', progress: 5, errorMessage: undefined });
          try {
            const { uploadUrl } = await catalogService.getTrackUploadUrl(item.trackId!, {
              fileName: item.file.name,
              fileSizeBytes: item.sizeBytes,
              contentType: item.contentType,
            });
            targets.push({ item, uploadUrl });
          } catch (err: any) {
            failedCount++;
            updateItem(item.id, {
              status: 'error',
              errorMessage: getErrorMessage(err, 'Failed to get upload URL'),
            });
          }
        })
      );

      // Step 3: Concurrently upload audio files directly to S3 via their presigned PUT URLs
      const results = await Promise.all(targets.map((t) => uploadItem(t.item, t.uploadUrl)));
      failedCount += results.filter((ok) => !ok).length;
    } finally {
      setIsProcessing(false);
    }

    if (failedCount === 0) {
      setAllCompleted(true);
      onSuccess?.();
    } else {
      setGlobalError(
        `${failedCount} of ${pending.length} ${pending.length === 1 ? 'track' : 'tracks'} failed. Hover a failed track for details, then retry.`
      );
    }
  };

  const totalSize = items.reduce((acc, curr) => acc + curr.sizeBytes, 0);
  const completedCount = items.filter((i) => i.status === 'completed').length;
  const pendingCount = items.length - completedCount;
  const hasFailures = items.some((i) => i.status === 'error');
  const hasRegisteredTracks = items.some((i) => i.trackId);
  const overallProgress =
    items.length > 0
      ? Math.round(
          items.reduce((acc, curr) => acc + curr.progress, 0) / items.length
        )
      : 0;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-md animate-in fade-in duration-200">
      <div className="bg-[#181818] border border-white/10 rounded-xl shadow-2xl w-full max-w-2xl max-h-[85vh] flex flex-col overflow-hidden text-white">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-white/10 bg-[#202020]">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-spotify-green/20 flex items-center justify-center text-spotify-green">
              <UploadCloud className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold">Batch Upload Audio Tracks</h2>
              <p className="text-xs text-spotify-muted">
                Adding tracks to <span className="text-white font-medium">{albumTitle}</span>
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            disabled={isProcessing}
            className="p-1 rounded-full text-spotify-muted hover:text-white hover:bg-white/10 transition-colors disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Global Error Banner */}
        {globalError && (
          <div className="px-6 py-3 bg-red-500/10 border-b border-red-500/20 text-red-400 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span className="flex-1">{globalError}</span>
          </div>
        )}

        {/* Content / Scrollable Area */}
        <div className="p-6 overflow-y-auto space-y-4 flex-1">
          {/* Drag & Drop Zone */}
          {!allCompleted && (
            <div
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
              onClick={() => {
                if (!isProcessing) fileInputRef.current?.click();
              }}
              className={cn(
                'border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-200 flex flex-col items-center justify-center gap-2',
                isDragging
                  ? 'border-spotify-green bg-spotify-green/10 scale-[1.01]'
                  : 'border-white/20 hover:border-white/40 bg-white/[0.02] hover:bg-white/[0.05]',
                isProcessing && 'pointer-events-none opacity-50'
              )}
            >
              <input
                ref={fileInputRef}
                type="file"
                multiple
                accept="audio/mp3,audio/mpeg,audio/flac,audio/wav,audio/aac,audio/m4a,.mp3,.flac,.wav,.aac,.m4a"
                className="hidden"
                onChange={(e) => {
                  handleFiles(e.target.files);
                  // Reset so re-selecting the same file fires onChange again
                  e.target.value = '';
                }}
              />
              <div className="w-12 h-12 rounded-full bg-white/5 flex items-center justify-center text-spotify-muted">
                <FileAudio className="w-6 h-6 text-spotify-green" />
              </div>
              <p className="text-sm font-semibold text-white">
                Drag and drop audio files here, or <span className="text-spotify-green underline">browse</span>
              </p>
              <p className="text-xs text-spotify-muted">
                Supports MP3, FLAC, WAV, AAC, and M4A · Up to 50 MB per file
              </p>
            </div>
          )}

          {/* Overall Upload Progress Summary */}
          {isProcessing && (
            <div className="p-4 rounded-lg bg-black/40 border border-white/10 space-y-2">
              <div className="flex justify-between text-xs">
                <span className="text-spotify-muted">Overall Upload Progress</span>
                <span className="font-semibold text-spotify-green">{overallProgress}%</span>
              </div>
              <div className="w-full bg-white/10 h-2 rounded-full overflow-hidden">
                <div
                  className="bg-spotify-green h-full transition-all duration-300"
                  style={{ width: `${overallProgress}%` }}
                />
              </div>
            </div>
          )}

          {/* Queue List */}
          {items.length > 0 && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs text-spotify-muted px-1">
                <span>
                  Track Queue ({items.length}) · {formatBytes(totalSize)} total
                </span>
                {!isProcessing && !allCompleted && !hasRegisteredTracks && (
                  <button
                    onClick={() => setItems([])}
                    className="hover:text-red-400 transition-colors"
                  >
                    Clear All
                  </button>
                )}
              </div>

              <div className="space-y-2">
                {items.map((item, index) => (
                  <div
                    key={item.id}
                    className={cn(
                      'p-3 rounded-lg border transition-all flex flex-col gap-2',
                      item.status === 'completed'
                        ? 'bg-emerald-950/20 border-emerald-500/30'
                        : item.status === 'error'
                        ? 'bg-red-950/20 border-red-500/30'
                        : 'bg-white/5 border-white/5 hover:border-white/10'
                    )}
                  >
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-mono text-spotify-muted w-5 text-right">
                        #{index + 1}
                      </span>
                      <div className="w-8 h-8 rounded bg-white/10 flex items-center justify-center shrink-0">
                        <Music className="w-4 h-4 text-spotify-muted" />
                      </div>

                      {/* Title input or label */}
                      <div className="flex-1 min-w-0">
                        {!isProcessing && !allCompleted && !item.trackId ? (
                          <input
                            type="text"
                            value={item.title}
                            onChange={(e) => handleTitleChange(item.id, e.target.value)}
                            placeholder="Track Title"
                            className="w-full bg-black/40 border border-white/10 rounded px-2.5 py-1 text-xs text-white focus:outline-none focus:border-spotify-green font-medium"
                          />
                        ) : (
                          <div className="text-xs font-semibold text-white truncate">
                            {item.title}
                          </div>
                        )}
                        <div className="flex items-center gap-2 mt-0.5 text-[11px] text-spotify-muted">
                          <span className="font-mono bg-white/10 px-1 py-0.2 rounded text-[10px]">
                            {item.extension}
                          </span>
                          <span>{formatBytes(item.sizeBytes)}</span>
                          <span className="truncate max-w-[200px]">({item.file.name})</span>
                        </div>
                      </div>

                      {/* Status indicator */}
                      <div className="flex items-center gap-2 shrink-0">
                        {item.status === 'creating' && (
                          <div className="flex items-center gap-1.5 text-xs text-yellow-400">
                            <Loader2 className="w-4 h-4 animate-spin" />
                            <span className="hidden sm:inline">Registering...</span>
                          </div>
                        )}
                        {item.status === 'uploading' && (
                          <div className="flex items-center gap-1.5 text-xs text-spotify-green">
                            <Loader2 className="w-4 h-4 animate-spin" />
                            <span className="font-mono">{item.progress}%</span>
                          </div>
                        )}
                        {item.status === 'completed' && (
                          <div className="flex items-center gap-1.5 text-xs text-emerald-400">
                            <CheckCircle2 className="w-4 h-4" />
                            <span className="hidden sm:inline">Uploaded</span>
                          </div>
                        )}
                        {item.status === 'error' && (
                          <div
                            className="flex items-center gap-1 text-xs text-red-400"
                            title={item.errorMessage || 'Upload failed'}
                          >
                            <AlertCircle className="w-4 h-4" />
                            <span className="hidden sm:inline">Failed</span>
                          </div>
                        )}
                        {(item.status === 'idle' || item.status === 'error') && !item.trackId && !isProcessing && (
                          <button
                            onClick={() => handleRemoveItem(item.id)}
                            className="p-1.5 text-spotify-muted hover:text-red-400 rounded transition-colors"
                            title="Remove file"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </div>

                    {/* Per-track progress bar */}
                    {(item.status === 'uploading' || item.status === 'creating') && (
                      <div className="w-full bg-white/10 h-1.5 rounded-full overflow-hidden">
                        <div
                          className="bg-spotify-green h-full transition-all duration-150"
                          style={{ width: `${item.progress}%` }}
                        />
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Success summary */}
          {allCompleted && (
            <div className="p-6 rounded-xl bg-emerald-950/20 border border-emerald-500/30 text-center space-y-2">
              <div className="w-12 h-12 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-6 h-6" />
              </div>
              <h3 className="text-base font-bold text-white">All Tracks Uploaded Successfully!</h3>
              <p className="text-xs text-spotify-muted">
                {completedCount} audio files were safely streamed directly into S3 object storage. Audio
                processing will begin automatically via background workers.
              </p>
            </div>
          )}
        </div>

        {/* Footer Actions */}
        <div className="px-6 py-4 border-t border-white/10 bg-[#202020] flex items-center justify-between">
          <div>
            {!allCompleted && !isProcessing && items.length > 0 && (
              <button
                onClick={() => fileInputRef.current?.click()}
                className="flex items-center gap-1 text-xs text-spotify-muted hover:text-white transition-colors"
              >
                <Plus className="w-4 h-4" />
                Add More
              </button>
            )}
          </div>
          <div className="flex items-center gap-3">
            <button
              onClick={onClose}
              disabled={isProcessing}
              className="px-4 py-2 text-xs font-semibold text-white/80 hover:text-white transition-colors disabled:opacity-50"
            >
              {allCompleted ? 'Close' : 'Cancel'}
            </button>
            {!allCompleted && (
              <button
                onClick={handleStartUpload}
                disabled={pendingCount === 0 || isProcessing}
                className={cn(
                  'px-6 py-2 rounded-full text-xs font-bold text-black bg-spotify-green hover:bg-spotify-green-hover transition-all duration-200 flex items-center gap-2',
                  (pendingCount === 0 || isProcessing) && 'opacity-50 cursor-not-allowed hover:bg-spotify-green'
                )}
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Uploading Tracks...
                  </>
                ) : (
                  `${hasFailures ? 'Retry' : 'Upload'} ${pendingCount} ${pendingCount === 1 ? 'Track' : 'Tracks'}`
                )}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
