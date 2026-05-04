import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { NavItem } from './admin-sidebar.models';
import { AdminNavItems } from '../../constants/admin-navigation.constants';

@Component({
  selector: 'app-admin-sidebar',
  templateUrl: './admin-sidebar.component.html',
  styleUrls: ['./admin-sidebar.component.scss']
})
export class AdminSidebarComponent {
  @Input() isOpen = true;
  @Output() toggleSidebar = new EventEmitter<void>();

  navItems: NavItem[] = [...AdminNavItems] as NavItem[];

  constructor(public router: Router) {}

  isActive(route: string): boolean {
    return this.router.url.includes(route);
  }

  onNavClick(): void {
    if (window.innerWidth <= 768) {
      this.toggleSidebar.emit();
    }
  }
}
