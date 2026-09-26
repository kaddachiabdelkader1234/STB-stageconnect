import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { HomePageComponent } from './features/home/home-page/home-page.component';
import { DashboardLayoutComponent } from './features/dashboard/dashboard-layout/dashboard-layout.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page/dashboard-page.component';
import { SignUpComponent } from './features/auth/sign-up/sign-up.component';
import { SignInComponent } from './features/auth/sign-in/sign-in.component';
import { WorkspaceShellComponent } from './features/dashboard/workspace-shell/workspace-shell.component';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { roleShellGuard } from './core/guards/role-shell.guard';
import { Role } from './core/enums/role.enum';

/**
 * Two shells, two worlds:
 *
 *   /admin/**  — ADMIN console (dark sidebar, management screens)
 *   /espace/** — TRAINER & LEARNER workspace (modern top-bar, personal screens)
 *
 * roleShellGuard keeps each role in its own shell; permissionGuard keeps feature pages
 * restricted. Feature components are shared where the API already scopes data by role
 * (stagiaires-list is the admin roster AND the trainer's "mes stagiaires").
 */
export const routes: Routes = [
  { path: '', component: HomePageComponent },

  // ---------- Auth ----------
  { path: 'auth/sign-in', component: SignInComponent },
  { path: 'auth/sign-up', component: SignUpComponent },
  {
    path: 'auth/reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },

  // ================= ADMIN CONSOLE (/admin) =================
  {
    path: 'admin',
    component: DashboardLayoutComponent,
    canActivate: [authGuard, roleShellGuard(['ADMIN'])],
    children: [
      { path: '', component: DashboardPageComponent },

      // ---------- Notifications ----------
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notification/notifications-list/notifications-list.component')
            .then(m => m.NotificationsListComponent)
      },

      // ---------- Gestion ----------
      {
        path: 'creer-encadrant',
        loadComponent: () =>
          import('./features/admin/create-encadrant/create-encadrant.component')
            .then(m => m.CreateEncadrantComponent)
      },
      {
        path: 'comptes',
        loadComponent: () =>
          import('./features/admin/accounts/accounts.component')
            .then(m => m.AccountsComponent)
      },
      {
        path: 'candidatures',
        loadComponent: () =>
          import('./features/candidature/candidatures-admin/candidatures-admin.component')
            .then(m => m.CandidaturesAdminComponent)
      },
      {
        path: 'conventions',
        loadComponent: () =>
          import('./features/convention/conventions-admin/conventions-admin.component')
            .then(m => m.ConventionsAdminComponent)
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./features/admin/audit-log/audit-log.component')
            .then(m => m.AuditLogComponent)
      },
      {
        path: 'stagiaires',
        loadComponent: () =>
          import('./features/stagiaire/stagiaires-list/stagiaires-list.component')
            .then(m => m.StagiairesListComponent)
      },

      // Admin also evaluates (can act on behalf of an encadrant) — reuse the trainer screen.
      {
        path: 'evaluations',
        loadComponent: () =>
          import('./features/evaluation/evaluation-encadrant/evaluation-encadrant.component')
            .then(m => m.EvaluationEncadrantComponent)
      },
      {
        path: 'subjects',
        loadComponent: () =>
          import('./features/admin/subjects/subjects-admin.component')
            .then(m => m.SubjectsAdminComponent)
      },
      {
        path: 'subject-change-requests',
        loadComponent: () =>
          import('./features/admin/subject-change-requests/subject-change-requests-admin.component')
            .then(m => m.SubjectChangeRequestsAdminComponent)
      },
      {
        path: 'profil',
        loadComponent: () =>
          import('./features/profile/profile.component')
            .then(m => m.ProfileComponent)
      }
    ]
  },

  // ================= MEMBER WORKSPACE (/espace) =================
  {
    path: 'espace',
    component: WorkspaceShellComponent,
    canActivate: [authGuard, roleShellGuard(['TRAINER', 'LEARNER'])],
    children: [
      { path: '', component: DashboardPageComponent },

      // ---------- TRAINER ----------
      {
        path: 'mes-stagiaires',
        canActivate: [permissionGuard],
        data: { roles: [Role.TRAINER] },
        loadComponent: () =>
          import('./features/stagiaire/stagiaires-list/stagiaires-list.component')
            .then(m => m.StagiairesListComponent)
      },
      {
        path: 'journaux',
        canActivate: [permissionGuard],
        data: { roles: [Role.TRAINER] },
        loadComponent: () =>
          import('./features/journal/journal-encadrant/journal-encadrant.component')
            .then(m => m.JournalEncadrantComponent)
      },
      {
        path: 'evaluations',
        canActivate: [permissionGuard],
        data: { roles: [Role.TRAINER] },
        loadComponent: () =>
          import('./features/evaluation/evaluation-encadrant/evaluation-encadrant.component')
            .then(m => m.EvaluationEncadrantComponent)
      },

      // ---------- LEARNER ----------
      {
        path: 'ma-candidature',
        canActivate: [permissionGuard],
        data: { roles: [Role.LEARNER] },
        loadComponent: () =>
          import('./features/candidature/ma-candidature/ma-candidature.component')
            .then(m => m.MaCandidatureComponent)
      },
      {
        path: 'ma-convention',
        canActivate: [permissionGuard],
        data: { roles: [Role.LEARNER] },
        loadComponent: () =>
          import('./features/convention/ma-convention/ma-convention.component')
            .then(m => m.MaConventionComponent)
      },
      {
        path: 'mon-journal',
        canActivate: [permissionGuard],
        data: { roles: [Role.LEARNER] },
        loadComponent: () =>
          import('./features/journal/mon-journal/mon-journal.component')
            .then(m => m.MonJournalComponent)
      },
      {
        path: 'mes-evaluations',
        canActivate: [permissionGuard],
        data: { roles: [Role.LEARNER] },
        loadComponent: () =>
          import('./features/evaluation/mon-evaluation/mon-evaluation.component')
            .then(m => m.MonEvaluationComponent)
      },
      {
        path: 'mon-sujet',
        canActivate: [permissionGuard],
        data: { roles: [Role.LEARNER] },
        loadComponent: () =>
          import('./features/subject/mon-sujet/mon-sujet.component')
            .then(m => m.MonSujetComponent)
      },

      // ---------- Shared ----------
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notification/notifications-list/notifications-list.component')
            .then(m => m.NotificationsListComponent)
      },
      {
        path: 'profil',
        loadComponent: () =>
          import('./features/profile/profile.component')
            .then(m => m.ProfileComponent)
      }
    ]
  },

  // Legacy /dashboard paths → role-aware redirect (kept for bookmarks / old sessions).
  {
    path: 'dashboard',
    canActivate: [authGuard, legacyDashboardRedirectGuard],
    children: []
  },

  { path: '**', redirectTo: '' }
];

/**
 * Sends /dashboard visitors to their role's new home. A guard rather than a redirect entry
 * because the target depends on the caller's role at navigation time.
 */
function legacyDashboardRedirectGuard(): boolean {
  const router = inject(Router);
  const authService = inject(AuthService);
  router.navigateByUrl(authService.hasRole('ADMIN') ? '/admin' : '/espace');
  return false;
}
