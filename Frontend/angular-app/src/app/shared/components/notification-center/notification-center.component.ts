import { Component, computed, inject, signal, effect, untracked, OnInit, OnDestroy } from '@angular/core';
import { DatePipe, NgIf, NgFor, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NotificationCenterService } from '../../../core/services/notification-center.service';
import { NotificationApiService, ApiNotification } from '../../../core/services/notification-api.service';
import { AuthService } from '../../../core/services/auth.service';
import { RealtimeService } from '../../../core/services/realtime.service';

/**
 * Slide-over notification panel (the "aside" the user asked for).
 *
 * Slides in from the right over a dimmed backdrop, like Notion/Linear's inbox:
 *   • list of notifications, newest first, click = mark as read
 *   • "Tout marquer comme lu" action
 *   • admin sees a recipient badge on each row (they receive platform-wide items)
 *
 * Shared by both shells — the admin header bell and the workspace top-bar bell toggle the
 * same NotificationCenterService.open signal.
 */
@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [DatePipe, NgIf, NgFor, NgClass],
  templateUrl: './notification-center.component.html',
  styleUrl: './notification-center.component.scss'
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  private readonly api = inject(NotificationApiService);
  private readonly authService = inject(AuthService);
  private readonly realtimeService = inject(RealtimeService);
  private readonly router = inject(Router);

  readonly center = inject(NotificationCenterService);

  readonly open = this.center.open;
  readonly items = signal<ApiNotification[]>([]);
  readonly loading = signal(false);
  readonly loadedOnce = signal(false);

  readonly isAdmin = this.authService.isAdmin();

  readonly filter = signal<'all' | 'unread'>('all');
  readonly filtered = computed(() => {
    const list = this.items();
    return this.filter() === 'unread' ? list.filter(n => !n.lu) : list;
  });

  readonly unread = this.center.unread;

  /** Non-null view of the unread signal for templates. */
  readonly unreadCount = computed(() => this.unread() ?? 0);

  private realtimeSub?: Subscription;

  constructor() {
    effect(() => {
      const isOpen = this.open();
      if (isOpen) {
        untracked(() => {
          this.load();
        });
      }
    }, { allowSignalWrites: true });
  }

  ngOnInit(): void {
    // Pre-load notifications so they are ready as soon as the panel opens
    this.load();

    this.realtimeSub = this.realtimeService.newNotification$.subscribe(() => {
      this.load();
    });
  }

  ngOnDestroy(): void {
    this.realtimeSub?.unsubscribe();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAll(1, 50).subscribe({
      next: result => {
        const items = result?.items ?? [];
        this.items.set(items);
        this.loading.set(false);
        this.loadedOnce.set(true);
        this.center.setUnread(items.filter(n => !n.lu).length);
      },
      error: () => this.loading.set(false)
    });
  }

  close(): void {
    this.center.closePanel();
  }

  markAsRead(n: ApiNotification): void {
    if (n.lu) return;
    this.api.markAsRead(n.id).subscribe({
      next: () => {
        n.lu = true;
        this.items.update(list => [...list]);
        this.center.setUnread(this.items().filter(x => !x.lu).length);
      },
      error: () => {}
    });
  }

  markAllRead(): void {
    const unreadItems = this.items().filter(n => !n.lu);
    unreadItems.forEach(n => this.markAsRead(n));
  }

  setFilter(f: 'all' | 'unread'): void {
    this.filter.set(f);
  }

  getTypeIcon(type: string): string {
    switch (type) {
      case 'CandidatureDeposee': return 'how_to_reg';
      case 'CandidatureAcceptee': return 'check_circle';
      case 'CandidatureRejetee': return 'cancel';
      case 'ConventionGeneree': return 'description';
      case 'EvaluationSoumise': return 'grading';
      case 'EvaluationValidee': return 'verified';
      case 'SujetPropose': return 'lightbulb';
      case 'SujetAccepte': return 'task_alt';
      case 'ChangementSujetDemande': return 'swap_horiz';
      case 'ChangementSujetTraite': return 'assignment_turned_in';
      case 'RappelDelai': return 'schedule';
      default: return 'notifications';
    }
  }

  getTypeColor(type: string): string {
    switch (type) {
      case 'CandidatureDeposee': return 'text-amber-600 bg-amber-500/10';
      case 'CandidatureAcceptee': return 'text-emerald-600 bg-emerald-500/10';
      case 'CandidatureRejetee': return 'text-red-600 bg-red-500/10';
      case 'ConventionGeneree': return 'text-sky-600 bg-sky-500/10';
      case 'EvaluationSoumise': return 'text-violet-600 bg-violet-500/10';
      case 'EvaluationValidee': return 'text-teal-600 bg-teal-500/10';
      case 'SujetPropose': return 'text-amber-500 bg-amber-500/10';
      case 'SujetAccepte': return 'text-emerald-500 bg-emerald-500/10';
      case 'ChangementSujetDemande': return 'text-indigo-500 bg-indigo-500/10';
      case 'ChangementSujetTraite': return 'text-blue-500 bg-blue-500/10';
      case 'RappelDelai': return 'text-amber-600 bg-amber-500/10';
      default: return 'text-gray-500 bg-gray-500/10';
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case 'CandidatureDeposee': return 'Nouvelle candidature';
      case 'CandidatureAcceptee': return 'Candidature acceptée';
      case 'CandidatureRejetee': return 'Candidature rejetée';
      case 'ConventionGeneree': return 'Convention générée';
      case 'EvaluationSoumise': return 'Évaluation enregistrée';
      case 'EvaluationValidee': return 'Évaluation validée';
      case 'SujetPropose': return 'Sujet proposé';
      case 'SujetAccepte': return 'Sujet accepté';
      case 'ChangementSujetDemande': return 'Changement de sujet';
      case 'ChangementSujetTraite': return 'Sujet traité';
      case 'RappelDelai': return 'Rappel';
      default: return 'Notification';
    }
  }

  getRoleLabel(role: string): string {
    switch (role) {
      case 'Stagiaire': return 'Stagiaire';
      case 'Encadrant': return 'Encadrant';
      case 'AdminRH': return 'Administration';
      default: return role;
    }
  }
}
