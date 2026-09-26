import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type ThemeMode = 'light' | 'dark' | 'system';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly storageKey = 'stb_theme';
  private currentMode: ThemeMode = this.loadSaved();

  /** Subscribe to theme changes; fires immediately with current value. */
  themeChanged = new Subject<ThemeMode>();

  constructor() {}

  /** Current active theme mode selection. */
  get mode(): ThemeMode {
    return this.currentMode;
  }

  /** Whether the dark theme is currently active (after resolving system preference). */
  get isDark(): boolean {
    if (this.currentMode === 'dark') return true;
    if (this.currentMode === 'light') return false;
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }

  /**
   * Change the theme mode selection.
   * `'system'` follows the OS/browser preference at runtime (and on change).
   */
  setTheme(mode: ThemeMode): void {
    this.currentMode = mode;
    localStorage.setItem(this.storageKey, mode);
    this.themeChanged.next(mode);
    this.applyToDocument(mode);
  }

  /** Apply the saved theme immediately to the document element (no transition). */
  applyStoredTheme(): void {
    this.applyToDocument(this.currentMode);
  }

  private loadSaved(): ThemeMode {
    try {
      const stored = localStorage.getItem(this.storageKey);
      if (stored === 'light' || stored === 'dark' || stored === 'system') {
        return stored;
      }
    } catch { /* localStorage may be unavailable */ }
    return 'system';
  }

  private applyToDocument(mode: ThemeMode): void {
    const isDark = mode === 'dark' || (mode === 'system' &&
      window.matchMedia?.('(prefers-color-scheme: dark)').matches);
    document.documentElement.classList.toggle('dark', isDark);
  }
}
