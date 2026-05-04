/**
 * Shared admin shell labels: sidebar navigation and breadcrumb segment names
 */

export const AdminNavItems = [
  { icon: 'icon-grid', label: 'Dashboard', route: '/admin/dashboard' },
  { icon: 'icon-file-text', label: 'Assessments', route: '/admin/assessments' },
  { icon: 'icon-help-circle', label: 'Questions', route: '/admin/questions' },
  { icon: 'icon-users', label: 'Candidates', route: '/admin/candidates' },
  { icon: 'icon-send', label: 'Assignments', route: '/admin/assignments' },
  { icon: 'icon-activity', label: 'Live Monitoring', route: '/admin/monitoring' },
  { icon: 'icon-bar-chart-2', label: 'Analytics', route: '/admin/analytics' }
] as const;

export const AdminBreadcrumbLabels: Record<string, string> = {
  admin: 'Admin',
  dashboard: 'Dashboard',
  assessments: 'Assessments',
  questions: 'Questions',
  candidates: 'Candidates',
  assignments: 'Assignments',
  monitoring: 'Live Monitoring',
  analytics: 'Analytics'
};
