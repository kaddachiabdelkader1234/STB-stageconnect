import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { PermissionService } from '../../../core/services/permission.service';
import { NotificationCenterService } from '../../../core/services/notification-center.service';
import { ThemeService } from '../../../core/services/theme.service';
import { MENU_ITEMS, MenuItem } from '../../../core/config/menu.config';

/**
 * Admin console sidebar — dark, grouped, premium (Vercel/Stripe console style).
 *
 * The admin keeps a sidebar because their workspace is a management console with many
 * destinations; trainers and learners get the top-bar WorkspaceShell instead. Both shells
 * share the NotificationCenter slide-over via NotificationCenterService.
 */
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit {
  readonly themeService = inject(ThemeService);
  private readonly center = inject(NotificationCenterService);

  currentUser: any = null;

  /** Grouped nav: headers from MENU_ITEMS become section titles, routes become links. */
  groups: Array<{ title: string | null; items: MenuItem[] }> = [];

  /** Bell badge — null until first load (no flash of 0). */
  readonly unread = this.center.unread;

  constructor(
    private authService: AuthService,
    private permissionService: PermissionService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.currentUser = this.authService.getUserProfile();
      this.buildGroups();
      this.center.refreshUnread();
    }
  }

  toggleNotifications(): void {
    this.center.togglePanel();
  }

  toggleTheme(): void {
    this.themeService.setTheme(this.themeService.isDark ? 'light' : 'dark');
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  getUserInitial(): string {
    return this.currentUser?.firstName?.charAt(0).toUpperCase() || 'U';
  }

  formatRole(role: string | undefined): string {
    if (!role) return 'Admin';
    return role === 'ADMIN' ? 'Administrateur' : role;
  }

  private buildGroups(): void {
    const groups: Array<{ title: string | null; items: MenuItem[] }> = [];
    let current: { title: string | null; items: MenuItem[] } = { title: null, items: [] };

    for (const item of MENU_ITEMS) {
      // Role gate (same rules as the old filterMenuItems).
      if (item.roles && item.roles.length > 0 && !this.permissionService.hasAnyRole(item.roles)) {
        continue;
      }

      if (item.header) {
        if (current.items.length > 0) {
          groups.push(current);
        }
        current = { title: item.header, items: [] };
        continue;
      }

      if (item.route) {
        current.items.push(item);
      }
    }
    if (current.items.length > 0) {
      groups.push(current);
    }
    this.groups = groups;
  }
}
