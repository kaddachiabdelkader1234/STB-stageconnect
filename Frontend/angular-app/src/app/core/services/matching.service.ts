import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CandidateProfile, SubjectMatch } from '../models/subject-matching.model';

@Injectable({
  providedIn: 'root'
})
export class MatchingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/matching`;

  analyzeCandidate(stagiaireId: string, force = false, utilisateurId?: number): Observable<CandidateProfile> {
    let params = new HttpParams().set('force', force);
    if (utilisateurId) {
      params = params.set('utilisateurId', utilisateurId);
    }
    return this.http.post<CandidateProfile>(`${this.baseUrl}/candidates/${stagiaireId}/analyze`, {}, { params });
  }

  getRecommendations(stagiaireId: string): Observable<SubjectMatch[]> {
    return this.http.get<SubjectMatch[]>(`${this.baseUrl}/candidates/${stagiaireId}/recommendations`);
  }

  getCandidateProfile(stagiaireId: string): Observable<CandidateProfile> {
    return this.http.get<CandidateProfile>(`${this.baseUrl}/candidates/${stagiaireId}/profile`);
  }
}
