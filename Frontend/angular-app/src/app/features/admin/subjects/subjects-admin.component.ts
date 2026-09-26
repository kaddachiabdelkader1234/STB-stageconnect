import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SubjectService } from '../../../core/services/subject.service';
import {
  InternshipSubject,
  SubjectCreateDto,
  SubjectUpdateDto,
  SubjectStatus,
  SubjectDifficulty,
  DIFFICULTY_LABELS,
  SUBJECT_STATUS_LABELS
} from '../../../core/models/subject-matching.model';

@Component({
  selector: 'app-subjects-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subjects-admin.component.html'
})
export class SubjectsAdminComponent implements OnInit {
  private readonly subjectService = inject(SubjectService);

  subjects: InternshipSubject[] = [];
  loading = true;
  saving = false;
  error: string | null = null;
  success: string | null = null;

  // Filters
  filterSearch = '';
  filterDepartment = '';
  filterStatus = '';

  // Modal state
  showModal = false;
  isEditing = false;
  editingId: string | null = null;

  // Form model
  formTitle = '';
  formDescription = '';
  formProblemStatement = '';
  formDepartment = '';
  formTypeStage = 'PFE';
  formDifficulty: SubjectDifficulty = 'Intermediate';
  formStartDate = '';
  formEndDate = '';
  formAvailablePositions = 1;
  formStatus: SubjectStatus = 'Open';
  formEducationRequirements = '';
  formExperienceRequirements = '';

  // Skill input helpers
  requiredSkillInput = '';
  formRequiredSkills: string[] = [];

  preferredSkillInput = '';
  formPreferredSkills: string[] = [];

  readonly difficultyLabels = DIFFICULTY_LABELS;
  readonly statusLabels = SUBJECT_STATUS_LABELS;

  ngOnInit(): void {
    this.loadSubjects();
  }

  loadSubjects(): void {
    this.loading = true;
    this.error = null;

    this.subjectService.getSubjects({
      search: this.filterSearch || undefined,
      department: this.filterDepartment || undefined,
      status: this.filterStatus || undefined
    }).subscribe({
      next: data => {
        this.subjects = data;
        this.loading = false;
      },
      error: err => {
        this.error = err.error?.error || 'Erreur lors du chargement des sujets.';
        this.loading = false;
      }
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.formTitle = '';
    this.formDescription = '';
    this.formProblemStatement = '';
    this.formDepartment = 'Informatique & Digitalisation';
    this.formTypeStage = 'PFE';
    this.formDifficulty = 'Intermediate';
    this.formStartDate = new Date().toISOString().substring(0, 10);
    this.formEndDate = new Date(Date.now() + 180 * 24 * 3600 * 1000).toISOString().substring(0, 10);
    this.formAvailablePositions = 1;
    this.formStatus = 'Open';
    this.formEducationRequirements = 'Diplôme National d’Ingénieur ou Master en Informatique';
    this.formExperienceRequirements = '';
    this.formRequiredSkills = [];
    this.formPreferredSkills = [];
    this.requiredSkillInput = '';
    this.preferredSkillInput = '';
    this.showModal = true;
  }

  openEditModal(subject: InternshipSubject): void {
    this.isEditing = true;
    this.editingId = subject.id;
    this.formTitle = subject.title;
    this.formDescription = subject.description;
    this.formProblemStatement = subject.problemStatement;
    this.formDepartment = subject.department;
    this.formTypeStage = subject.typeStage;
    this.formDifficulty = subject.difficulty;
    this.formStartDate = subject.startDate;
    this.formEndDate = subject.endDate;
    this.formAvailablePositions = subject.availablePositions;
    this.formStatus = subject.status;
    this.formEducationRequirements = subject.educationRequirements || '';
    this.formExperienceRequirements = subject.experienceRequirements || '';
    this.formRequiredSkills = [...(subject.requiredSkills || [])];
    this.formPreferredSkills = [...(subject.preferredSkills || [])];
    this.requiredSkillInput = '';
    this.preferredSkillInput = '';
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingId = null;
  }

  addRequiredSkill(): void {
    const s = this.requiredSkillInput.trim();
    if (s && !this.formRequiredSkills.includes(s)) {
      this.formRequiredSkills.push(s);
      this.requiredSkillInput = '';
    }
  }

  removeRequiredSkill(index: number): void {
    this.formRequiredSkills.splice(index, 1);
  }

  addPreferredSkill(): void {
    const s = this.preferredSkillInput.trim();
    if (s && !this.formPreferredSkills.includes(s)) {
      this.formPreferredSkills.push(s);
      this.preferredSkillInput = '';
    }
  }

  removePreferredSkill(index: number): void {
    this.formPreferredSkills.splice(index, 1);
  }

  saveSubject(): void {
    if (!this.formTitle || !this.formDescription || !this.formProblemStatement || !this.formDepartment) {
      this.error = 'Veuillez remplir tous les champs obligatoires.';
      return;
    }

    this.saving = true;
    this.error = null;

    if (this.isEditing && this.editingId) {
      const dto: SubjectUpdateDto = {
        title: this.formTitle,
        description: this.formDescription,
        problemStatement: this.formProblemStatement,
        department: this.formDepartment,
        typeStage: this.formTypeStage,
        difficulty: this.formDifficulty,
        startDate: this.formStartDate,
        endDate: this.formEndDate,
        availablePositions: this.formAvailablePositions,
        status: this.formStatus,
        requiredSkills: this.formRequiredSkills,
        preferredSkills: this.formPreferredSkills,
        educationRequirements: this.formEducationRequirements || undefined,
        experienceRequirements: this.formExperienceRequirements || undefined
      };

      this.subjectService.updateSubject(this.editingId, dto).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.success = 'Sujet mis à jour avec succès.';
          this.loadSubjects();
        },
        error: err => {
          this.saving = false;
          this.error = err.error?.error || 'Erreur lors de la mise à jour.';
        }
      });
    } else {
      const dto: SubjectCreateDto = {
        title: this.formTitle,
        description: this.formDescription,
        problemStatement: this.formProblemStatement,
        department: this.formDepartment,
        typeStage: this.formTypeStage,
        difficulty: this.formDifficulty,
        startDate: this.formStartDate,
        endDate: this.formEndDate,
        availablePositions: this.formAvailablePositions,
        requiredSkills: this.formRequiredSkills,
        preferredSkills: this.formPreferredSkills,
        educationRequirements: this.formEducationRequirements || undefined,
        experienceRequirements: this.formExperienceRequirements || undefined
      };

      this.subjectService.createSubject(dto).subscribe({
        next: () => {
          this.saving = false;
          this.closeModal();
          this.success = 'Sujet créé avec succès.';
          this.loadSubjects();
        },
        error: err => {
          this.saving = false;
          this.error = err.error?.error || 'Erreur lors de la création.';
        }
      });
    }
  }

  deleteSubject(subject: InternshipSubject): void {
    if (!confirm(`Supprimer définitivement le sujet "${subject.title}" ?`)) {
      return;
    }

    this.subjectService.deleteSubject(subject.id).subscribe({
      next: () => {
        this.success = 'Sujet supprimé.';
        this.loadSubjects();
      },
      error: err => {
        this.error = err.error?.error || 'Erreur lors de la suppression.';
      }
    });
  }

  getTotalPositions(): number {
    return this.subjects.reduce((acc, s) => acc + s.availablePositions, 0);
  }

  getFilledPositions(): number {
    return this.subjects.reduce((acc, s) => acc + s.filledPositions, 0);
  }
}
