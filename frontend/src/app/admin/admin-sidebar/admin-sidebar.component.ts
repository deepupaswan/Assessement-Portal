import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';

interface NavItem {
  icon: string;
  label: string;
  route: string;
}

@Component({
  selector: 'app-admin-sidebar',
  templateUrl: './admin-sidebar.component.html',
  styleUrls: ['./admin-sidebar.component.scss']
})
export class AdminSidebarComponent {
  @Input() isOpen = true;
  @Output() toggleSidebar = new EventEmitter<void>();

  navItems: NavItem[] = [
    { icon: 'icon-grid', label: 'Dashboard', route: '/admin/dashboard' },
    { icon: 'icon-file-text', label: 'Assessments', route: '/admin/assessments' },
    { icon: 'icon-help-circle', label: 'Questions', route: '/admin/questions' },
    { icon: 'icon-users', label: 'Candidates', route: '/admin/candidates' },
    { icon: 'icon-send', label: 'Assignments', route: '/admin/assignments' },
    { icon: 'icon-activity', label: 'Live Monitoring', route: '/admin/monitoring' },
    { icon: 'icon-bar-chart-2', label: 'Analytics', route: '/admin/analytics' }
  ];

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
