import { Injectable, NgZone, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AuthService } from './auth.service';

const IDLE_LIMIT_MS = 2 * 60 * 1000;
const WARNING_AT_MS = 1 * 60 * 1000;

const ACTIVITY_EVENTS: (keyof DocumentEventMap)[] = [
  'mousedown',
  'mousemove',
  'keydown',
  'scroll',
  'touchstart',
  'click',
  'wheel'
];

@Injectable({ providedIn: 'root' })
export class InactivityService {
  private readonly auth = inject(AuthService);
  private readonly zone = inject(NgZone);

  private readonly warningSubject = new BehaviorSubject(false);
  readonly warningVisible$ = this.warningSubject.asObservable();

  private warningTimer: ReturnType<typeof setTimeout> | null = null;
  private logoutTimer: ReturnType<typeof setTimeout> | null = null;
  private listening = false;
  private readonly onActivity = (): void => this.resetTimers();

  start(): void {
    if (!this.auth.isLoggedIn) {
      this.stop();
      return;
    }

    this.attachListeners();
    this.resetTimers();
  }

  stop(): void {
    this.clearTimers();
    this.warningSubject.next(false);
    this.detachListeners();
  }

  staySignedIn(): void {
    if (!this.auth.isLoggedIn) {
      return;
    }

    this.resetTimers();
  }

  private resetTimers(): void {
    if (!this.auth.isLoggedIn) {
      this.stop();
      return;
    }

    this.clearTimers();
    this.warningSubject.next(false);

    this.zone.runOutsideAngular(() => {
      this.warningTimer = setTimeout(() => {
        this.zone.run(() => this.warningSubject.next(true));
      }, WARNING_AT_MS);

      this.logoutTimer = setTimeout(() => {
        this.zone.run(() => {
          this.stop();
          this.auth.logout();
        });
      }, IDLE_LIMIT_MS);
    });
  }

  private clearTimers(): void {
    if (this.warningTimer !== null) {
      clearTimeout(this.warningTimer);
      this.warningTimer = null;
    }

    if (this.logoutTimer !== null) {
      clearTimeout(this.logoutTimer);
      this.logoutTimer = null;
    }
  }

  private attachListeners(): void {
    if (this.listening) {
      return;
    }

    for (const eventName of ACTIVITY_EVENTS) {
      document.addEventListener(eventName, this.onActivity, { passive: true });
    }

    this.listening = true;
  }

  private detachListeners(): void {
    if (!this.listening) {
      return;
    }

    for (const eventName of ACTIVITY_EVENTS) {
      document.removeEventListener(eventName, this.onActivity);
    }

    this.listening = false;
  }
}
