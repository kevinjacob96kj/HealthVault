import { AsyncPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';
import { InactivityService } from './auth/inactivity.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [AsyncPipe, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit, OnDestroy {
  readonly auth = inject(AuthService);
  readonly inactivity = inject(InactivityService);

  private readonly onVisibilityChange = (): void => {
    if (document.visibilityState === 'visible' && this.auth.isLoggedIn) {
      this.auth.refreshSession().subscribe();
    }
  };

  ngOnInit(): void {
    if (this.auth.isLoggedIn) {
      this.inactivity.start();
    }

    document.addEventListener('visibilitychange', this.onVisibilityChange);
  }

  ngOnDestroy(): void {
    document.removeEventListener('visibilitychange', this.onVisibilityChange);
  }
}
