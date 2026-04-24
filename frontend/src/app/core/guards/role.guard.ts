import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const requiredRole = route.data['role'] as string | undefined;
    if (!requiredRole) {
      return true;
    }

    const user = this.authService.getUser();
    if (user && user.role.toLowerCase() === requiredRole.toLowerCase()) {
      return true;
    }

    return this.router.createUrlTree(['/auth/login']);
  }
}
