import { Injectable } from '@angular/core';
import { RealtimeNotification } from './realtime.service';

/** Toast visual type — matches NotificationService's allowed values. */
type ToastType = 'success' | 'error' | 'warning' | 'info' | 'reminder';

interface PresentedNotification {
  title: string;
  toastType: ToastType;
}

/**
 * Translates backend realtime events into human toast copy.
 *
 * Kept as a pure injectable so it can be unit-tested without Angular compilation of
 * components. One mapping table for every role: the backend message text already names the
 * concerned stagiaire/evaluation, so the title only carries the event kind.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeNotificationPresenter {
  present(n: RealtimeNotification): PresentedNotification {
    switch (n.type) {
      case 'CandidatureDeposee':
        return { title: 'Nouvelle candidature reçue 📋', toastType: 'info' };
      case 'CandidatureAcceptee':
        return { title: 'Candidature acceptée 🎉', toastType: 'success' };
      case 'CandidatureRejetee':
        return { title: 'Candidature non retenue', toastType: 'error' };
      case 'ConventionGeneree':
        return { title: 'Convention disponible 📄', toastType: 'info' };
      case 'EvaluationSoumise':
        return { title: 'Évaluation enregistrée 📝', toastType: 'info' };
      case 'EvaluationValidee':
        return { title: 'Évaluation validée ✅', toastType: 'success' };
      case 'SujetPropose':
        return { title: 'Sujet de stage proposé 💡', toastType: 'info' };
      case 'SujetAccepte':
        return { title: 'Sujet validé par le stagiaire 🎯', toastType: 'success' };
      case 'ChangementSujetDemande':
        return { title: 'Demande de changement de sujet 🔄', toastType: 'warning' };
      case 'ChangementSujetTraite':
        return { title: 'Mise à jour de votre sujet 📌', toastType: 'info' };
      case 'RappelDelai':
        return { title: 'Rappel ⏰', toastType: 'warning' };
      default:
        return { title: 'Notification', toastType: 'info' };
    }
  }
}
