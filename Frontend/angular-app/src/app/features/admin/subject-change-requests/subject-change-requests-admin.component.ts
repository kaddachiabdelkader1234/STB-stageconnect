import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubjectService } from '../../../core/services/subject.service';
import { SubjectChangeRequest, ChangeRequestStatus } from '../../../core/models/subject-matching.model';

@Component({
  selector: 'app-subject-change-requests-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subject-change-requests-admin.component.html'
})
export class SubjectChangeRequestsAdminComponent implements OnInit {
  private readonly subjectService = inject(SubjectService);

  requests: SubjectChangeRequest[] = [];
  loading = true;
  error: string | null = null;
  success: string | null = null;
  processingId: string | null = null;

  filterStatus: ChangeRequestStatus | '' = 'Pending';

  // Action modal
  showReviewModal = false;
  selectedRequest: SubjectChangeRequest | null = null;
  reviewAction: 'approve' | 'reject' = 'approve';
  reviewComment = '';

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading = true;
    this.error = null;

    this.subjectService.getChangeRequests(this.filterStatus || undefined).subscribe({
      next: data => {
        this.requests = data;
        this.loading = false;
      },
      error: err => {
        this.error = err.error?.error || 'Erreur lors du chargement des demandes.';
        this.loading = false;
      }
    });
  }

  openReviewModal(request: SubjectChangeRequest, action: 'approve' | 'reject'): void {
    this.selectedRequest = request;
    this.reviewAction = action;
    this.reviewComment = action === 'approve' ? 'Demande accordée en fonction de vos compétences.' : '';
    this.showReviewModal = true;
  }

  closeReviewModal(): void {
    this.showReviewModal = false;
    this.selectedRequest = null;
    this.reviewComment = '';
  }

  submitReview(): void {
    if (!this.selectedRequest) return;

    this.processingId = this.selectedRequest.id;
    this.error = null;

    const op = this.reviewAction === 'approve'
      ? this.subjectService.approveChangeRequest(this.selectedRequest.id, this.reviewComment)
      : this.subjectService.rejectChangeRequest(this.selectedRequest.id, this.reviewComment);

    op.subscribe({
      next: () => {
        this.processingId = null;
        this.closeReviewModal();
        this.success = this.reviewAction === 'approve'
          ? 'Demande de changement approuvée avec succès. Le stagiaire a été réaffecté.'
          : 'Demande de changement rejetée.';
        this.loadRequests();
      },
      error: err => {
        this.processingId = null;
        this.error = err.error?.error || 'Erreur lors du traitement de la demande.';
      }
    });
  }
}
