import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/services/auth.service';
import { toApiError } from '../../../core/http/api-error';

/** Message-bearing response shape from the resend-credentials endpoint. */
interface AuthResponse {
  message?: string;
}

/**
 * Summary shape returned by GET /api/v1/auth/users/all (and /users?role=…).
 * Mirrors the Java UserSummaryResponse — no token, no password hash, no image.
 */
export interface AccountSummary {
  userId: number;
  email: string;
  firstName: string;
  lastName?: string | null;
  phone?: string | null;
  role: 'ADMIN' | 'TRAINER' | 'LEARNER';
  createdAt?: string | null;
}

/**
 * Admin accounts management — see, filter and delete every account on the platform.
 *
 * Reads GET /api/v1/auth/users/all (ADMIN-only, enforced by the gateway) and deletes via
 * DELETE /api/v1/auth/user/{id}. The primary admin (userId 1) is protected server-side:
 * its delete button stays visible but returns a 409 with an explanation, shown as an error.
 */
@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './accounts.component.html'
})
export class AccountsComponent implements OnInit {
  accounts: AccountSummary[] = [];
  isLoading = true;
  errorMessage = '';
  successMessage = '';

  /** '' = all roles; otherwise 'ADMIN' | 'TRAINER' | 'LEARNER'. */
  filtreRole = '';
  recherche = '';

  /** Row pending deletion confirmation (userId), so the template can swap to confirm UI. */
  pendingDeleteId: number | null = null;
  isDeleting = false;

  /** Row pending credentials-resend confirmation (userId) — separate from delete. */
  pendingResendId: number | null = null;
  isResending = false;

  private readonly baseUrl = `${environment.authApiUrl}`;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadAccounts();
  }

  get currentUserId(): number | undefined {
    return this.authService.getUserInfo()?.userId;
  }

  get filteredAccounts(): AccountSummary[] {
    const q = this.recherche.trim().toLowerCase();
    return this.accounts.filter(a => {
      if (this.filtreRole && a.role !== this.filtreRole) {
        return false;
      }
      if (!q) {
        return true;
      }
      const haystack = `${a.firstName} ${a.lastName ?? ''} ${a.email}`.toLowerCase();
      return haystack.includes(q);
    });
  }

  get counts(): { admin: number; trainer: number; learner: number } {
    return {
      admin: this.accounts.filter(a => a.role === 'ADMIN').length,
      trainer: this.accounts.filter(a => a.role === 'TRAINER').length,
      learner: this.accounts.filter(a => a.role === 'LEARNER').length
    };
  }

  loadAccounts(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.http.get<AccountSummary[]>(`${this.baseUrl}/users/all`).subscribe({
      next: accounts => {
        this.accounts = accounts;
        this.isLoading = false;
      },
      error: err => {
        this.errorMessage = toApiError(err).message;
        this.isLoading = false;
      }
    });
  }

  askDelete(account: AccountSummary): void {
    this.pendingDeleteId = account.userId;
    this.successMessage = '';
    this.errorMessage = '';
  }

  cancelDelete(): void {
    this.pendingDeleteId = null;
  }

  confirmDelete(userId: number): void {
    this.isDeleting = true;
    this.errorMessage = '';
    this.http.delete(`${this.baseUrl}/user/${userId}`).subscribe({
      next: () => {
        this.isDeleting = false;
        this.pendingDeleteId = null;
        this.accounts = this.accounts.filter(a => a.userId !== userId);
        this.successMessage = 'Compte supprimé.';
      },
      error: err => {
        this.isDeleting = false;
        this.pendingDeleteId = null;
        this.errorMessage = toApiError(err).message;
      }
    });
  }

  /**
   * Resending is only offered for encadrants: their password is auto-generated and travels
   * only by email, so a lost welcome email means they cannot log in at all. Learners and
   * admins set their own password at sign-up.
   */
  canResend(account: AccountSummary): boolean {
    return account.role === 'TRAINER';
  }

  askResend(account: AccountSummary): void {
    this.pendingResendId = account.userId;
    this.successMessage = '';
    this.errorMessage = '';
  }

  cancelResend(): void {
    this.pendingResendId = null;
  }

  /**
   * Generates a new password and emails it — the old one is hashed and cannot be recovered,
   * so "resend" is really "reset and resend". The encadrant's current password stops working.
   */
  confirmResend(account: AccountSummary): void {
    this.isResending = true;
    this.errorMessage = '';
    this.http.post<AuthResponse>(`${this.baseUrl}/user/${account.userId}/resend-credentials`, {}).subscribe({
      next: res => {
        this.isResending = false;
        this.pendingResendId = null;
        this.successMessage = res?.message || `Nouveaux identifiants envoyés à ${account.email}.`;
      },
      error: err => {
        this.isResending = false;
        this.pendingResendId = null;
        this.errorMessage = toApiError(err).message;
      }
    });
  }

  roleLabel(role: string): string {
    switch (role) {
      case 'ADMIN': return 'Admin';
      case 'TRAINER': return 'Encadrant';
      case 'LEARNER': return 'Stagiaire';
      default: return role;
    }
  }

  roleBadgeClass(role: string): string {
    switch (role) {
      case 'ADMIN': return 'bg-purple-50 text-purple-700 border-purple-200';
      case 'TRAINER': return 'bg-blue-50 text-blue-700 border-blue-200';
      case 'LEARNER': return 'bg-green-50 text-green-700 border-green-200';
      default: return 'bg-gray-50 text-gray-700 border-gray-200';
    }
  }

  fullName(account: AccountSummary): string {
    return [account.firstName, account.lastName].filter(Boolean).join(' ') || '—';
  }

  initials(account: AccountSummary): string {
    const first = account.firstName?.[0] ?? '';
    const last = account.lastName?.[0] ?? '';
    return (first + last).toUpperCase() || '?';
  }

  avatarClass(role: string): string {
    switch (role) {
      case 'ADMIN': return 'bg-purple-100 text-purple-700';
      case 'TRAINER': return 'bg-blue-100 text-blue-700';
      default: return 'bg-green-100 text-green-700';
    }
  }
}
