import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

interface Breadcrumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumbs',
  templateUrl: './breadcrumbs.component.html',
  styleUrls: ['./breadcrumbs.component.scss']
})
export class BreadcrumbsComponent implements OnInit {
  breadcrumbs: Breadcrumb[] = [];

  private labelMap: Record<string, string> = {
    'admin': 'Admin',
    'dashboard': 'Dashboard',
    'assessments': 'Assessments',
    'questions': 'Questions',
    'candidates': 'Candidates',
    'assignments': 'Assignments',
    'monitoring': 'Live Monitoring',
    'analytics': 'Analytics'
  };

  constructor(private router: Router, private activatedRoute: ActivatedRoute) {}

  ngOnInit(): void {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => {
        this.buildBreadcrumbs(this.activatedRoute.root);
      });
  }

  private buildBreadcrumbs(
    route: ActivatedRoute,
    url: string = '',
    breadcrumbs: Breadcrumb[] = []
  ): Breadcrumb[] {
    const ROUTE_DATA_BREADCRUMB = 'breadcrumb';
    const children: ActivatedRoute[] = route.children;

    if (children.length === 0) {
      this.breadcrumbs = breadcrumbs;
      return breadcrumbs;
    }

    for (const child of children) {
      const routeURL: string = child.snapshot.url
        .map(segment => segment.path)
        .join('/');

      if (routeURL !== '') {
        url += `/${routeURL}`;
      }

      const label = child.snapshot.data[ROUTE_DATA_BREADCRUMB] || this.labelMap[routeURL] || routeURL;
      const breadcrumb: Breadcrumb = { label, url };

      const existingBreadcrumb = breadcrumbs.find(b => b.url === breadcrumb.url);
      if (!existingBreadcrumb) {
        breadcrumbs.push(breadcrumb);
      }

      return this.buildBreadcrumbs(child, url, breadcrumbs);
    }

    this.breadcrumbs = breadcrumbs;
    return breadcrumbs;
  }
}
