import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationCenterService } from '../../../core/services/notification-center.service';
import { NotificationService } from '../../../shared/notification.service';
import { NotificationCenterComponent } from '../../../shared/components/notification-center/notification-center.component';
import { NotificationComponent } from '../../../shared/components/notification/notification.component';
import { ThemeService } from '../../../core/services/theme.service';

/**
 * Workspace shell — the home of TRAINER (encadrant) and LEARNER (stagiaire) users.
 *
 * Deliberately a different experience from the admin console: no sidebar. A modern
 * top-bar workspace (Notion/Linear/Slack style):
 *   • slim sticky header — product mark, centered pill navigation, right cluster with
 *     the notification bell (opens the slide-over panel) and the account menu
 *   • mobile: horizontal scrollable pill nav, no hamburger needed for 3–4 items
 *   • the same NotificationCenter slide-over as the admin shell, fed by the shared
 *     NotificationCenterService
 *
 * Navigation is role-driven: the trainer gets their four destinations, the learner theirs,
 * straight from AuthService — no menu config duplication.
 */
@Component({
  selector: 'app-workspace-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgFor, NgIf, RouterOutlet, NotificationCenterComponent, NotificationComponent],
  templateUrl: './workspace-shell.component.html',
  styleUrl: './workspace-shell.component.scss'
})
export class WorkspaceShellComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly center = inject(NotificationCenterService);
  readonly toastService = inject(NotificationService);
  readonly themeService = inject(ThemeService);

  readonly centerService = this.center;
  readonly openPanel = this.center.open;
  readonly unread = this.center.unread;

  /** Non-null unread count for template comparisons. */
  readonly unreadCount = computed(() => this.unread() ?? 0);

  readonly profile = this.authService.getUserProfile();
  readonly isTrainer = this.authService.hasRole('TRAINER');

  readonly firstName = this.profile?.firstName ?? 'Utilisateur';
  readonly initial = this.firstName.charAt(0).toUpperCase();
  readonly roleLabel = this.isTrainer ? 'Encadrant' : 'Stagiaire';

  /** Account dropdown visibility. */
  readonly accountOpen = signal(false);

  /** Current URL (string) so pills highlight correctly after lazy navigation. */
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  readonly navigation = computed(() => {
    void this.currentUrl(); // recompute on navigation (kept for future route-driven badges)
    const items: Array<{ label: string; route: string; icon: string; exact: boolean }> = [];

    items.push({ label: 'Accueil', route: '/espace', icon: 'home', exact: true });

    if (this.isTrainer) {
      items.push({ label: 'Mes stagiaires', route: '/espace/mes-stagiaires', icon: 'supervisor_account', exact: false });
      items.push({ label: 'Journaux', route: '/espace/journaux', icon: 'rate_review', exact: false });
      items.push({ label: 'Évaluations', route: '/espace/evaluations', icon: 'grading', exact: false });
    } else {
      items.push({ label: 'Ma candidature', route: '/espace/ma-candidature', icon: 'assignment', exact: false });
      items.push({ label: 'Mon sujet', route: '/espace/mon-sujet', icon: 'lightbulb', exact: false });
      items.push({ label: 'Ma convention', route: '/espace/ma-convention', icon: 'description', exact: false });
      items.push({ label: 'Mon journal', route: '/espace/mon-journal', icon: 'menu_book', exact: false });
      items.push({ label: 'Mon évaluation', route: '/espace/mes-evaluations', icon: 'grading', exact: false });
    }

    return items;
  });

  ngOnInit(): void {
    // Lazy hydrate the unread badge for the bell.
    this.center.refreshUnread();
  }

  toggleTheme(): void {
    this.themeService.setTheme(this.themeService.isDark ? 'light' : 'dark');
  }

  toggleBell(): void {
    this.center.togglePanel();
  }

  toggleAccount(): void {
    this.accountOpen.update(v => !v);
  }

  isActive(route: string, exact = false): boolean {
    const url = this.currentUrl();
    return exact ? url === route : url.startsWith(route);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
