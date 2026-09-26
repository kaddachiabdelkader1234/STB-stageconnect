import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, AuthResponse } from '../../../core/services/auth.service';
import { StatsService, DashboardStats, StagiaireStats, EvaluationStats } from '../../../core/services/stats.service';
import { RealtimeService, RealtimeNotification } from '../../../core/services/realtime.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationService, NotificationMessage } from '../../../shared/notification.service';
import {
  BarChartComponent, BarData
} from '../../../shared/components/charts/bar-chart.component';
import {
  DonutChartComponent, DonutSlice
} from '../../../shared/components/charts/donut-chart.component';
import {
  PipelineChartComponent, PipelineStage
} from '../../../shared/components/charts/pipeline-chart.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    CommonModule, RouterModule,
    BarChartComponent, DonutChartComponent, PipelineChartComponent
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardPageComponent implements OnInit {
  currentUser: AuthResponse | null = null;
  stats: DashboardStats | null = null;
  statsLoading = false;
  notifications: NotificationMessage[] = [];
  lastNotification?: RealtimeNotification;
  unreadCount = 0;

  // Dashboard stat cards
  dashboardCards = [
    { label: 'Total Stagiaires', value: 0, icon: 'people', color: 'primary' },
    { label: 'Candidatures en attente', value: 0, icon: 'pending_actions', color: 'yellow' },
    { label: 'Stagiaires acceptés', value: 0, icon: 'check_circle', color: 'green' },
    { label: 'Note moyenne', value: 0, icon: 'grading', color: 'blue', suffix: '/20' }
  ];

  // Charts data
  departementBarData: BarData[] = [];
  typeStageDonutData: DonutSlice[] = [];
  pipelineData: PipelineStage[] = [];

  constructor(
    private authService: AuthService,
    private statsService: StatsService,
    private realtimeService: RealtimeService,
    public themeService: ThemeService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getUserInfo();

    this.authService.fetchUserData().subscribe({
      next: (userData) => { this.currentUser = userData; },
      error: () => { /* keep cached user data if refresh fails */ }
    });

    if (this.currentUser?.role === 'ADMIN') {
      this.statsLoading = true;
      this.statsService.getDashboardStats().subscribe({
        next: (data) => {
          this.stats = data;
          this.statsLoading = false;
          this.updateDashboardCards();
          this.updateCharts();
        },
        error: () => { this.statsLoading = false; }
      });
    }

    // Listen to real-time notifications
    this.realtimeService.newNotification$.subscribe((notif) => {
      this.lastNotification = notif;
      this.unreadCount++;
    });
  }

  private updateDashboardCards(): void {
    if (!this.stats) return;
    const s = this.stats.stagiaires;
    this.dashboardCards[0].value = s.totalStagiaires;
    this.dashboardCards[1].value = s.pendingCandidatures;
    this.dashboardCards[2].value = s.acceptedStagiaires;
    this.dashboardCards[3].value = Math.round(this.stats.evaluations.averageScore);
  }

  private updateCharts(): void {
    if (!this.stats) return;

    // Bar chart — by department
    this.departementBarData = this.stats.stagiaires.byDepartement.map(d => ({
      label: d.departement,
      value: d.count
    }));

    // Donut chart — by type de stage
    this.typeStageDonutData = this.stats.stagiaires.byTypeStage.map((t, i) => ({
      label: t.typeStage,
      value: t.count,
      color: ['#002B5C', '#C8A951', '#004A8F', '#0067B3', '#4DB8E8'][i % 5]
    }));

    // Pipeline — candidatures status funnel
    const byStatut: Record<string, number> = {};
    this.stats.stagiaires.byStatut.forEach(s => { byStatut[s.statut] = s.count; });
    this.pipelineData = [
      { label: 'En attente', value: byStatut['EnAttente'] || 0 },
      { label: 'Acceptée', value: byStatut['Acceptee'] || 0 },
      { label: 'En cours', value: byStatut['EnCours'] || 0 },
      { label: 'Terminé', value: byStatut['Termine'] || 0 },
      { label: 'Rejetée', value: byStatut['Rejetee'] || 0 }
    ];
  }

  getUserImage(): string | null {
    if (this.currentUser?.imageBase64) {
      return `data:image/jpeg;base64,${this.currentUser.imageBase64}`;
    }
    return null;
  }

  getUserInitial(): string {
    return this.currentUser?.firstName?.charAt(0).toUpperCase() || 'U';
  }

  formatRole(role: string | undefined): string {
    if (!role) return 'User';
    const roleMap: { [key: string]: string } = {
      'ADMIN': 'Admin',
      'LEARNER': 'Learner',
      'TRAINER': 'Encadrant'
    };
    return roleMap[role] || role;
  }

  isAdmin(): boolean {
    return this.currentUser?.role === 'ADMIN';
  }

  /** Tailwind classes must exist at build time, so card accents are pre-declared here and
   *  applied with [ngClass] instead of interpolating class names (which Tailwind never sees). */
  cardStyle(color: string): Record<string, boolean> {
    return { 'bg-white': true, 'border-gray-100': true, 'dark:bg-gray-900': true, 'dark:border-gray-800': true };
  }

  iconBg(color: string): Record<string, boolean> {
    const map: Record<string, Record<string, boolean>> = {
      primary: { 'bg-primary/10': true, 'dark:bg-primary/20': true },
      yellow: { 'bg-yellow-100': true, 'dark:bg-yellow-500/20': true },
      green: { 'bg-green-100': true, 'dark:bg-green-500/20': true },
      blue: { 'bg-blue-100': true, 'dark:bg-blue-500/20': true }
    };
    return map[color] ?? map['primary'];
  }

  iconText(color: string): Record<string, boolean> {
    const map: Record<string, Record<string, boolean>> = {
      primary: { 'text-primary': true, 'dark:text-blue-300': true },
      yellow: { 'text-yellow-600': true, 'dark:text-yellow-300': true },
      green: { 'text-green-600': true, 'dark:text-green-300': true },
      blue: { 'text-blue-600': true, 'dark:text-blue-300': true }
    };
    return map[color] ?? map['primary'];
  }

  toggleTheme(): void {
    const next = this.themeService.mode === 'light' ? 'dark' : 'light';
    this.themeService.setTheme(next);
  }

  dismissNotification(): void {
    this.lastNotification = undefined;
  }
}
