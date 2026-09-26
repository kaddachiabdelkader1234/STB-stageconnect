import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { StagiaireService } from '../../../core/services/stagiaire.service';
import { CandidatureService } from '../../../core/services/candidature.service';
import { UserService, UserSummary } from '../../../core/services/user.service';
import { ApiError } from '../../../core/http/api-error';
import { PagedResult } from '../../../core/models/paged-result.model';
import { RealtimeService } from '../../../core/services/realtime.service';
import { SubjectService } from '../../../core/services/subject.service';
import { MatchingService } from '../../../core/services/matching.service';
import {
  CandidateProfile,
  SubjectMatch,
  SubjectAssignment,
  InternshipSubject
} from '../../../core/models/subject-matching.model';
import {
  Stagiaire,
  StagiaireQuery,
  STATUT_STAGIAIRE_BADGE,
  STATUT_STAGIAIRE_LABELS,
  STATUT_STAGIAIRE_VALUES,
  StatutStagiaire,
  TYPE_STAGE_LABELS,
  TYPE_STAGE_VALUES,
  TypeStage
} from '../../../core/models/stagiaire.model';

/**
 * Admin queue: review candidatures, then accept (assigning département + encadrant) or reject.
 *
 * Paging, filtering and search are all server-side — the table never receives the whole roster.
 */
@Component({
  selector: 'app-candidatures-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './candidatures-admin.component.html'
})
export class CandidaturesAdminComponent implements OnInit, OnDestroy {
  page: PagedResult<Stagiaire> | null = null;
  chargement = true;
  erreur: string | null = null;
  succes: string | null = null;

  /** Defaults to the pending queue, which is what an admin opens this screen for. */
  filtres: StagiaireQuery = {
    page: 1,
    pageSize: 10,
    statut: 'EnAttente'
  };

  encadrants: UserSummary[] = [];

  /** Candidature being acted on, and which panel is open. */
  selection: Stagiaire | null = null;
  mode: 'accepter' | 'rejeter' | 'ia' | null = null;
  actionEnCours = false;

  // AI Matching state
  profilIa: CandidateProfile | null = null;
  recommandations: SubjectMatch[] = [];
  affectationActuelle: SubjectAssignment | null = null;
  chargementIa = false;
  erreurIa: string | null = null;
  succesIa: string | null = null;
  proposerEnCoursId: string | null = null;

  // Accept form
  encadrantId: number | null = null;
  departement = '';
  dateDebut = '';
  dateFin = '';
  selectedSubjectId: string | null = null;
  sujetsDisponibles: InternshipSubject[] = [];
  chargementSujets = false;
  topRecommendation: SubjectMatch | null = null;
  analyseIaErreur: string | null = null;

  // Reject form
  motifRejet = '';

  erreursChamps: Record<string, string[]> = {};

  readonly statuts = STATUT_STAGIAIRE_VALUES;
  readonly statutLabels = STATUT_STAGIAIRE_LABELS;
  readonly statutBadges = STATUT_STAGIAIRE_BADGE;
  readonly typesStage = TYPE_STAGE_VALUES;
  readonly typeStageLabels = TYPE_STAGE_LABELS;

  // Preview modal
  previewOpen = false;
  previewTitle = '';
  previewUrl: string | null = null;
  previewSafeUrl: SafeResourceUrl | null = null;
  previewLoading = false;

  private realtimeSub?: Subscription;

  constructor(
    private stagiaireService: StagiaireService,
    private candidatureService: CandidatureService,
    private userService: UserService,
    private sanitizer: DomSanitizer,
    private realtimeService: RealtimeService,
    private subjectService: SubjectService,
    private matchingService: MatchingService
  ) {}

  ngOnInit(): void {
    this.charger();
    this.chargerEncadrants();

    // Live refresh — when a candidature is accepted or rejected, reload the queue so the table
    // never shows a stale status. Scoped server-side to this user's own notifications.
    this.realtimeSub = this.realtimeService.newNotification$.subscribe(notification => {
      if (notification.type === 'CandidatureAcceptee' || notification.type === 'CandidatureRejetee') {
        this.charger();
      }
    });
  }

  ngOnDestroy(): void {
    this.realtimeSub?.unsubscribe();
  }

  charger(): void {
    this.chargement = true;
    this.erreur = null;

    this.stagiaireService.getAll(this.filtres).subscribe({
      next: page => {
        this.page = page;
        this.chargement = false;
      },
      error: (error: ApiError) => {
        this.erreur = error.message;
        this.chargement = false;
      }
    });
  }

  private chargerEncadrants(): void {
    this.userService.getTrainers().subscribe({
      next: encadrants => (this.encadrants = encadrants),
      error: (error: ApiError) => (this.erreur = error.message)
    });
  }

  /** Any filter change resets to page 1 — staying on page 5 of a narrower result set shows nothing. */
  appliquerFiltres(): void {
    this.filtres.page = 1;
    this.charger();
  }

  changerPage(delta: number): void {
    const cible = (this.filtres.page ?? 1) + delta;

    if (cible < 1 || (this.page && cible > this.page.totalPages)) {
      return;
    }

    this.filtres.page = cible;
    this.charger();
  }

  ouvrir(candidature: Stagiaire, mode: 'accepter' | 'rejeter'): void {
    this.selection = candidature;
    this.mode = mode;
    this.erreursChamps = {};
    this.erreur = null;
    this.selectedSubjectId = null;
    this.topRecommendation = null;
    this.recommandations = [];
    this.profilIa = null;
    this.erreurIa = null;
    this.succesIa = null;

    // Prefill with what the candidate requested, so accepting unchanged is one click.
    this.departement = candidature.departement;
    this.dateDebut = candidature.dateDebut;
    this.dateFin = candidature.dateFin;
    this.encadrantId = null;
    this.motifRejet = '';

    if (mode === 'accepter') {
      this.preparerAcceptation(candidature);
    }
  }

  preparerAcceptation(candidature: Stagiaire): void {
    this.chargementSujets = true;
    this.chargementIa = true;
    this.analyseIaErreur = null;

    // 1. Charger tous les sujets disponibles (Open)
    this.subjectService.getSubjects({ status: 'Open' }).subscribe({
      next: sujets => {
        if (sujets && sujets.length > 0) {
          this.sujetsDisponibles = sujets;
          this.chargementSujets = false;
        } else {
          this.subjectService.getSubjects().subscribe({
            next: allSujets => {
              this.sujetsDisponibles = allSujets;
              this.chargementSujets = false;
            },
            error: () => {
              this.chargementSujets = false;
            }
          });
        }
      },
      error: () => {
        this.subjectService.getSubjects().subscribe({
          next: allSujets => {
            this.sujetsDisponibles = allSujets;
            this.chargementSujets = false;
          },
          error: () => {
            this.chargementSujets = false;
          }
        });
      }
    });

    // 2. Déclencher automatiquement l'analyse IA ou charger le profil existant
    this.matchingService.getCandidateProfile(candidature.id).subscribe({
      next: profile => {
        this.profilIa = profile;
        this.chargerRecommandationsPourAcceptation(candidature.id);
      },
      error: () => {
        // Profil non existant : lancer l'analyse automatique du CV
        this.matchingService.analyzeCandidate(candidature.id, false, candidature.utilisateurId ?? undefined).subscribe({
          next: profile => {
            this.profilIa = profile;
            this.chargerRecommandationsPourAcceptation(candidature.id);
          },
          error: err => {
            this.chargementIa = false;
            this.analyseIaErreur = err.error?.error || "L'analyse automatique du profil n'a pas pu être effectuée.";
          }
        });
      }
    });
  }

  chargerRecommandationsPourAcceptation(stagiaireId: string): void {
    this.matchingService.getRecommendations(stagiaireId).subscribe({
      next: list => {
        this.recommandations = list;
        this.chargementIa = false;
        if (list.length > 0) {
          this.topRecommendation = list[0];
          // Pré-sélectionner automatiquement le meilleur sujet recommandé
          this.selectedSubjectId = list[0].subjectId;
          if (list[0].department) {
            this.departement = list[0].department;
          }
        }
      },
      error: () => {
        this.chargementIa = false;
      }
    });
  }

  onSubjectSelected(subjectId: string): void {
    this.selectedSubjectId = subjectId;
    const match = this.recommandations.find(r => r.subjectId === subjectId);
    if (match?.department) {
      this.departement = match.department;
    } else {
      const subject = this.sujetsDisponibles.find(s => s.id === subjectId);
      if (subject?.department) {
        this.departement = subject.department;
      }
    }
  }

  fermer(): void {
    this.selection = null;
    this.mode = null;
    this.profilIa = null;
    this.recommandations = [];
    this.affectationActuelle = null;
    this.erreurIa = null;
    this.succesIa = null;
    this.selectedSubjectId = null;
    this.topRecommendation = null;
    this.sujetsDisponibles = [];
    this.analyseIaErreur = null;
  }

  ouvrirAiMatching(candidature: Stagiaire): void {
    this.selection = candidature;
    this.mode = 'ia';
    this.erreurIa = null;
    this.succesIa = null;
    this.profilIa = null;
    this.recommandations = [];
    this.affectationActuelle = null;

    // Check existing assignment
    this.subjectService.getAssignmentByStagiaire(candidature.id).subscribe({
      next: a => { this.affectationActuelle = a; },
      error: () => { this.affectationActuelle = null; }
    });

    // Check if profile exists already
    this.matchingService.getCandidateProfile(candidature.id).subscribe({
      next: profile => {
        this.profilIa = profile;
        this.chargerRecommandations(candidature.id);
      },
      error: () => {
        // Not analyzed yet — admin can click analyze
      }
    });
  }

  analyserCvIa(force = false): void {
    if (!this.selection) return;

    this.chargementIa = true;
    this.erreurIa = null;
    this.succesIa = null;

    this.matchingService.analyzeCandidate(this.selection.id, force, this.selection.utilisateurId ?? undefined).subscribe({
      next: profile => {
        this.profilIa = profile;
        this.chargementIa = false;
        this.succesIa = 'Analyse du CV effectuée avec succès.';
        this.chargerRecommandations(this.selection!.id);
      },
      error: err => {
        this.chargementIa = false;
        this.erreurIa = err.error?.error || 'Erreur lors de l\'analyse du CV.';
      }
    });
  }

  chargerRecommandations(stagiaireId: string): void {
    this.matchingService.getRecommendations(stagiaireId).subscribe({
      next: list => {
        this.recommandations = list;
      },
      error: err => {
        this.erreurIa = err.error?.error || 'Impossible de calculer les recommandations.';
      }
    });
  }

  proposerSujet(match: SubjectMatch): void {
    if (!this.selection) return;

    this.proposerEnCoursId = match.subjectId;
    this.erreurIa = null;
    this.succesIa = null;

    this.subjectService.proposeSubject(
      this.selection.id,
      match.subjectId,
      this.selection.utilisateurId ?? undefined,
      this.selection.email,
      `${this.selection.prenom} ${this.selection.nom}`
    ).subscribe({
      next: assignment => {
        this.proposerEnCoursId = null;
        this.affectationActuelle = assignment;
        this.succesIa = `Le sujet "${match.subjectTitle}" a été proposé avec succès au stagiaire !`;
      },
      error: err => {
        this.proposerEnCoursId = null;
        this.erreurIa = err.error?.error || 'Erreur lors de la proposition du sujet.';
      }
    });
  }

  confirmerAcceptation(): void {
    if (!this.selection || this.actionEnCours) {
      return;
    }

    this.erreursChamps = {};

    if (!this.encadrantId) {
      this.erreursChamps['encadrantId'] = ['Veuillez sélectionner un encadrant.'];
    }

    if (!this.selectedSubjectId) {
      this.erreursChamps['selectedSubjectId'] = ['Veuillez sélectionner un sujet de stage pour le candidat.'];
    }

    if (Object.keys(this.erreursChamps).length > 0) {
      return;
    }

    const encadrant = this.encadrants.find(e => e.userId === Number(this.encadrantId));
    const candidateName = `${this.selection.prenom} ${this.selection.nom}`;
    const subjectId = this.selectedSubjectId!;
    const candidateEmail = this.selection.email;
    const encadrantNom = encadrant?.firstName ?? 'Encadrant';

    this.actionEnCours = true;

    // 1. Accepter la candidature dans Stagiaire.Service
    this.candidatureService
      .accepter(this.selection.id, {
        departement: this.departement,
        encadrantId: Number(this.encadrantId),
        encadrantNom: encadrantNom,
        dateDebut: this.dateDebut,
        dateFin: this.dateFin
      })
      .subscribe({
        next: () => {
          // 2. Proposer et affecter le sujet dans SubjectMatching.Service
          this.subjectService.proposeSubject(
            this.selection!.id,
            subjectId,
            this.selection!.utilisateurId ?? undefined,
            candidateEmail,
            candidateName,
            encadrantNom
          ).subscribe({
            next: () => {
              this.actionEnCours = false;
              this.succes = `Candidature acceptée ! Le sujet de stage a été attribué et un email de confirmation a été envoyé à ${candidateName}.`;
              this.fermer();
              this.charger();
            },
            error: () => {
              this.actionEnCours = false;
              this.succes = 'Candidature acceptée. Attention: vérifiez l\'affectation du sujet.';
              this.fermer();
              this.charger();
            }
          });
        },
        error: (error: ApiError) => {
          this.actionEnCours = false;
          this.erreur = error.message;
          this.erreursChamps = error.fieldErrors ?? {};
        }
      });
  }

  confirmerRejet(): void {
    if (!this.selection || this.actionEnCours) {
      return;
    }

    this.actionEnCours = true;
    this.erreursChamps = {};

    this.candidatureService.rejeter(this.selection.id, { motifRejet: this.motifRejet }).subscribe({
      next: () => {
        this.actionEnCours = false;
        this.succes = 'Candidature rejetée.';
        this.fermer();
        this.charger();
      },
      error: (error: ApiError) => {
        this.actionEnCours = false;
        this.erreur = error.message;
        this.erreursChamps = error.fieldErrors ?? {};
      }
    });
  }

  telechargerCv(candidature: Stagiaire): void {
    this.candidatureService.telechargerCv(candidature.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = candidature.cvNomFichier ?? `cv-${candidature.nom}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (error: ApiError) => (this.erreur = error.message)
    });
  }

  telechargerDocument(candidature: Stagiaire): void {
    this.candidatureService.telechargerDocument(candidature.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = candidature.documentNomFichier ?? `candidature-${candidature.nom}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (error: ApiError) => (this.erreur = error.message)
    });
  }

  /** Open a preview modal for a file (CV or candidature document). */
  previewer(candidature: Stagiaire, type: 'cv' | 'document'): void {
    this.previewLoading = true;
    this.previewTitle = type === 'cv'
      ? `CV — ${candidature.prenom} ${candidature.nom}`
      : `Demande de stage — ${candidature.prenom} ${candidature.nom}`;
    this.previewOpen = true;

    const call$ = type === 'cv'
      ? this.candidatureService.telechargerCv(candidature.id)
      : this.candidatureService.telechargerDocument(candidature.id);

    call$.subscribe({
      next: blob => {
        // Revoke previous URL to avoid memory leaks
        if (this.previewUrl) {
          URL.revokeObjectURL(this.previewUrl);
        }
        this.previewUrl = URL.createObjectURL(blob);
        this.previewSafeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.previewUrl);
        this.previewLoading = false;
      },
      error: (error: ApiError) => {
        this.previewLoading = false;
        this.previewOpen = false;
        this.erreur = error.message;
      }
    });
  }

  fermerPreview(): void {
    this.previewOpen = false;
    if (this.previewUrl) {
      URL.revokeObjectURL(this.previewUrl);
      this.previewUrl = null;
      this.previewSafeUrl = null;
    }
  }

  telechargerPreview(): void {
    if (!this.previewUrl) return;
    const link = document.createElement('a');
    link.href = this.previewUrl;
    link.download = this.previewTitle;
    link.click();
  }

  /** Only a pending candidature can be decided on. */
  estEnAttente(candidature: Stagiaire): boolean {
    return candidature.statut === 'EnAttente';
  }

  // Bound to <select> values, which arrive as strings — normalize to the union type or undefined.
  set statutFiltre(value: string) {
    this.filtres.statut = (value || undefined) as StatutStagiaire | undefined;
  }

  get statutFiltre(): string {
    return this.filtres.statut ?? '';
  }

  set typeStageFiltre(value: string) {
    this.filtres.typeStage = (value || undefined) as TypeStage | undefined;
  }

  get typeStageFiltre(): string {
    return this.filtres.typeStage ?? '';
  }
}
