export type SubjectDifficulty = 'Beginner' | 'Intermediate' | 'Advanced';
export type SubjectStatus = 'Draft' | 'Open' | 'Full' | 'Assigned' | 'Closed';
export type AssignmentStatus = 'Proposed' | 'Accepted' | 'ChangeRequested' | 'Reassigned' | 'Rejected';
export type ChangeRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface InternshipSubject {
  id: string;
  title: string;
  description: string;
  problemStatement: string;
  department: string;
  typeStage: string;
  requiredSkills: string[];
  preferredSkills: string[];
  educationRequirements?: string;
  experienceRequirements?: string;
  difficulty: SubjectDifficulty;
  startDate: string;
  endDate: string;
  availablePositions: number;
  filledPositions: number;
  status: SubjectStatus;
  createdBy: number;
  createdAt: string;
  updatedAt: string;
}

export interface SubjectCreateDto {
  title: string;
  description: string;
  problemStatement: string;
  department: string;
  typeStage: string;
  requiredSkills: string[];
  preferredSkills: string[];
  educationRequirements?: string;
  experienceRequirements?: string;
  difficulty: SubjectDifficulty;
  startDate: string;
  endDate: string;
  availablePositions: number;
}

export interface SubjectUpdateDto extends SubjectCreateDto {
  status: SubjectStatus;
}

export interface CandidateProfile {
  id: string;
  stagiaireId: string;
  utilisateurId: number;
  education?: string;
  skills: string[];
  experience?: string;
  projects: string[];
  languages: string[];
  parsedAt: string;
  provider: string;
}

export interface SubjectMatch {
  id: string;
  stagiaireId: string;
  subjectId: string;
  subjectTitle: string;
  department: string;
  typeStage: string;
  difficulty: SubjectDifficulty;
  compatibilityScore: number;
  matchedSkills: string[];
  missingSkills: string[];
  explanation: string;
  availablePositions: number;
  filledPositions: number;
  status: SubjectStatus;
  generatedAt: string;
}

export interface SubjectAssignment {
  id: string;
  stagiaireId: string;
  utilisateurId: number;
  subjectId: string;
  subjectTitle: string;
  subjectDescription: string;
  problemStatement: string;
  department: string;
  requiredSkills: string[];
  status: AssignmentStatus;
  scoreAtProposal: number;
  proposedAt: string;
  respondedAt?: string;
}

export interface SubjectChangeRequest {
  id: string;
  assignmentId: string;
  stagiaireId: string;
  utilisateurId: number;
  currentSubjectId: string;
  currentSubjectTitle: string;
  requestedSubjectId: string;
  requestedSubjectTitle: string;
  reason: string;
  status: ChangeRequestStatus;
  adminComment?: string;
  requestedAt: string;
  reviewedAt?: string;
}

export const DIFFICULTY_LABELS: Record<SubjectDifficulty, string> = {
  Beginner: 'Débutant',
  Intermediate: 'Intermédiaire',
  Advanced: 'Avancé'
};

export const SUBJECT_STATUS_LABELS: Record<SubjectStatus, string> = {
  Draft: 'Brouillon',
  Open: 'Ouvert',
  Full: 'Complet',
  Assigned: 'Attribué',
  Closed: 'Fermé'
};
