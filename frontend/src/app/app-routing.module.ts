import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { AppRoutes, AppRoles, AppRouteUrls } from './constants/app.constants';

const routes: Routes = [
  { path: '', redirectTo: `${AppRoutes.auth}/${AppRoutes.login}`, pathMatch: 'full' },
  { path: AppRoutes.auth, loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule) },
  {
    path: AppRoutes.admin,
    canActivate: [AuthGuard, RoleGuard],
    data: { role: AppRoles.Admin },
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule)
  },
  {
    path: AppRoutes.candidate,
    canActivate: [AuthGuard, RoleGuard],
    data: { role: AppRoles.Candidate },
    loadChildren: () => import('./candidate/candidate.module').then(m => m.CandidateModule)
  },
  { path: '**', redirectTo: `${AppRoutes.auth}/${AppRoutes.login}` }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
