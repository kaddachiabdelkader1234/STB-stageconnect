import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationApiService, ApiNotification } from '../../../core/services/notification-api.service';

/**
 * The notifications panel.
 *
 * Scoping mirrors the backend (NotificationsController.ApplyReadScope):
 *   • ADMIN  — sees every notification in the platform. Each row carries a recipient badge
 *     ("Stagiaire — Yasmine") so the admin knows who the message was addressed to, and the
 *     wording makes sense ("La candidature de Yasmine a été acceptée" rather than "Votre
 *     candidature…"). Two tabs filter Toutes / Non lues.
 *   • TRAINER / LEARNER — see only their own notifications; no recipient badge needed and no
 *     tabs beyond the unread filter.
 */
@Component({
  selector: 'app-notifications-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications-list.component.html'
})
export class NotificationsListComponent implements OnInit {
  notifications: ApiNotification[] = [];
  isLoading = true;
  errorMessage = '';

  isAdmin = false;

  /** 'all' | 'unread' — the only two filters that matter here. */
  filtre: 'all' | 'unread' = 'all';

  constructor(
    private notificationApi: NotificationApiService,
    private authService: AuthService
  ) {
    this.isAdmin = this.authService.isAdmin();
  }

  ngOnInit(): void {
    this.loadNotifications();
  }

  get notificationsFiltrees(): ApiNotification[] {
    return this.filtre === 'unread'
      ? this.notifications.filter(n => !n.lu)
      : this.notifications;
  }

  get nonLues(): number {
    return this.notifications.filter(n => !n.lu).length;
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.notificationApi.getAll(1, 50).subscribe({
      next: (result) => {
        this.notifications = result.items;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Erreur lors du chargement des notifications.';
        this.isLoading = false;
      }
    });
  }

  marquerToutLu(): void {
    const cibles = this.notifications.filter(n => !n.lu);
    cibles.forEach(n => this.markAsRead(n, true));
    if (cibles.length === 0) {
      return;
    }
  }

  markAsRead(notification: ApiNotification, silent = false): void {
    if (notification.lu) return;
    this.notificationApi.markAsRead(notification.id).subscribe({
      next: () => { notification.lu = true; },
      error: () => {
        if (!silent) {
          this.errorMessage = 'Impossible de marquer la notification comme lue.';
        }
      }
    });
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case 'CandidatureAcceptee': return 'Candidature acceptée';
      case 'CandidatureRejetee': return 'Candidature rejetée';
      case 'ConventionGeneree': return 'Convention générée';
      case 'EvaluationSoumise': return 'Évaluation';
      case 'RappelDelai': return 'Rappel';
      default: return type;
    }
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'CandidatureAcceptee': return 'check_circle';
      case 'CandidatureRejetee': return 'cancel';
      case 'ConventionGeneree': return 'description';
      case 'EvaluationSoumise': return 'grading';
      case 'RappelDelai': return 'schedule';
      default: return 'notifications';
    }
  }

  getTypeColor(type: string): string {
    switch (type) {
      case 'CandidatureAcceptee': return 'text-green-600 bg-green-50 dark:bg-green-500/10 dark:text-green-400';
      case 'CandidatureRejetee': return 'text-red-600 bg-red-50 dark:bg-red-500/10 dark:text-red-400';
      case 'ConventionGeneree': return 'text-blue-600 bg-blue-50 dark:bg-blue-500/10 dark:text-blue-400';
      case 'EvaluationSoumise': return 'text-purple-600 bg-purple-50 dark:bg-purple-500/10 dark:text-purple-400';
      case 'RappelDelai': return 'text-orange-600 bg-orange-50 dark:bg-orange-500/10 dark:text-orange-400';
      default: return 'text-gray-600 bg-gray-100 dark:bg-gray-500/10 dark:text-gray-400';
    }
  }

  /** Human label for the recipient-role badge (admin view only). */
  getRoleLabel(role: string): string {
    switch (role) {
      case 'Stagiaire': return 'Stagiaire';
      case 'Encadrant': return 'Encadrant';
      case 'AdminRH': return 'Administration';
      default: return role;
    }
  }
}
