import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Candidate } from '../../../core/models/candidate.models';
import { Assessment } from '../../../core/models/assessment.models';
import { CandidateApiService } from '../../../core/services/candidate-api.service';
import { AssessmentApiService } from '../../../core/services/assessment-api.service';
import { BulkUploadResult, CandidateForm, CandidateRow } from './candidates.models';
import {
  CandidateFilterStatus,
  CandidateFilterStatusValues,
  CandidateStatusLabels,
  CandidateMessages,
  CandidateCsvMessages
} from '../../../constants/candidates.constants';

@Component({
  selector: 'app-candidates',
  templateUrl: './candidates.component.html',
  styleUrls: ['./candidates.component.scss']
})
export class CandidatesComponent implements OnInit, OnDestroy {
  candidates: CandidateRow[] = [];
  loading = true;
  error: string | null = null;
  searchTerm = '';
  filterStatus: CandidateFilterStatus = CandidateFilterStatusValues.All;
  sortBy: 'name' | 'email' | 'date' = 'date';
  sortDesc = true;

  showForm = false;
  editingCandidate: CandidateRow | null = null;
  formData: CandidateForm = { name: '', email: '' };

  showBulkUpload = false;
  bulkUploadFile: File | null = null;
  bulkUploading = false;
  bulkUploadResult: BulkUploadResult | null = null;

  showAssignForm = false;
  assigningCandidate: CandidateRow | null = null;
  assessments: Assessment[] = [];
  selectedAssessment: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private candidateApi: CandidateApiService,
    private assessmentApi: AssessmentApiService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadCandidates();
    this.loadAssessments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCandidates(): void {
    this.loading = true;
    this.error = null;
    this.candidateApi
      .listCandidates()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: Candidate[]) => {
          this.candidates = data.map((c: Candidate) => ({
            ...c,
            status: CandidateStatusLabels.Active,
            assignmentCount: 0
          }));
          this.loading = false;
        },
        error: (err: any) => {
          this.error = CandidateMessages.LoadError;
          this.loading = false;
          console.error(err);
        }
      });
  }

  loadAssessments(): void {
    this.assessmentApi
      .listAssessments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: Assessment[]) => {
          this.assessments = data.filter((a: Assessment) => a.isActive);
        },
        error: (err: any) => console.error(CandidateMessages.LoadAssessmentsError, err)
      });
  }

  get filteredCandidates(): CandidateRow[] {
    let result = this.candidates;

    // Search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(
        (c) => c.name.toLowerCase().includes(term) || c.email.toLowerCase().includes(term)
      );
    }

    // Status filter
    if (this.filterStatus !== CandidateFilterStatusValues.All) {
      result = result.filter((c) => c.status?.toLowerCase() === this.filterStatus);
    }

    // Sort
    result.sort((a, b) => {
      let aVal = '';
      let bVal = '';

      if (this.sortBy === 'name') {
        aVal = a.name.toLowerCase();
        bVal = b.name.toLowerCase();
      } else if (this.sortBy === 'email') {
        aVal = a.email.toLowerCase();
        bVal = b.email.toLowerCase();
      } else {
        aVal = a.createdAt || '';
        bVal = b.createdAt || '';
      }

      const comparison = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
      return this.sortDesc ? -comparison : comparison;
    });

    return result;
  }

  changeSortBy(field: 'name' | 'email' | 'date'): void {
    if (this.sortBy === field) {
      this.sortDesc = !this.sortDesc;
    } else {
      this.sortBy = field;
      this.sortDesc = false;
    }
  }

  openCreateForm(): void {
    this.editingCandidate = null;
    this.formData = { name: '', email: '' };
    this.showForm = true;
  }

  editCandidate(candidate: CandidateRow): void {
    this.editingCandidate = candidate;
    this.formData = { name: candidate.name, email: candidate.email };
    this.showForm = true;
  }

  saveCandidate(): void {
    if (!this.formData.name.trim() || !this.formData.email.trim()) {
      this.error = CandidateMessages.NameEmailRequired;
      return;
    }

    if (!this.isValidEmail(this.formData.email)) {
      this.error = CandidateMessages.InvalidEmailFormat;
      return;
    }

    this.candidateApi
      .createCandidate(this.formData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newCandidate: Candidate) => {
          const candidateRow: CandidateRow = {
            ...newCandidate,
            status: 'Active',
            assignmentCount: 0
          };

          if (this.editingCandidate) {
            const index = this.candidates.findIndex((c) => c.id === this.editingCandidate!.id);
            if (index !== -1) {
              this.candidates[index] = candidateRow;
            }
          } else {
            this.candidates.unshift(candidateRow);
          }

          this.cancelForm();
        },
        error: (err: any) => {
          this.error = CandidateMessages.SaveError;
          console.error(err);
        }
      });
  }

  deleteCandidate(candidate: CandidateRow): void {
    if (!confirm(CandidateMessages.DeleteConfirm(candidate.name))) return;

    // Optimistic removal
    this.candidates = this.candidates.filter((c) => c.id !== candidate.id);
  }

  cancelForm(): void {
    this.showForm = false;
    this.editingCandidate = null;
    this.formData = { name: '', email: '' };
    this.error = null;
  }

  openBulkUpload(): void {
    this.showBulkUpload = true;
    this.bulkUploadFile = null;
    this.bulkUploadResult = null;
  }

  onFileSelected(event: any): void {
    this.bulkUploadFile = event.target.files[0];
  }

  uploadBulkCandidates(): void {
    if (!this.bulkUploadFile) {
      this.error = CandidateMessages.SelectCsvFile;
      return;
    }

    this.bulkUploading = true;
    this.error = null;
    this.bulkUploadResult = null;

    const reader = new FileReader();
    reader.onload = (e: any) => {
      try {
        const csv = e.target.result;
        const lines = csv.split('\n').filter((line: string) => line.trim());

        // Skip header if present
        let startIndex = 0;
        if (lines[0].toLowerCase().includes('name') && lines[0].includes('email')) {
          startIndex = 1;
        }

        const result: BulkUploadResult = {
          total: lines.length - startIndex,
          success: 0,
          failed: 0,
          errors: []
        };

        const candidatesToAdd: CandidateForm[] = [];

        for (let i = startIndex; i < lines.length; i++) {
          const parts = lines[i].split(',').map((p: string) => p.trim());
          if (parts.length < 2) {
            result.errors.push(CandidateCsvMessages.InvalidFormat(i + 1));
            result.failed++;
            continue;
          }

          const name = parts[0];
          const email = parts[1];

          if (!name || !email) {
            result.errors.push(CandidateCsvMessages.NameEmailRequired(i + 1));
            result.failed++;
            continue;
          }

          if (!this.isValidEmail(email)) {
            result.errors.push(CandidateCsvMessages.InvalidEmail(i + 1, email));
            result.failed++;
            continue;
          }

          // Check for duplicates in candidates list
          if (this.candidates.some((c) => c.email?.toLowerCase() === email.toLowerCase())) {
            result.errors.push(CandidateCsvMessages.CandidateExists(i + 1, email));
            result.failed++;
            continue;
          }

          candidatesToAdd.push({ name, email });
          result.success++;
        }

        // Add all valid candidates
        candidatesToAdd.forEach((candidate) => {
          this.candidateApi
            .createCandidate(candidate)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (newCandidate: Candidate) => {
                const candidateRow: CandidateRow = {
                  ...newCandidate,
                  status: 'Active',
                  assignmentCount: 0
                };
                this.candidates.unshift(candidateRow);
              },
              error: (err: any) => console.error(err)
            });
        });

        this.bulkUploadResult = result;
        this.bulkUploading = false;
      } catch (err) {
        this.error = CandidateMessages.ParseCsvError;
        this.bulkUploading = false;
      }
    };
    reader.readAsText(this.bulkUploadFile);
  }

  closeBulkUpload(): void {
    this.showBulkUpload = false;
    this.bulkUploadFile = null;
    this.bulkUploadResult = null;
  }

  openAssignForm(candidate: CandidateRow): void {
    this.assigningCandidate = candidate;
    this.selectedAssessment = null;
    this.showAssignForm = true;
  }

  assignAssessment(): void {
    if (!this.assigningCandidate || !this.selectedAssessment) {
      this.error = CandidateMessages.SelectAssessment;
      return;
    }

    const candidateId = this.assigningCandidate.id;
    this.candidateApi
      .assignAssessment({
        candidateId,
        assessmentId: this.selectedAssessment
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.showAssignForm = false;
          this.assigningCandidate = null;
          // Optionally refresh candidates
          this.loadCandidates();
        },
        error: (err: any) => {
          this.error = CandidateMessages.AssignError;
          console.error(err);
        }
      });
  }

  closeAssignForm(): void {
    this.showAssignForm = false;
    this.assigningCandidate = null;
    this.selectedAssessment = null;
  }

  private isValidEmail(email: string): boolean {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
  }
}
