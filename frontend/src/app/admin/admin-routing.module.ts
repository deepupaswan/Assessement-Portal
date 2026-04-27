import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { AdminDashboardComponent } from './pages/dashboard/dashboard.component';
import { AssessmentsComponent } from './pages/assessments/assessments.component';
import { QuestionsComponent } from './pages/questions/questions.component';
import { CandidatesComponent } from './pages/candidates/candidates.component';
import { AssignmentsComponent } from './pages/assignments/assignments.component';
import { MonitoringComponent } from './pages/monitoring/monitoring.component';
import { AnalyticsComponent } from './pages/analytics/analytics.component';
import { AdminDashboardComponent as LegacyAdminDashboardComponent } from './admin-dashboard.component';

const routes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: AdminDashboardComponent },
      { path: 'assessments', component: AssessmentsComponent },
      { path: 'assessments/:id/questions', component: QuestionsComponent },
      { path: 'questions', component: QuestionsComponent },
      { path: 'questions/:id', component: QuestionsComponent },
      { path: 'candidates', component: CandidatesComponent },
      { path: 'assignments', component: AssignmentsComponent },
      { path: 'monitoring', component: MonitoringComponent },
      { path: 'analytics', component: AnalyticsComponent }
    ]
  },
  // Legacy route for backward compatibility
  { path: 'legacy', component: LegacyAdminDashboardComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
