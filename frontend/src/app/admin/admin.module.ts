import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

// Layout Components
import { AdminLayoutComponent } from './admin-layout/admin-layout.component';
import { AdminSidebarComponent } from './admin-sidebar/admin-sidebar.component';
import { BreadcrumbsComponent } from './breadcrumbs/breadcrumbs.component';

// Page Components
import { AdminDashboardComponent } from './pages/dashboard/dashboard.component';
import { AssessmentsComponent } from './pages/assessments/assessments.component';
import { QuestionsComponent } from './pages/questions/questions.component';
import { CandidatesComponent } from './pages/candidates/candidates.component';
import { AssignmentsComponent } from './pages/assignments/assignments.component';
import { MonitoringComponent } from './pages/monitoring/monitoring.component';
import { AnalyticsComponent } from './pages/analytics/analytics.component';

// Old Dashboard (for migration/legacy)
import { AdminDashboardComponent as LegacyAdminDashboardComponent } from './admin-dashboard.component';

import { AdminRoutingModule } from './admin-routing.module';

@NgModule({
  declarations: [
    // Layout
    AdminLayoutComponent,
    AdminSidebarComponent,
    BreadcrumbsComponent,
    // Pages
    AdminDashboardComponent,
    AssessmentsComponent,
    QuestionsComponent,
    CandidatesComponent,
    AssignmentsComponent,
    MonitoringComponent,
    AnalyticsComponent,
    // Legacy (temporary)
    LegacyAdminDashboardComponent
  ],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, AdminRoutingModule],
  exports: [AdminLayoutComponent]
})
export class AdminModule {}
