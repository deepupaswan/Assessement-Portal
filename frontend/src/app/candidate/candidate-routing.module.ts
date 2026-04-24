import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { CandidateDashboardComponent } from './candidate-dashboard.component';
import { CandidateAssessmentComponent } from './candidate-assessment.component';
import { CandidateResultSummaryComponent } from './candidate-result-summary.component';

const routes: Routes = [
  { path: '', component: CandidateDashboardComponent },
  { path: 'assessment/:candidateAssessmentId', component: CandidateAssessmentComponent },
  { path: 'result/:assessmentId/:candidateId', component: CandidateResultSummaryComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CandidateRoutingModule {}
