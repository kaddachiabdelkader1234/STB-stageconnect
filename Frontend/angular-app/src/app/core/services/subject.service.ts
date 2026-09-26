import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InternshipSubject,
  SubjectCreateDto,
  SubjectUpdateDto,
  SubjectAssignment,
  SubjectChangeRequest
} from '../models/subject-matching.model';

@Injectable({
  providedIn: 'root'
})
export class SubjectService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/subjects`;
  private readonly assignmentsUrl = `${environment.apiUrl}/subject-assignments`;
  private readonly changeRequestsUrl = `${environment.apiUrl}/subject-change-requests`;

  getSubjects(filters?: { status?: string; department?: string; typeStage?: string; search?: string }): Observable<InternshipSubject[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.department) params = params.set('department', filters.department);
    if (filters?.typeStage) params = params.set('typeStage', filters.typeStage);
    if (filters?.search) params = params.set('search', filters.search);

    return this.http.get<InternshipSubject[]>(this.baseUrl, { params });
  }

  getSubjectById(id: string): Observable<InternshipSubject> {
    return this.http.get<InternshipSubject>(`${this.baseUrl}/${id}`);
  }

  createSubject(dto: SubjectCreateDto): Observable<InternshipSubject> {
    return this.http.post<InternshipSubject>(this.baseUrl, dto);
  }

  updateSubject(id: string, dto: SubjectUpdateDto): Observable<InternshipSubject> {
    return this.http.put<InternshipSubject>(`${this.baseUrl}/${id}`, dto);
  }

  deleteSubject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Assignments
  proposeSubject(
    stagiaireId: string,
    subjectId: string,
    utilisateurId?: number,
    candidateEmail?: string,
    candidateName?: string,
    encadrantNom?: string
  ): Observable<SubjectAssignment> {
    return this.http.post<SubjectAssignment>(`${this.assignmentsUrl}/propose`, {
      stagiaireId,
      subjectId,
      utilisateurId: utilisateurId ?? 0,
      candidateEmail,
      candidateName,
      encadrantNom
    });
  }

  getMyAssignment(): Observable<SubjectAssignment> {
    return this.http.get<SubjectAssignment>(`${this.assignmentsUrl}/my-assignment`);
  }

  getAssignmentByStagiaire(stagiaireId: string): Observable<SubjectAssignment> {
    return this.http.get<SubjectAssignment>(`${this.assignmentsUrl}/stagiaire/${stagiaireId}`);
  }

  acceptSubject(assignmentId: string): Observable<SubjectAssignment> {
    return this.http.post<SubjectAssignment>(`${this.assignmentsUrl}/${assignmentId}/accept`, {});
  }

  // Change requests
  requestChange(assignmentId: string, requestedSubjectId: string, reason: string): Observable<SubjectChangeRequest> {
    return this.http.post<SubjectChangeRequest>(this.changeRequestsUrl, {
      assignmentId,
      requestedSubjectId,
      reason
    });
  }

  getChangeRequests(status?: string): Observable<SubjectChangeRequest[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<SubjectChangeRequest[]>(this.changeRequestsUrl, { params });
  }

  approveChangeRequest(id: string, comment?: string): Observable<SubjectChangeRequest> {
    return this.http.post<SubjectChangeRequest>(`${this.changeRequestsUrl}/${id}/approve`, {
      approved: true,
      comment
    });
  }

  rejectChangeRequest(id: string, comment?: string): Observable<SubjectChangeRequest> {
    return this.http.post<SubjectChangeRequest>(`${this.changeRequestsUrl}/${id}/reject`, {
      approved: false,
      comment
    });
  }
}
