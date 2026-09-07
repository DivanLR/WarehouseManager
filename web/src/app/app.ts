import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { Auth } from './auth/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected readonly isAuthenticated = this.auth.isAuthenticated;

  protected signOut(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
