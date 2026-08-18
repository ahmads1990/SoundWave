import React, { useRef, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { PlayerBar } from './PlayerBar';
import { Sidebar } from './Sidebar';
import { TopBar } from './TopBar';

export const MainLayout: React.FC = () => {
  const [scrolled, setScrolled] = useState<boolean>(false);
  const mainScrollRef = useRef<HTMLDivElement>(null);

  const handleScroll = () => {
    if (mainScrollRef.current) {
      setScrolled(mainScrollRef.current.scrollTop > 30);
    }
  };

  return (
    <div className="flex flex-col h-screen w-screen bg-spotify-black overflow-hidden select-none">
      {/* Upper Area: Sidebar + Scrollable Main Content */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        {/* Left Sidebar */}
        <Sidebar className="hidden md:flex" />

        {/* Main Content Area */}
        <div className="flex-1 flex flex-col min-w-0 p-2 pl-0 overflow-hidden">
          <div
            ref={mainScrollRef}
            onScroll={handleScroll}
            className="flex-1 rounded-lg bg-spotify-base overflow-y-auto relative flex flex-col"
          >
            {/* Top Bar with Dynamic Blur */}
            <TopBar scrolled={scrolled} />

            {/* View Outlet */}
            <main className="flex-1 px-6 pb-8">
              <Outlet />
            </main>
          </div>
        </div>
      </div>

      {/* Bottom Persistent Spotify Player Bar */}
      <PlayerBar />
    </div>
  );
};
