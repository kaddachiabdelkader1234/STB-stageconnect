import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubjectService } from '../../../core/services/subject.service';
import {
  SubjectAssignment,
  InternshipSubject,
  DIFFICULTY_LABELS
} from '../../../core/models/subject-matching.model';

@Component({
  selector: 'app-mon-sujet',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mon-sujet.component.html'
})
export class MonSujetComponent implements OnInit {
  private readonly subjectService = inject(SubjectService);

  assignment: SubjectAssignment | null = null;
  loading = true;
  actionLoading = false;
  error: string | null = null;
  success: string | null = null;

  // Change request modal
  showChangeModal = false;
  openSubjects: InternshipSubject[] = [];
  selectedRequestedSubjectId: string | null = null;
  changeReason = '';

  readonly difficultyLabels = DIFFICULTY_LABELS;

  ngOnInit(): void {
    this.loadAssignment();
  }

  loadAssignment(): void {
    this.loading = true;
    this.error = null;

    this.subjectService.getMyAssignment().subscribe({
      next: data => {
        this.assignment = data;
        this.loading = false;
      },
      error: err => {
        this.loading = false;
        if (err.status === 404) {
          this.assignment = null;
        } else {
          this.error = err.error?.error || 'Erreur lors du chargement de votre sujet de stage.';
        }
      }
    });
  }

  acceptSubject(): void {
    if (!this.assignment) return;

    if (!confirm('Confirmez-vous l\'acceptation de ce sujet de stage ?')) {
      return;
    }

    this.actionLoading = true;
    this.error = null;

    this.subjectService.acceptSubject(this.assignment.id).subscribe({
      next: updated => {
        this.assignment = updated;
        this.actionLoading = false;
        this.success = 'Félicitations ! Vous avez accepté ce sujet de stage. Votre affectation est enregistrée.';
      },
      error: err => {
        this.actionLoading = false;
        this.error = err.error?.error || 'Erreur lors de l\'acceptation du sujet.';
      }
    });
  }

  openChangeModal(): void {
    this.showChangeModal = true;
    this.changeReason = '';
    this.selectedRequestedSubjectId = null;

    // Load available open subjects
    this.subjectService.getSubjects({ status: 'Open' }).subscribe({
      next: data => {
        // Exclude current subject
        this.openSubjects = data.filter(s => s.id !== this.assignment?.subjectId);
      },
      error: err => {
        this.error = 'Impossible de charger les sujets alternatifs.';
      }
    });
  }

  closeChangeModal(): void {
    this.showChangeModal = false;
    this.changeReason = '';
    this.selectedRequestedSubjectId = null;
  }

  submitChangeRequest(): void {
    if (!this.assignment || !this.selectedRequestedSubjectId || !this.changeReason.trim()) {
      this.error = 'Veuillez sélectionner un sujet alternatif et indiquer votre motif.';
      return;
    }

    this.actionLoading = true;
    this.error = null;

    this.subjectService.requestChange(
      this.assignment.id,
      this.selectedRequestedSubjectId,
      this.changeReason.trim()
    ).subscribe({
      next: () => {
        this.actionLoading = false;
        this.closeChangeModal();
        this.success = 'Votre demande de changement a été transmise à l\'administration avec succès.';
        this.loadAssignment();
      },
      error: err => {
        this.actionLoading = false;
        this.error = err.error?.error || 'Erreur lors de la soumission de la demande.';
      }
    });
  }
}
