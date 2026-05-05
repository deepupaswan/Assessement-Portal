import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'warning' | 'info';
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private toasts: Toast[] = [];
  private toastIdCounter = 0;
  private toastsSubject = new BehaviorSubject<Toast[]>([]);

  /**
   * Observable stream of toast notifications
   */
  toasts$: Observable<Toast[]> = this.toastsSubject.asObservable();

  /**
   * Display a success notification to the user
   */
  showSuccess(message: string, duration: number = 3000): void {
    this.addToast(message, 'success', duration);
  }

  /**
   * Display an error notification to the user
   */
  showError(message: string, duration: number = 4000): void {
    this.addToast(message, 'error', duration);
  }

  /**
   * Display a warning notification to the user
   */
  showWarning(message: string, duration: number = 3000): void {
    this.addToast(message, 'warning', duration);
  }

  /**
   * Display an info notification to the user
   */
  showInfo(message: string, duration: number = 3000): void {
    this.addToast(message, 'info', duration);
  }

  /**
   * Get all current toasts
   */
  getToasts(): Toast[] {
    return [...this.toasts];
  }

  /**
   * Remove a toast by ID
   */
  removeToast(id: string): void {
    this.toasts = this.toasts.filter(t => t.id !== id);
    this.toastsSubject.next([...this.toasts]);
  }

  /**
   * Add a toast notification
   */
  private addToast(message: string, type: Toast['type'], duration: number): void {
    const id = `toast-${this.toastIdCounter++}`;
    const toast: Toast = { id, message, type, duration };

    this.toasts.push(toast);
    this.toastsSubject.next([...this.toasts]);

    // Auto-remove after duration
    if (duration > 0) {
      setTimeout(() => {
        this.removeToast(id);
      }, duration);
    }
  }
}



