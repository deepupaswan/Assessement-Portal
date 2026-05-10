import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AgGridModule } from 'ag-grid-angular';
import { CandidateDashboardComponent } from './candidate-dashboard.component';
import { CandidateAssessmentComponent } from './candidate-assessment.component';
import { CandidateResultSummaryComponent } from './candidate-result-summary.component';
import { CandidateRoutingModule } from './candidate-routing.module';

@NgModule({
  declarations: [CandidateDashboardComponent, CandidateAssessmentComponent, CandidateResultSummaryComponent],
  imports: [CommonModule, FormsModule, RouterModule, CandidateRoutingModule, AgGridModule],
  exports: [CandidateDashboardComponent, CandidateAssessmentComponent, CandidateResultSummaryComponent]
})
export class CandidateModule {}
