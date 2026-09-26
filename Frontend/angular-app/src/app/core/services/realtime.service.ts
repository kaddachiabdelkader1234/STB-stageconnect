import { Injectable, OnDestroy } from '@angular/core';
import { Observable, Subject, throwError, timer, ObservableInput } from 'rxjs';
import { switchMap, retry, catchError, map } from 'rxjs/operators';
import * as signalR from '@microsoft/signalr';
// Infinite delay constant (not exported in some SignalR versions)
const INFINITE_DELAY = -1;
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

/**
 * Incoming notification pushed from the backend SignalR hub.
 */
export interface RealtimeNotification {
  type: string;
  message: string;
  timestamp: string;
  unread: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private destroy$ = new Subject<void>();

  /** Emits every new notification pushed from the hub. */
  private newNotificationSubject = new Subject<RealtimeNotification>();
  newNotification$ = this.newNotificationSubject.asObservable();

  connected = false;
  reconnectAttempts = 0;
  maxReconnectAttempts = 10;
  baseDelayMs = 2000;

  constructor(private authService: AuthService) {}

  /**
   * Start the SignalR connection.
   * The JWT is passed as the `access_token` query parameter (browsers cannot
   * set custom headers on WebSocket connections).
   */
  start(): void {
    if (this.hubConnection) {
      return; // already running
    }

    const token = this.authService.getToken();
    if (!token) {
      return; // not authenticated yet
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl.replace('/api/v1', '')}/hub/notifications`, {
        accessTokenFactory: () => Promise.resolve(token),
        skipNegotiation: true, // skip negotiate; we use a simple connect since we already have a token
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: () => {
          this.reconnectAttempts++;
          if (this.reconnectAttempts > this.maxReconnectAttempts) {
            return INFINITE_DELAY; // stop retrying
          }
          // Exponential back-off with jitter: 2s to 30s
          return Math.min(
            this.baseDelayMs * Math.pow(2, this.reconnectAttempts) + Math.random() * 1000,
            30000
          );
        }
      })
      .build();

    // Incoming notifications from the hub.
    this.hubConnection.on('NewNotification', (notification: any) => {
      this.newNotificationSubject.next(notification);
    });

    // Connection events.
    this.hubConnection.onclose(() => {
      this.connected = false;
      this.reconnectAttempts = 0;
    });

    this.startConnection();
  }

  private startConnection(): void {
    if (!this.hubConnection) return;

    this.hubConnection.start()
      .then(() => {
        this.connected = true;
        this.reconnectAttempts = 0;

        // Ask to join the admin broadcast group if the current user is an admin.
        const user = this.authService.getUserInfo();
        if (user?.role?.toUpperCase() === 'ADMIN') {
          this.hubConnection?.invoke('JoinAdminGroup').catch();
        }
      })
      .catch((err: any) => {
        this.connected = false;
        console.error('SignalR connection error:', err);
        // Retry with back-off
        timer(this.baseDelayMs)
          .pipe(
            switchMap(() => {
              if (this.reconnectAttempts >= this.maxReconnectAttempts) {
                return throwError(() => new Error('SignalR max reconnect attempts reached'));
              }
              return this.hubConnection!.start();
            }),
            catchError(() => {
              // Schedule next retry via timer recursion
              return timer(this.baseDelayMs);
            }),
            // eslint-disable-next-line @typescript-eslint/no-use-before-define
            switchMap(() => {
              if (this.reconnectAttempts < this.maxReconnectAttempts && !this.connected) {
                this.startConnection();
              }
              return [] as ObservableInput<never>;
            })
          )
          .subscribe();
      });
  }

  /**
   * Manually join the admin broadcast group (called from UI if needed).
   */
  joinAdminGroup(): void {
    if (this.hubConnection && this.connected) {
      this.hubConnection.invoke('JoinAdminGroup').catch();
    }
  }

  stop(): void {
    if (this.hubConnection) {
      this.hubConnection.stop().catch();
      this.hubConnection = null;
    }
    this.connected = false;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stop();
  }
}
