import {
  Bell,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  LogOut,
  Search,
  Settings,
  Shield,
  User as UserIcon,
} from 'lucide-react';
import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { cn } from '../../utils/cn';
import { Button } from '../common/Button';

interface TopBarProps {
  scrolled?: boolean;
}

export const TopBar: React.FC<TopBarProps> = ({ scrolled = false }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, isAuthenticated, logout } = useAuth();
  const [dropdownOpen, setDropdownOpen] = useState<boolean>(false);
  const [searchValue, setSearchValue] = useState<string>('');

  const isSearchPage = location.pathname.startsWith('/search');

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchValue.trim()) {
      navigate(`/search?q=${encodeURIComponent(searchValue.trim())}`);
    }
  };

  return (
    <header
      className={cn(
        'sticky top-0 z-30 flex items-center justify-between px-6 py-3 transition-colors duration-300',
        scrolled ? 'bg-spotify-base/95 backdrop-blur-md shadow-md' : 'bg-transparent'
      )}
    >
      {/* Left: Navigation Arrows & Search */}
      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate(-1)}
            title="Go back"
            className="flex items-center justify-center w-8 h-8 rounded-full bg-black/60 hover:bg-black/80 text-white transition-colors cursor-pointer"
          >
            <ChevronLeft className="w-5 h-5" />
          </button>
          <button
            onClick={() => navigate(1)}
            title="Go forward"
            className="flex items-center justify-center w-8 h-8 rounded-full bg-black/60 hover:bg-black/80 text-white transition-colors cursor-pointer"
          >
            <ChevronRight className="w-5 h-5" />
          </button>
        </div>

        {/* Live Search Input when on Search View */}
        {isSearchPage && (
          <form onSubmit={handleSearchSubmit} className="relative w-80 max-w-sm">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-spotify-muted">
              <Search className="w-4 h-4" />
            </div>
            <input
              type="text"
              value={searchValue}
              onChange={(e) => setSearchValue(e.target.value)}
              placeholder="What do you want to play?"
              className="w-full rounded-full bg-[#242424] hover:bg-[#2a2a2a] focus:bg-[#2a2a2a] pl-9 pr-4 py-2 text-sm text-white placeholder-spotify-muted border border-transparent focus:border-white/40 focus:outline-none transition-all"
            />
          </form>
        )}
      </div>

      {/* Right: Auth Actions or User Profile Pill */}
      <div className="flex items-center gap-4">
        {isAuthenticated && user ? (
          <div className="flex items-center gap-3">
            {user.role === 'Admin' && (
              <span className="hidden sm:inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-amber-500/20 text-amber-300 text-xs font-bold border border-amber-500/30">
                <Shield className="w-3.5 h-3.5" />
                Admin
              </span>
            )}

            {user.role === 'Artist' && (
              <Link
                to="/artist-studio"
                className="hidden sm:inline-flex items-center gap-1 px-3 py-1.5 rounded-full bg-white/10 hover:bg-white/20 text-white text-xs font-bold transition-colors"
              >
                Artist Studio
                <ExternalLink className="w-3 h-3" />
              </Link>
            )}

            <button
              title="Notifications"
              className="flex items-center justify-center w-8 h-8 rounded-full bg-black/60 hover:bg-black/80 text-spotify-muted hover:text-white transition-colors"
            >
              <Bell className="w-4 h-4" />
            </button>

            {/* Profile Dropdown */}
            <div className="relative">
              <button
                onClick={() => setDropdownOpen((prev) => !prev)}
                className="flex items-center gap-2 p-1 pl-1.5 pr-2.5 rounded-full bg-black/70 hover:bg-black/90 text-white font-bold text-xs transition-colors border border-white/10 cursor-pointer"
              >
                <div className="flex items-center justify-center w-7 h-7 rounded-full bg-[#282828] text-white overflow-hidden font-bold">
                  {user.profilePicUrl ? (
                    <img src={user.profilePicUrl} alt={user.userName} className="w-full h-full object-cover" />
                  ) : (
                    <UserIcon className="w-4 h-4 text-spotify-muted" />
                  )}
                </div>
                <span className="max-w-[100px] truncate">{user.userName}</span>
                <ChevronDown className="w-3.5 h-3.5 text-spotify-muted" />
              </button>

              {dropdownOpen && (
                <>
                  <div
                    className="fixed inset-0 z-40"
                    onClick={() => setDropdownOpen(false)}
                  />
                  <div className="absolute right-0 mt-2 w-48 rounded-md bg-[#282828] p-1 shadow-spotify-card border border-white/10 z-50 animate-in fade-in zoom-in-95 text-xs font-semibold">
                    <Link
                      to="/profile"
                      onClick={() => setDropdownOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 text-white hover:bg-white/10 rounded-sm transition-colors"
                    >
                      <UserIcon className="w-4 h-4" />
                      Account Profile
                    </Link>
                    <Link
                      to="/settings"
                      onClick={() => setDropdownOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 text-white hover:bg-white/10 rounded-sm transition-colors"
                    >
                      <Settings className="w-4 h-4" />
                      Settings
                    </Link>
                    <div className="my-1 border-t border-white/10" />
                    <button
                      onClick={() => {
                        setDropdownOpen(false);
                        logout();
                      }}
                      className="flex items-center gap-2.5 w-full px-3 py-2 text-red-400 hover:bg-white/10 rounded-sm transition-colors text-left"
                    >
                      <LogOut className="w-4 h-4" />
                      Log out
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <Link
              to="/register"
              className="text-spotify-muted hover:text-white font-bold text-sm transition-colors px-2 py-1"
            >
              Sign up
            </Link>
            <Link to="/login">
              <Button variant="white" size="md">
                Log in
              </Button>
            </Link>
          </div>
        )}
      </div>
    </header>
  );
};
