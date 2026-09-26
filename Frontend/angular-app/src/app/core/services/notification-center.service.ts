import { Injectable, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { RealtimeService } from './realtime.service';
import { NotificationService } from '../../shared/notification.service';
import { RealtimeNotificationPresenter } from './realtime-notification-presenter.service';

/**
 * State + orchestration for the notification slide-over panel.
 *
 * One instance (providedIn root) shared by the admin shell and the member workspace shell:
 *   • `open`   — slide-over visibility (signal, toggled from the bell button)
 *   • `unread` — live unread counter for the bell badge (loaded from the API, incremented
 *                by SignalR pushes)
 *
 * The service also owns the realtime → toast wiring: every SignalR push becomes a branded
 * popup, regardless of which shell is currently displayed. Before this, the wiring lived in
 * the dashboard layout component and died with it.
 */
@Injectable({ providedIn: 'root' })
export class NotificationCenterService implements OnDestroy {
  private readonly apiUrl = `${environment.apiUrl}/notifications`;

  /** Slide-over visibility. */
  readonly open = signal(false);

  /** Unread count for the bell badge; null until first load. */
  readonly unread = signal<number | null>(null);

  private realtimeSub?: Subscription;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private realtimeService: RealtimeService,
    private toastService: NotificationService,
    private presenter: RealtimeNotificationPresenter
  ) {
    // Every realtime push: bump the badge + show a toast. Started lazily so no connection
    // exists for anonymous visitors — start() itself no-ops without a token.
    this.realtimeSub = this.realtimeService.newNotification$.subscribe(n => {
      if (this.unread() !== null) {
        this.unread.update(c => (c ?? 0) + 1);
      }
      const mapping = this.presenter.present(n);
      this.toastService.showNotification({
        type: mapping.toastType,
        title: mapping.title,
        message: n.message,
        duration: 7000
      });
    });
  }

  /** Loads the unread badge — call once per shell after authentication. */
  refreshUnread(): void {
    if (!this.authService.isAuthenticated()) {
      return;
    }
    this.http.get<any>(`${this.apiUrl}?page=1&pageSize=50`).subscribe({
      next: result => {
        const items: Array<{ lu: boolean }> = result?.items ?? [];
        this.unread.set(items.filter(n => !n.lu).length);
      },
      error: () => this.unread.set(0)
    });
  }

  openPanel(): void {
    this.open.set(true);
  }

  closePanel(): void {
    this.open.set(false);
  }

  togglePanel(): void {
    this.open.update(v => !v);
  }

  /** Called by the panel after marking items read, so the badge stays honest. */
  setUnread(count: number): void {
    this.unread.set(count);
  }

  ngOnDestroy(): void {
    this.realtimeSub?.unsubscribe();
  }
}
