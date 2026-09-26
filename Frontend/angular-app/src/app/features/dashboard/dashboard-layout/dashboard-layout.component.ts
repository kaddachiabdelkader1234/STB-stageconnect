import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationCenterComponent } from '../../../shared/components/notification-center/notification-center.component';
import { NotificationComponent } from '../../../shared/components/notification/notification.component';
import { NotificationCenterService } from '../../../core/services/notification-center.service';
import { RealtimeService } from '../../../core/services/realtime.service';
import { ThemeService } from '../../../core/services/theme.service';
import { Subscription, filter } from 'rxjs';

/**
 * Admin console shell.
 *
 * Responsibilities are deliberately thin — the heavy lifting moved to services:
 *   • NotificationCenterService owns the slide-over state, the unread badge AND the
 *     realtime → toast wiring (so it works in both shells, not only here)
 *   • RealtimeService owns the SignalR connection lifecycle
 *   • this component just starts them and renders sidebar + outlet + overlays
 */
@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, CommonModule, NotificationCenterComponent, NotificationComponent],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.scss'
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  readonly center = inject(NotificationCenterService);
  readonly unread = this.center.unread;

  currentUser: any = null;
  currentRoute: string = '';
  private destroy$: Subscription = new Subscription();

  constructor(
    private authService: AuthService,
    private router: Router,
    private realtimeService: RealtimeService,
    private themeService: ThemeService
  ) {}

  toggleNotifications(): void {
    this.center.togglePanel();
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.currentUser = this.authService.getUserProfile();
      this.center.refreshUnread();
    }

    const routerEventsSubscription = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentRoute = event.url;
    });
    this.destroy$.add(routerEventsSubscription);

    this.themeService.applyStoredTheme();

    // SignalR: starts the connection; NotificationCenterService (injected app-wide) turns
    // pushes into toasts + badge updates for whichever shell is on screen.
    this.realtimeService.start();
    this.destroy$.add(() => this.realtimeService.stop());
  }

  ngOnDestroy(): void {
    this.destroy$.unsubscribe();
  }
}
